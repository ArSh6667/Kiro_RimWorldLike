using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Tests.Tasks
{
    /// <summary>
    /// 协作系统基于属性的测试
    /// Feature: rimworld-game-framework
    /// </summary>
    [TestFixture]
    public class CollaborationPropertyTests
    {
        private TaskSystem _taskSystem = null!;
        private CharacterSystem _characterSystem = null!;
        private CollaborationSystem _collaborationSystem = null!;
        private IEntityManager _entityManager = null!;

        [SetUp]
        public void Setup()
        {
            _entityManager = new EntityManager();
            _taskSystem = new TaskSystem();
            _characterSystem = new CharacterSystem(_entityManager);
            _collaborationSystem = new CollaborationSystem(_taskSystem, _characterSystem);

            _taskSystem.Initialize();
            _characterSystem.Initialize();
            _collaborationSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _collaborationSystem.Shutdown();
            _characterSystem.Shutdown();
            _taskSystem.Shutdown();
        }

        /// <summary>
        /// Property 4: 协作冲突避免
        /// 对于任何需要多人协作的任务，系统应当协调分配以避免资源冲突和重复工作
        /// 验证需求: 需求 2.3
        /// </summary>
        [Property(Arbitrary = new[] { typeof(CollaborationGenerators) })]
        [Category("Property")]
        public Property CollaborationConflictAvoidance_ShouldCoordinateAssignments(
            List<ValidCharacterData> characterDataList,
            List<ValidTaskData> taskDataList)
        {
            return Prop.ForAll(
                Gen.Choose(2, 5).ToArbitrary(), // 角色数量
                Gen.Choose(1, 3).ToArbitrary(), // 任务数量
                (characterCount, taskCount) =>
                {
                    try
                    {
                        // 限制输入大小以确保测试性能
                        var characters = characterDataList.Take(Math.Min(characterCount, 5)).ToList();
                        var tasks = taskDataList.Take(Math.Min(taskCount, 3)).ToList();

                        if (!characters.Any() || !tasks.Any())
                            return true;

                        // 创建角色实体
                        var characterEntities = new List<CharacterEntity>();
                        foreach (var charData in characters)
                        {
                            var character = CreateTestCharacter(charData);
                            _characterSystem.RegisterCharacter(character);
                            characterEntities.Add(character);
                        }

                        // 创建协作任务
                        var taskIds = new List<TaskId>();
                        foreach (var taskData in tasks)
                        {
                            var definition = CreateCollaborativeTaskDefinition(taskData);
                            var result = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
                            
                            if (result.IsSuccess && result.Group != null)
                            {
                                taskIds.Add(result.Group.TaskId);
                            }
                        }

                        if (!taskIds.Any())
                            return true;

                        // 执行自动分配
                        var coordinationResult = _collaborationSystem.AutoAssignCollaborativeTasks();

                        // 验证协作冲突避免属性
                        return ValidateCollaborationConflictAvoidance(coordinationResult, characterEntities, taskIds);
                    }
                    catch (Exception)
                    {
                        // 异常情况下应该优雅处理
                        return false;
                    }
                });
        }

        /// <summary>
        /// 验证协作冲突避免
        /// </summary>
        private bool ValidateCollaborationConflictAvoidance(TaskCoordinationResult result, List<CharacterEntity> characters, List<TaskId> taskIds)
        {
            // 1. 验证没有角色被重复分配到冲突的任务
            var characterAssignments = result.Assignments.GroupBy(a => a.CharacterId);
            foreach (var group in characterAssignments)
            {
                if (group.Count() > 1)
                {
                    // 检查是否有冲突解决方案
                    var hasResolution = result.ConflictResolutions.Any(r => 
                        r.ConflictType == ConflictType.CharacterOverassignment);
                    
                    if (!hasResolution)
                        return false;
                }
            }

            // 2. 验证资源冲突被正确处理
            var positionBasedTasks = result.Assignments
                .Where(a => taskIds.Contains(a.TaskId))
                .GroupBy(a => GetTaskPosition(a.TaskId))
                .Where(g => g.Key.HasValue);

            foreach (var positionGroup in positionBasedTasks)
            {
                var position = positionGroup.Key!.Value;
                var assignmentsAtPosition = positionGroup.ToList();
                
                // 检查同一位置的多个分配是否被协调
                if (assignmentsAtPosition.Count > 1)
                {
                    // 应该有适当的角色分工（领导者和工人）
                    var hasLeader = assignmentsAtPosition.Any(a => a.Role == CollaborationRole.Leader);
                    var hasWorkers = assignmentsAtPosition.Any(a => a.Role == CollaborationRole.Worker);
                    
                    if (!hasLeader && assignmentsAtPosition.Count > 1)
                        return false;
                }
            }

            // 3. 验证协作组的合理性
            foreach (var group in result.CreatedGroups)
            {
                var groupAssignments = result.Assignments.Where(a => a.TaskId == group.TaskId).ToList();
                
                // 协作组应该有合理的参与者数量
                if (groupAssignments.Count > group.MaxParticipants)
                    return false;
                
                // 应该有适当的角色分配
                if (groupAssignments.Count > 1)
                {
                    var roleDistribution = groupAssignments.GroupBy(a => a.Role).ToDictionary(g => g.Key, g => g.Count());
                    
                    // 应该最多有一个领导者
                    if (roleDistribution.GetValueOrDefault(CollaborationRole.Leader, 0) > 1)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 资源预订冲突避免属性测试
        /// </summary>
        [Property(Arbitrary = new[] { typeof(CollaborationGenerators) })]
        [Category("Property")]
        public Property ResourceReservation_ShouldAvoidConflicts(
            List<ValidPosition> positions,
            List<ValidCharacterData> characterDataList)
        {
            return Prop.ForAll(
                Gen.Choose(2, 4).ToArbitrary(),
                (characterCount) =>
                {
                    try
                    {
                        var characters = characterDataList.Take(Math.Min(characterCount, 4)).ToList();
                        var testPositions = positions.Take(Math.Min(3, positions.Count)).ToList();

                        if (!characters.Any() || !testPositions.Any())
                            return true;

                        // 创建角色
                        var characterEntities = new List<CharacterEntity>();
                        foreach (var charData in characters)
                        {
                            var character = CreateTestCharacter(charData);
                            _characterSystem.RegisterCharacter(character);
                            characterEntities.Add(character);
                        }

                        // 测试资源预订冲突避免
                        var reservationResults = new List<ResourceReservationResult>();
                        
                        foreach (var position in testPositions)
                        {
                            foreach (var character in characterEntities)
                            {
                                var result = _collaborationSystem.ReserveWorkArea(
                                    position.ToVector3(), 
                                    character.Id, 
                                    60f // 1分钟预订
                                );
                                reservationResults.Add(result);
                            }
                        }

                        // 验证资源冲突避免
                        return ValidateResourceConflictAvoidance(reservationResults, testPositions, characterEntities);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// 验证资源冲突避免
        /// </summary>
        private bool ValidateResourceConflictAvoidance(List<ResourceReservationResult> results, List<ValidPosition> positions, List<CharacterEntity> characters)
        {
            // 对于每个位置，应该最多只有一个成功的预订
            foreach (var position in positions)
            {
                var positionResults = results.Where(r => 
                    r.Reservation != null && 
                    Vector3.Distance(r.Reservation.Position, position.ToVector3()) < 0.1f
                ).ToList();

                var successfulReservations = positionResults.Count(r => r.IsSuccess);
                
                // 同一位置应该最多只有一个成功的预订
                if (successfulReservations > 1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 协作效率属性测试
        /// </summary>
        [Property(Arbitrary = new[] { typeof(CollaborationGenerators) })]
        [Category("Property")]
        public Property CollaborationEfficiency_ShouldImproveWithCoordination(
            List<ValidCharacterData> characterDataList,
            ValidTaskData taskData)
        {
            return Prop.ForAll(
                Gen.Choose(3, 6).ToArbitrary(),
                (characterCount) =>
                {
                    try
                    {
                        var characters = characterDataList.Take(Math.Min(characterCount, 6)).ToList();
                        
                        if (characters.Count < 2)
                            return true;

                        // 创建角色
                        var characterEntities = new List<CharacterEntity>();
                        foreach (var charData in characters)
                        {
                            var character = CreateTestCharacter(charData);
                            _characterSystem.RegisterCharacter(character);
                            characterEntities.Add(character);
                        }

                        // 创建需要协作的任务
                        var definition = CreateCollaborativeTaskDefinition(taskData);
                        definition.MaxAssignedCharacters = Math.Min(characters.Count, 3);
                        
                        var result = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
                        
                        if (!result.IsSuccess || result.Group == null)
                            return true;

                        // 分配角色到协作任务
                        var assignments = new List<CollaborationResult>();
                        for (int i = 0; i < Math.Min(characters.Count, definition.MaxAssignedCharacters); i++)
                        {
                            var role = i == 0 ? CollaborationRole.Leader : CollaborationRole.Worker;
                            var assignment = _collaborationSystem.JoinCollaboration(
                                result.Group.TaskId, 
                                characterEntities[i].Id, 
                                role
                            );
                            assignments.Add(assignment);
                        }

                        // 验证协作效率
                        return ValidateCollaborationEfficiency(assignments, result.Group);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// 验证协作效率
        /// </summary>
        private bool ValidateCollaborationEfficiency(List<CollaborationResult> assignments, CollaborationGroup group)
        {
            // 成功的分配应该多于失败的分配
            var successfulAssignments = assignments.Count(a => a.IsSuccess);
            var totalAssignments = assignments.Count;
            
            if (totalAssignments == 0)
                return true;

            // 至少50%的分配应该成功
            var successRate = (float)successfulAssignments / totalAssignments;
            if (successRate < 0.5f)
                return false;

            // 如果有成功的分配，协作组应该是活跃的或正在形成
            if (successfulAssignments > 0)
            {
                return group.Status == CollaborationStatus.Active || 
                       group.Status == CollaborationStatus.Forming;
            }

            return true;
        }

        /// <summary>
        /// 协作角色分配属性测试
        /// </summary>
        [Property(Arbitrary = new[] { typeof(CollaborationGenerators) })]
        [Category("Property")]
        public Property CollaborationRoles_ShouldBeAssignedAppropriately(
            List<ValidCharacterData> characterDataList)
        {
            return Prop.ForAll(
                Gen.Choose(2, 4).ToArbitrary(),
                (characterCount) =>
                {
                    try
                    {
                        var characters = characterDataList.Take(Math.Min(characterCount, 4)).ToList();
                        
                        if (characters.Count < 2)
                            return true;

                        // 创建具有不同技能水平的角色
                        var characterEntities = new List<CharacterEntity>();
                        for (int i = 0; i < characters.Count; i++)
                        {
                            var character = CreateTestCharacter(characters[i]);
                            
                            // 给第一个角色更高的建造技能（应该成为领导者）
                            if (character.Skills != null && i == 0)
                            {
                                character.Skills.SetSkillLevel(SkillType.Construction, 10);
                            }
                            
                            _characterSystem.RegisterCharacter(character);
                            characterEntities.Add(character);
                        }

                        // 执行自动协作分配
                        var coordinationResult = _collaborationSystem.AutoAssignCollaborativeTasks();

                        // 验证角色分配的合理性
                        return ValidateRoleAssignment(coordinationResult, characterEntities);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// 验证角色分配
        /// </summary>
        private bool ValidateRoleAssignment(TaskCoordinationResult result, List<CharacterEntity> characters)
        {
            foreach (var group in result.CreatedGroups)
            {
                var groupAssignments = result.Assignments.Where(a => a.TaskId == group.TaskId).ToList();
                
                if (groupAssignments.Count <= 1)
                    continue;

                // 应该有一个明确的领导者
                var leaders = groupAssignments.Where(a => a.Role == CollaborationRole.Leader).ToList();
                if (leaders.Count != 1)
                    return false;

                // 领导者应该是技能最高的角色之一
                var leaderCharacter = characters.FirstOrDefault(c => c.Id == leaders[0].CharacterId);
                if (leaderCharacter?.Skills != null)
                {
                    var leaderSkill = GetRelevantSkillLevel(leaderCharacter, group.Type);
                    var averageSkill = groupAssignments
                        .Select(a => characters.FirstOrDefault(c => c.Id == a.CharacterId))
                        .Where(c => c?.Skills != null)
                        .Average(c => GetRelevantSkillLevel(c!, group.Type));

                    // 领导者的技能应该不低于平均水平
                    if (leaderSkill < averageSkill * 0.8f)
                        return false;
                }
            }

            return true;
        }

        #region Helper Methods

        private CharacterEntity CreateTestCharacter(ValidCharacterData data)
        {
            var character = new CharacterEntity(_entityManager.CreateEntity())
            {
                Name = data.Name
            };

            // 设置基本组件
            var positionComponent = new PositionComponent(data.Position.ToVector3());
            var skillComponent = new SkillComponent();
            var needComponent = new NeedComponent();
            var inventoryComponent = new InventoryComponent();

            _entityManager.AddComponent(character.Id, positionComponent);
            _entityManager.AddComponent(character.Id, skillComponent);
            _entityManager.AddComponent(character.Id, needComponent);
            _entityManager.AddComponent(character.Id, inventoryComponent);

            character.SetComponentReferences(_entityManager);

            // 设置随机技能
            if (character.Skills != null)
            {
                character.Skills.SetSkillLevel(SkillType.Construction, data.ConstructionSkill);
                character.Skills.SetSkillLevel(SkillType.Mining, data.MiningSkill);
                character.Skills.SetSkillLevel(SkillType.Research, data.ResearchSkill);
            }

            return character;
        }

        private TaskDefinition CreateCollaborativeTaskDefinition(ValidTaskData data)
        {
            return new TaskDefinition
            {
                Name = data.Name,
                Type = data.Type,
                Priority = data.Priority,
                MaxAssignedCharacters = Math.Max(2, data.MaxCharacters),
                EstimatedDuration = data.Duration,
                TargetPosition = data.Position?.ToVector3(),
                WorkRadius = 3.0f
            };
        }

        private Vector3? GetTaskPosition(TaskId taskId)
        {
            var task = _taskSystem.GetTask(taskId);
            return task?.Definition.TargetPosition;
        }

        private float GetRelevantSkillLevel(CharacterEntity character, CollaborationType type)
        {
            if (character.Skills == null)
                return 0f;

            return type switch
            {
                CollaborationType.Construction => character.Skills.GetSkill(SkillType.Construction).Level,
                CollaborationType.Mining => character.Skills.GetSkill(SkillType.Mining).Level,
                CollaborationType.Research => character.Skills.GetSkill(SkillType.Research).Level,
                _ => character.Skills.GetAllSkills().Average(s => s.Level)
            };
        }

        #endregion
    }

    /// <summary>
    /// 协作测试数据生成器
    /// </summary>
    public static class CollaborationGenerators
    {
        public static Arbitrary<ValidCharacterData> ValidCharacterData()
        {
            return Gen.zip5(
                Gen.Elements("Alice", "Bob", "Charlie", "Diana", "Eve", "Frank"),
                ValidPosition().Generator,
                Gen.Choose(1, 10), // Construction skill
                Gen.Choose(1, 10), // Mining skill
                Gen.Choose(1, 10)  // Research skill
            ).Select(t => new ValidCharacterData
            {
                Name = t.Item1,
                Position = t.Item2,
                ConstructionSkill = t.Item3,
                MiningSkill = t.Item4,
                ResearchSkill = t.Item5
            }).ToArbitrary();
        }

        public static Arbitrary<ValidTaskData> ValidTaskData()
        {
            return Gen.zip6(
                Gen.Elements("Build Wall", "Mine Ore", "Research Tech", "Craft Items"),
                Gen.Elements(TaskType.Construction, TaskType.Mining, TaskType.Research),
                Gen.Elements(TaskPriority.Normal, TaskPriority.High),
                Gen.Choose(2, 4), // Max characters
                Gen.Choose(5.0f, 30.0f), // Duration
                ValidPosition().Generator.Select(p => (ValidPosition?)p)
            ).Select(t => new ValidTaskData
            {
                Name = t.Item1,
                Type = t.Item2,
                Priority = t.Item3,
                MaxCharacters = t.Item4,
                Duration = t.Item5,
                Position = t.Item6
            }).ToArbitrary();
        }

        public static Arbitrary<ValidPosition> ValidPosition()
        {
            return Gen.zip3(
                Gen.Choose(0, 20),
                Gen.Choose(0, 20),
                Gen.Choose(0, 5)
            ).Select(t => new ValidPosition { X = t.Item1, Y = t.Item2, Z = t.Item3 })
            .ToArbitrary();
        }
    }

    /// <summary>
    /// 有效的角色数据
    /// </summary>
    public class ValidCharacterData
    {
        public string Name { get; set; } = string.Empty;
        public ValidPosition Position { get; set; } = new();
        public int ConstructionSkill { get; set; }
        public int MiningSkill { get; set; }
        public int ResearchSkill { get; set; }
    }

    /// <summary>
    /// 有效的任务数据
    /// </summary>
    public class ValidTaskData
    {
        public string Name { get; set; } = string.Empty;
        public TaskType Type { get; set; }
        public TaskPriority Priority { get; set; }
        public int MaxCharacters { get; set; }
        public float Duration { get; set; }
        public ValidPosition? Position { get; set; }
    }

    /// <summary>
    /// 有效的位置
    /// </summary>
    public class ValidPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}