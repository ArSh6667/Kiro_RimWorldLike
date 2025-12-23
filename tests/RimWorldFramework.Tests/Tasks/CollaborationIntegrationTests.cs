using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Tests.Tasks
{
    /// <summary>
    /// 协作系统集成测试
    /// </summary>
    [TestFixture]
    public class CollaborationIntegrationTests
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

        [Test]
        public void CreateCollaborativeTask_ValidDefinition_CreatesCollaborationGroup()
        {
            // Arrange
            var definition = new TaskDefinition
            {
                Name = "Build Wall",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                MaxAssignedCharacters = 3,
                EstimatedDuration = 10.0f,
                TargetPosition = new Vector3(5, 5, 0)
            };

            // Act
            var result = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Group, Is.Not.Null);
            Assert.That(result.Group.Type, Is.EqualTo(CollaborationType.Construction));
            Assert.That(result.Group.MaxParticipants, Is.EqualTo(3));
        }

        [Test]
        public void JoinCollaboration_ValidCharacter_AddsToGroup()
        {
            // Arrange
            var character = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            _characterSystem.RegisterCharacter(character);

            var definition = CreateTestTaskDefinition("Build House", 2);
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            
            Assert.That(createResult.IsSuccess, Is.True);
            var taskId = createResult.Group!.TaskId;

            // Act
            var joinResult = _collaborationSystem.JoinCollaboration(taskId, character.Id, CollaborationRole.Leader);

            // Assert
            Assert.That(joinResult.IsSuccess, Is.True);
            
            var collaboration = _collaborationSystem.GetCharacterCollaboration(character.Id);
            Assert.That(collaboration, Is.Not.Null);
            Assert.That(collaboration.TaskId, Is.EqualTo(taskId));
            Assert.That(collaboration.Participants.Count, Is.EqualTo(1));
            Assert.That(collaboration.Participants[0].Role, Is.EqualTo(CollaborationRole.Leader));
        }

        [Test]
        public void JoinCollaboration_GroupFull_ReturnsFailure()
        {
            // Arrange
            var character1 = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            var character2 = CreateTestCharacter("Bob", new Vector3(1, 1, 0));
            var character3 = CreateTestCharacter("Charlie", new Vector3(2, 2, 0));
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);
            _characterSystem.RegisterCharacter(character3);

            var definition = CreateTestTaskDefinition("Small Task", 2); // 最多2人
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            var taskId = createResult.Group!.TaskId;

            // 添加两个角色
            _collaborationSystem.JoinCollaboration(taskId, character1.Id, CollaborationRole.Leader);
            _collaborationSystem.JoinCollaboration(taskId, character2.Id, CollaborationRole.Worker);

            // Act - 尝试添加第三个角色
            var result = _collaborationSystem.JoinCollaboration(taskId, character3.Id, CollaborationRole.Worker);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Message, Does.Contain("协作组已满"));
        }

        [Test]
        public void LeaveCollaboration_ValidCharacter_RemovesFromGroup()
        {
            // Arrange
            var character = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            _characterSystem.RegisterCharacter(character);

            var definition = CreateTestTaskDefinition("Test Task", 2);
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            var taskId = createResult.Group!.TaskId;

            _collaborationSystem.JoinCollaboration(taskId, character.Id, CollaborationRole.Leader);

            // Act
            var leaveResult = _collaborationSystem.LeaveCollaboration(taskId, character.Id);

            // Assert
            Assert.That(leaveResult.IsSuccess, Is.True);
            
            var collaboration = _collaborationSystem.GetCharacterCollaboration(character.Id);
            Assert.That(collaboration, Is.Null);
        }

        [Test]
        public void ReserveWorkArea_ValidPosition_CreatesReservation()
        {
            // Arrange
            var character = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            _characterSystem.RegisterCharacter(character);

            var position = new Vector3(5, 5, 0);

            // Act
            var result = _collaborationSystem.ReserveWorkArea(position, character.Id, 60f);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Reservation, Is.Not.Null);
            Assert.That(result.Reservation.Position, Is.EqualTo(position));
            Assert.That(result.Reservation.CharacterId, Is.EqualTo(character.Id));
        }

        [Test]
        public void ReserveWorkArea_ConflictingPosition_ReturnsFailure()
        {
            // Arrange
            var character1 = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            var character2 = CreateTestCharacter("Bob", new Vector3(1, 1, 0));
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);

            var position = new Vector3(5, 5, 0);

            // 第一个角色预订位置
            var firstResult = _collaborationSystem.ReserveWorkArea(position, character1.Id, 60f);
            Assert.That(firstResult.IsSuccess, Is.True);

            // Act - 第二个角色尝试预订同一位置
            var secondResult = _collaborationSystem.ReserveWorkArea(position, character2.Id, 60f);

            // Assert
            Assert.That(secondResult.IsSuccess, Is.False);
            Assert.That(secondResult.Message, Does.Contain("已被"));
        }

        [Test]
        public void CheckPositionConflict_ConflictingReservations_DetectsConflict()
        {
            // Arrange
            var character1 = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            var character2 = CreateTestCharacter("Bob", new Vector3(1, 1, 0));
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);

            var position1 = new Vector3(5, 5, 0);
            var position2 = new Vector3(5.5f, 5.5f, 0); // 接近的位置

            // 预订第一个位置
            _collaborationSystem.ReserveWorkArea(position1, character1.Id, 60f);

            // Act - 检查附近位置的冲突
            var conflictResult = _collaborationSystem.CheckPositionConflict(position2, character2.Id, 2.0f);

            // Assert
            Assert.That(conflictResult.HasConflict, Is.True);
            Assert.That(conflictResult.ConflictingReservations.Count, Is.EqualTo(1));
        }

        [Test]
        public void AutoAssignCollaborativeTasks_MultipleCharactersAndTasks_CoordinatesAssignments()
        {
            // Arrange
            var characters = new[]
            {
                CreateTestCharacter("Alice", new Vector3(0, 0, 0)),
                CreateTestCharacter("Bob", new Vector3(1, 1, 0)),
                CreateTestCharacter("Charlie", new Vector3(2, 2, 0))
            };

            foreach (var character in characters)
            {
                // 设置不同的技能水平
                if (character.Skills != null)
                {
                    character.Skills.SetSkillLevel(SkillType.Construction, 5 + Array.IndexOf(characters, character) * 2);
                }
                _characterSystem.RegisterCharacter(character);
            }

            // 创建多个协作任务
            var task1 = CreateTestTaskDefinition("Build Wall", 2);
            var task2 = CreateTestTaskDefinition("Build Door", 2);
            
            _collaborationSystem.CreateCollaborativeTask(task1, CollaborationType.Construction);
            _collaborationSystem.CreateCollaborativeTask(task2, CollaborationType.Construction);

            // Act
            var result = _collaborationSystem.AutoAssignCollaborativeTasks();

            // Assert
            Assert.That(result.CreatedGroups.Count, Is.GreaterThan(0));
            Assert.That(result.Assignments.Count, Is.GreaterThan(0));
            
            // 验证没有角色被重复分配（或有冲突解决方案）
            var characterAssignments = result.Assignments.GroupBy(a => a.CharacterId);
            foreach (var group in characterAssignments)
            {
                if (group.Count() > 1)
                {
                    // 应该有冲突解决方案
                    Assert.That(result.ConflictResolutions.Any(), Is.True);
                }
            }
        }

        [Test]
        public void GetRecommendedCollaborators_ValidCharacterAndTask_ReturnsCompatibleCharacters()
        {
            // Arrange
            var mainCharacter = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            var collaborator1 = CreateTestCharacter("Bob", new Vector3(1, 1, 0));
            var collaborator2 = CreateTestCharacter("Charlie", new Vector3(2, 2, 0));

            // 设置技能
            if (mainCharacter.Skills != null)
                mainCharacter.Skills.SetSkillLevel(SkillType.Construction, 8);
            
            if (collaborator1.Skills != null)
                collaborator1.Skills.SetSkillLevel(SkillType.Construction, 6);
            
            if (collaborator2.Skills != null)
                collaborator2.Skills.SetSkillLevel(SkillType.Construction, 4);

            _characterSystem.RegisterCharacter(mainCharacter);
            _characterSystem.RegisterCharacter(collaborator1);
            _characterSystem.RegisterCharacter(collaborator2);

            var definition = CreateTestTaskDefinition("Complex Build", 3);
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            var taskId = createResult.Group!.TaskId;

            // Act
            var recommendations = _collaborationSystem.GetRecommendedCollaborators(mainCharacter.Id, taskId, 2);

            // Assert
            Assert.That(recommendations.Count, Is.LessThanOrEqualTo(2));
            Assert.That(recommendations.All(c => c.Id != mainCharacter.Id), Is.True);
            
            // 技能更高的角色应该排在前面
            if (recommendations.Count > 1)
            {
                var firstSkill = recommendations[0].Skills?.GetSkill(SkillType.Construction).Level ?? 0;
                var secondSkill = recommendations[1].Skills?.GetSkill(SkillType.Construction).Level ?? 0;
                Assert.That(firstSkill, Is.GreaterThanOrEqualTo(secondSkill));
            }
        }

        [Test]
        public void GetEfficiencyReport_WithActiveCollaborations_ReturnsValidReport()
        {
            // Arrange
            var character1 = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            var character2 = CreateTestCharacter("Bob", new Vector3(1, 1, 0));
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);

            var definition = CreateTestTaskDefinition("Test Task", 2);
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            var taskId = createResult.Group!.TaskId;

            _collaborationSystem.JoinCollaboration(taskId, character1.Id, CollaborationRole.Leader);
            _collaborationSystem.JoinCollaboration(taskId, character2.Id, CollaborationRole.Worker);

            // Act
            var report = _collaborationSystem.GetEfficiencyReport();

            // Assert
            Assert.That(report, Is.Not.Null);
            Assert.That(report.CollaborationStats, Is.Not.Null);
            Assert.That(report.AverageCollaborationSize, Is.GreaterThan(0));
            Assert.That(report.RecommendedOptimizations, Is.Not.Null);
        }

        [Test]
        public void ReleaseWorkArea_ValidReservation_ReleasesSuccessfully()
        {
            // Arrange
            var character = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            _characterSystem.RegisterCharacter(character);

            var position = new Vector3(5, 5, 0);
            var reserveResult = _collaborationSystem.ReserveWorkArea(position, character.Id, 60f);
            Assert.That(reserveResult.IsSuccess, Is.True);

            // Act
            var releaseResult = _collaborationSystem.ReleaseWorkArea(position, character.Id);

            // Assert
            Assert.That(releaseResult, Is.True);
            
            // 验证位置不再有冲突
            var conflictResult = _collaborationSystem.CheckPositionConflict(position, character.Id);
            Assert.That(conflictResult.HasConflict, Is.False);
        }

        [Test]
        public void Update_WithActiveCollaborations_UpdatesCorrectly()
        {
            // Arrange
            var character = CreateTestCharacter("Alice", new Vector3(0, 0, 0));
            _characterSystem.RegisterCharacter(character);

            var definition = CreateTestTaskDefinition("Test Task", 1);
            var createResult = _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            var taskId = createResult.Group!.TaskId;

            _collaborationSystem.JoinCollaboration(taskId, character.Id, CollaborationRole.Leader);

            // Act
            _collaborationSystem.Update(1.0f); // 更新1秒

            // Assert - 系统应该正常运行而不抛出异常
            var collaboration = _collaborationSystem.GetCharacterCollaboration(character.Id);
            Assert.That(collaboration, Is.Not.Null);
        }

        #region Helper Methods

        private CharacterEntity CreateTestCharacter(string name, Vector3 position)
        {
            var character = new CharacterEntity(_entityManager.CreateEntity())
            {
                Name = name
            };

            var positionComponent = new PositionComponent(position);
            var skillComponent = new SkillComponent();
            var needComponent = new NeedComponent();
            var inventoryComponent = new InventoryComponent();

            _entityManager.AddComponent(character.Id, positionComponent);
            _entityManager.AddComponent(character.Id, skillComponent);
            _entityManager.AddComponent(character.Id, needComponent);
            _entityManager.AddComponent(character.Id, inventoryComponent);

            character.SetComponentReferences(_entityManager);

            return character;
        }

        private TaskDefinition CreateTestTaskDefinition(string name, int maxCharacters)
        {
            return new TaskDefinition
            {
                Name = name,
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                MaxAssignedCharacters = maxCharacters,
                EstimatedDuration = 10.0f,
                TargetPosition = new Vector3(5, 5, 0),
                WorkRadius = 2.0f
            };
        }

        #endregion
    }
}