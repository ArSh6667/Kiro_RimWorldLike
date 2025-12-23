using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 协作系统 - 管理多人协作任务和冲突避免
    /// </summary>
    public class CollaborationSystem : IGameSystem
    {
        private readonly CollaborationManager _collaborationManager;
        private readonly TaskSystem _taskSystem;
        private readonly CharacterSystem _characterSystem;
        private float _updateInterval = 1.0f; // 每秒更新一次
        private float _timeSinceLastUpdate = 0f;

        public int Priority => 80; // 在任务系统和角色系统之后执行

        public CollaborationSystem(TaskSystem taskSystem, CharacterSystem characterSystem)
        {
            _taskSystem = taskSystem ?? throw new ArgumentNullException(nameof(taskSystem));
            _characterSystem = characterSystem ?? throw new ArgumentNullException(nameof(characterSystem));
            _collaborationManager = new CollaborationManager(_taskSystem.TaskManager);
        }

        public void Initialize()
        {
            // 订阅任务系统事件
            _taskSystem.SubscribeToTaskEvents(
                onTaskCreated: OnTaskCreated,
                onTaskCompleted: OnTaskCompleted,
                onTaskFailed: OnTaskFailed,
                onTaskCancelled: OnTaskCancelled
            );

            Console.WriteLine("协作系统已初始化");
        }

        public void Update(float deltaTime)
        {
            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                // 更新协作状态
                _collaborationManager.UpdateCollaborations(deltaTime);

                // 检查并创建新的协作机会
                CheckForCollaborationOpportunities();

                // 优化现有协作
                OptimizeActiveCollaborations();

                _timeSinceLastUpdate = 0f;
            }
        }

        public void Shutdown()
        {
            Console.WriteLine("协作系统已关闭");
        }

        /// <summary>
        /// 创建协作任务
        /// </summary>
        public CollaborationResult CreateCollaborativeTask(TaskDefinition definition, CollaborationType type)
        {
            // 确保任务支持多人协作
            if (definition.MaxAssignedCharacters <= 1)
            {
                definition.MaxAssignedCharacters = GetRecommendedParticipants(type);
            }

            var taskId = _taskSystem.CreateTask(definition);
            var group = _collaborationManager.CreateCollaborationGroup(taskId, type);

            return CollaborationResult.Success($"创建协作任务: {definition.Name}", group);
        }

        /// <summary>
        /// 自动分配协作任务
        /// </summary>
        public TaskCoordinationResult AutoAssignCollaborativeTasks()
        {
            var availableCharacters = _characterSystem.GetAllCharacters()
                .Where(c => IsCharacterAvailableForCollaboration(c))
                .ToList();

            var collaborativeTasks = _taskSystem.GetAvailableTasks()
                .Where(t => t.Definition.MaxAssignedCharacters > 1)
                .ToList();

            return _collaborationManager.CoordinateTaskAssignment(availableCharacters, collaborativeTasks);
        }

        /// <summary>
        /// 手动加入协作
        /// </summary>
        public CollaborationResult JoinCollaboration(TaskId taskId, uint characterId, CollaborationRole role = CollaborationRole.Worker)
        {
            return _collaborationManager.JoinCollaboration(taskId, characterId, role);
        }

        /// <summary>
        /// 离开协作
        /// </summary>
        public CollaborationResult LeaveCollaboration(TaskId taskId, uint characterId)
        {
            return _collaborationManager.LeaveCollaboration(taskId, characterId);
        }

        /// <summary>
        /// 预订工作区域
        /// </summary>
        public ResourceReservationResult ReserveWorkArea(Vector3 position, uint characterId, float duration = 300f)
        {
            return _collaborationManager.ReserveResource(
                position, 
                characterId, 
                ResourceType.WorkArea, 
                TimeSpan.FromSeconds(duration)
            );
        }

        /// <summary>
        /// 释放工作区域
        /// </summary>
        public bool ReleaseWorkArea(Vector3 position, uint characterId)
        {
            return _collaborationManager.ReleaseResourceReservation(position, characterId);
        }

        /// <summary>
        /// 检查位置冲突
        /// </summary>
        public ResourceConflictResult CheckPositionConflict(Vector3 position, uint characterId, float radius = 2.0f)
        {
            return _collaborationManager.CheckResourceConflict(position, characterId, radius);
        }

        /// <summary>
        /// 获取角色协作状态
        /// </summary>
        public CollaborationGroup? GetCharacterCollaboration(uint characterId)
        {
            return _collaborationManager.GetCharacterActiveCollaboration(characterId);
        }

        /// <summary>
        /// 获取推荐的协作伙伴
        /// </summary>
        public List<CharacterEntity> GetRecommendedCollaborators(uint characterId, TaskId taskId, int maxRecommendations = 3)
        {
            var character = _characterSystem.GetCharacter(characterId);
            var task = _taskSystem.GetTask(taskId);
            
            if (character == null || task == null)
                return new List<CharacterEntity>();

            var availableCharacters = _characterSystem.GetAllCharacters()
                .Where(c => c.Id != characterId && IsCharacterAvailableForCollaboration(c))
                .ToList();

            return availableCharacters
                .Select(c => new { Character = c, Score = CalculateCollaborationCompatibility(character, c, task) })
                .OrderByDescending(x => x.Score)
                .Take(maxRecommendations)
                .Select(x => x.Character)
                .ToList();
        }

        /// <summary>
        /// 强制重新分配协作
        /// </summary>
        public TaskCoordinationResult RebalanceCollaborations()
        {
            var characters = _characterSystem.GetAllCharacters().ToList();
            var tasks = _taskSystem.GetAvailableTasks()
                .Where(t => t.Definition.MaxAssignedCharacters > 1)
                .ToList();

            return _collaborationManager.CoordinateTaskAssignment(characters, tasks);
        }

        /// <summary>
        /// 获取协作效率报告
        /// </summary>
        public CollaborationEfficiencyReport GetEfficiencyReport()
        {
            var stats = _collaborationManager.GetStats();
            var taskStats = _taskSystem.GetStats();

            return new CollaborationEfficiencyReport
            {
                CollaborationStats = stats,
                TaskCompletionRate = taskStats.CompletedTasks > 0 
                    ? (float)taskStats.CompletedTasks / taskStats.TotalTasks 
                    : 0f,
                AverageCollaborationSize = stats.TotalCollaborationGroups > 0 
                    ? (float)stats.TotalParticipants / stats.TotalCollaborationGroups 
                    : 0f,
                ResourceUtilizationRate = CalculateResourceUtilization(),
                RecommendedOptimizations = GenerateOptimizationRecommendations(stats)
            };
        }

        #region Private Methods

        private void OnTaskCreated(ITask task)
        {
            // 检查是否需要创建协作组
            if (task.Definition.MaxAssignedCharacters > 1)
            {
                var type = DetermineCollaborationType(task);
                _collaborationManager.CreateCollaborationGroup(task.Id, type);
            }
        }

        private void OnTaskCompleted(ITask task)
        {
            // 协作组会在UpdateCollaborations中自动处理完成状态
        }

        private void OnTaskFailed(ITask task)
        {
            // 处理协作任务失败
            var group = _collaborationManager.GetCharacterActiveCollaboration(task.AssignedCharacters.FirstOrDefault());
            if (group != null)
            {
                Console.WriteLine($"协作任务失败: {task.Definition.Name}");
            }
        }

        private void OnTaskCancelled(ITask task)
        {
            // 处理协作任务取消
            foreach (var characterId in task.AssignedCharacters)
            {
                _collaborationManager.LeaveCollaboration(task.Id, characterId);
            }
        }

        private void CheckForCollaborationOpportunities()
        {
            var availableCharacters = _characterSystem.GetAllCharacters()
                .Where(IsCharacterAvailableForCollaboration)
                .ToList();

            var singlePersonTasks = _taskSystem.GetAvailableTasks()
                .Where(t => t.AssignedCharacters.Count() == 1 && CouldBenefitFromCollaboration(t))
                .ToList();

            foreach (var task in singlePersonTasks)
            {
                var assignedCharacter = _characterSystem.GetCharacter(task.AssignedCharacters.First());
                if (assignedCharacter == null) continue;

                var potentialCollaborators = GetRecommendedCollaborators(assignedCharacter.Id, task.Id, 2);
                if (potentialCollaborators.Any())
                {
                    // 建议创建协作组
                    Console.WriteLine($"建议为任务 {task.Definition.Name} 创建协作组");
                }
            }
        }

        private void OptimizeActiveCollaborations()
        {
            var stats = _collaborationManager.GetStats();
            
            // 如果协作效率低于阈值，进行优化
            if (stats.CollaborationEfficiency < 0.7f)
            {
                Console.WriteLine("协作效率较低，建议重新平衡");
            }
        }

        private bool IsCharacterAvailableForCollaboration(CharacterEntity character)
        {
            // 检查角色是否可用于协作
            if (character.Needs?.HasCriticalNeeds() == true)
                return false;

            var currentCollaboration = _collaborationManager.GetCharacterActiveCollaboration(character.Id);
            return currentCollaboration == null;
        }

        private bool CouldBenefitFromCollaboration(ITask task)
        {
            // 判断任务是否能从协作中受益
            return task.Definition.Type switch
            {
                TaskType.Construction => true,
                TaskType.Mining => task.Definition.EstimatedDuration > 10f,
                TaskType.Research => false, // 研究通常不需要协作
                _ => task.Definition.EstimatedDuration > 15f
            };
        }

        private float CalculateCollaborationCompatibility(CharacterEntity character1, CharacterEntity character2, ITask task)
        {
            float compatibility = 0f;

            // 技能互补性
            compatibility += CalculateSkillComplementarity(character1, character2, task);

            // 性格兼容性（简化）
            compatibility += CalculatePersonalityCompatibility(character1, character2);

            // 经验匹配度
            compatibility += CalculateExperienceMatch(character1, character2);

            return compatibility;
        }

        private float CalculateSkillComplementarity(CharacterEntity character1, CharacterEntity character2, ITask task)
        {
            if (character1.Skills == null || character2.Skills == null)
                return 0f;

            float complementarity = 0f;
            
            foreach (var requirement in task.Definition.SkillRequirements)
            {
                var skill1 = character1.Skills.GetSkill(requirement.SkillType).Level;
                var skill2 = character2.Skills.GetSkill(requirement.SkillType).Level;
                
                // 技能互补：一个高一个低比两个都中等要好
                var average = (skill1 + skill2) / 2f;
                var difference = Math.Abs(skill1 - skill2);
                
                complementarity += average + (difference * 0.1f);
            }

            return complementarity;
        }

        private float CalculatePersonalityCompatibility(CharacterEntity character1, CharacterEntity character2)
        {
            // 简化的性格兼容性计算
            return 50f; // 默认中等兼容性
        }

        private float CalculateExperienceMatch(CharacterEntity character1, CharacterEntity character2)
        {
            // 简化的经验匹配计算
            return 25f; // 默认经验匹配
        }

        private CollaborationType DetermineCollaborationType(ITask task)
        {
            return task.Definition.Type switch
            {
                TaskType.Construction => CollaborationType.Construction,
                TaskType.Research => CollaborationType.Research,
                TaskType.Mining => CollaborationType.Mining,
                _ => CollaborationType.General
            };
        }

        private int GetRecommendedParticipants(CollaborationType type)
        {
            return type switch
            {
                CollaborationType.Construction => 3,
                CollaborationType.Mining => 2,
                CollaborationType.Research => 2,
                CollaborationType.Defense => 4,
                _ => 2
            };
        }

        private float CalculateResourceUtilization()
        {
            var stats = _collaborationManager.GetStats();
            // 简化的资源利用率计算
            return stats.ActiveResourceReservations > 0 ? 0.8f : 0.5f;
        }

        private List<string> GenerateOptimizationRecommendations(CollaborationStats stats)
        {
            var recommendations = new List<string>();

            if (stats.CollaborationEfficiency < 0.5f)
            {
                recommendations.Add("协作效率较低，建议重新分配任务");
            }

            if (stats.ActiveResourceReservations > stats.TotalParticipants * 2)
            {
                recommendations.Add("资源预订过多，建议优化资源使用");
            }

            if (stats.ActiveCollaborationGroups == 0 && stats.TotalCollaborationGroups > 0)
            {
                recommendations.Add("没有活跃的协作组，建议检查任务分配");
            }

            return recommendations;
        }

        #endregion
    }

    /// <summary>
    /// 协作效率报告
    /// </summary>
    public class CollaborationEfficiencyReport
    {
        public CollaborationStats CollaborationStats { get; set; } = new();
        public float TaskCompletionRate { get; set; }
        public float AverageCollaborationSize { get; set; }
        public float ResourceUtilizationRate { get; set; }
        public List<string> RecommendedOptimizations { get; set; } = new();

        public override string ToString()
        {
            return $"协作效率报告: 完成率 {TaskCompletionRate:P}, " +
                   $"平均协作规模 {AverageCollaborationSize:F1}, " +
                   $"资源利用率 {ResourceUtilizationRate:P}, " +
                   $"{RecommendedOptimizations.Count} 项优化建议";
        }
    }
}