using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 协作任务管理器 - 管理多人协作任务和资源冲突
    /// </summary>
    public class CollaborationManager
    {
        private readonly TaskManager _taskManager;
        private readonly Dictionary<TaskId, CollaborationGroup> _collaborationGroups = new();
        private readonly Dictionary<Vector3, ResourceReservation> _resourceReservations = new();
        private readonly Dictionary<uint, CollaborationState> _characterStates = new();

        public CollaborationManager(TaskManager taskManager)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        }

        /// <summary>
        /// 创建协作任务组
        /// </summary>
        public CollaborationGroup CreateCollaborationGroup(TaskId taskId, CollaborationType type)
        {
            var task = _taskManager.GetTask(taskId);
            if (task == null)
                throw new ArgumentException($"任务 {taskId} 不存在", nameof(taskId));

            var group = new CollaborationGroup
            {
                TaskId = taskId,
                Type = type,
                CreatedTime = DateTime.Now,
                Status = CollaborationStatus.Forming,
                MaxParticipants = task.Definition.MaxAssignedCharacters,
                RequiredSkills = task.Definition.SkillRequirements.ToList()
            };

            _collaborationGroups[taskId] = group;
            return group;
        }

        /// <summary>
        /// 将角色加入协作组
        /// </summary>
        public CollaborationResult JoinCollaboration(TaskId taskId, uint characterId, CollaborationRole role)
        {
            if (!_collaborationGroups.TryGetValue(taskId, out var group))
                return CollaborationResult.Failure("协作组不存在");

            if (group.Participants.Count >= group.MaxParticipants)
                return CollaborationResult.Failure("协作组已满");

            if (group.Participants.Any(p => p.CharacterId == characterId))
                return CollaborationResult.Failure("角色已在协作组中");

            // 检查角色是否已参与其他冲突的协作
            var existingCollaboration = GetCharacterActiveCollaboration(characterId);
            if (existingCollaboration != null)
            {
                var conflict = CheckCollaborationConflict(group, existingCollaboration);
                if (conflict.HasConflict)
                    return CollaborationResult.Failure($"与现有协作冲突: {conflict.ConflictReason}");
            }

            var participant = new CollaborationParticipant
            {
                CharacterId = characterId,
                Role = role,
                JoinTime = DateTime.Now,
                Status = ParticipantStatus.Active
            };

            group.Participants.Add(participant);
            
            // 更新角色协作状态
            _characterStates[characterId] = new CollaborationState
            {
                CharacterId = characterId,
                CurrentTaskId = taskId,
                Role = role,
                JoinTime = DateTime.Now
            };

            // 检查是否可以开始协作
            if (CanStartCollaboration(group))
            {
                group.Status = CollaborationStatus.Active;
                group.StartTime = DateTime.Now;
            }

            return CollaborationResult.Success($"成功加入协作组，角色: {role}");
        }

        /// <summary>
        /// 离开协作组
        /// </summary>
        public CollaborationResult LeaveCollaboration(TaskId taskId, uint characterId)
        {
            if (!_collaborationGroups.TryGetValue(taskId, out var group))
                return CollaborationResult.Failure("协作组不存在");

            var participant = group.Participants.FirstOrDefault(p => p.CharacterId == characterId);
            if (participant == null)
                return CollaborationResult.Failure("角色不在协作组中");

            group.Participants.Remove(participant);
            _characterStates.Remove(characterId);

            // 检查协作组是否还能继续
            if (!CanContinueCollaboration(group))
            {
                group.Status = CollaborationStatus.Suspended;
                NotifyCollaborationSuspended(group);
            }

            return CollaborationResult.Success("成功离开协作组");
        }

        /// <summary>
        /// 预订资源
        /// </summary>
        public ResourceReservationResult ReserveResource(Vector3 position, uint characterId, ResourceType resourceType, TimeSpan duration)
        {
            var key = NormalizePosition(position);
            
            if (_resourceReservations.TryGetValue(key, out var existingReservation))
            {
                if (existingReservation.IsActive && existingReservation.CharacterId != characterId)
                {
                    return ResourceReservationResult.Failure($"资源已被角色 {existingReservation.CharacterId} 预订");
                }
            }

            var reservation = new ResourceReservation
            {
                Position = position,
                CharacterId = characterId,
                ResourceType = resourceType,
                ReservationTime = DateTime.Now,
                ExpirationTime = DateTime.Now.Add(duration),
                IsActive = true
            };

            _resourceReservations[key] = reservation;
            return ResourceReservationResult.Success(reservation);
        }

        /// <summary>
        /// 释放资源预订
        /// </summary>
        public bool ReleaseResourceReservation(Vector3 position, uint characterId)
        {
            var key = NormalizePosition(position);
            
            if (_resourceReservations.TryGetValue(key, out var reservation))
            {
                if (reservation.CharacterId == characterId)
                {
                    reservation.IsActive = false;
                    _resourceReservations.Remove(key);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查资源冲突
        /// </summary>
        public ResourceConflictResult CheckResourceConflict(Vector3 position, uint characterId, float radius = 2.0f)
        {
            var conflicts = new List<ResourceReservation>();
            var normalizedPos = NormalizePosition(position);

            foreach (var kvp in _resourceReservations)
            {
                var reservation = kvp.Value;
                if (!reservation.IsActive || reservation.CharacterId == characterId)
                    continue;

                var distance = Vector3.Distance(position, reservation.Position);
                if (distance <= radius)
                {
                    conflicts.Add(reservation);
                }
            }

            return new ResourceConflictResult
            {
                HasConflict = conflicts.Any(),
                ConflictingReservations = conflicts,
                ConflictRadius = radius
            };
        }

        /// <summary>
        /// 协调任务分配
        /// </summary>
        public TaskCoordinationResult CoordinateTaskAssignment(IEnumerable<CharacterEntity> characters, IEnumerable<ITask> tasks)
        {
            var result = new TaskCoordinationResult();
            var characterList = characters.ToList();
            var taskList = tasks.ToList();

            // 识别需要协作的任务
            var collaborativeTasks = taskList
                .Where(t => t.Definition.MaxAssignedCharacters > 1)
                .ToList();

            // 为协作任务创建协作组
            foreach (var task in collaborativeTasks)
            {
                var group = CreateCollaborationGroup(task.Id, DetermineCollaborationType(task));
                result.CreatedGroups.Add(group);
            }

            // 智能分配角色到协作组
            var assignments = AssignCharactersToCollaborations(characterList, collaborativeTasks);
            result.Assignments = assignments;

            // 检测和解决冲突
            var conflicts = DetectAssignmentConflicts(assignments);
            var resolutions = ResolveConflicts(conflicts);
            result.ConflictResolutions = resolutions;

            return result;
        }

        /// <summary>
        /// 更新协作状态
        /// </summary>
        public void UpdateCollaborations(float deltaTime)
        {
            var currentTime = DateTime.Now;

            // 更新协作组状态
            foreach (var group in _collaborationGroups.Values.ToList())
            {
                UpdateCollaborationGroup(group, currentTime);
            }

            // 清理过期的资源预订
            CleanupExpiredReservations(currentTime);
        }

        /// <summary>
        /// 获取角色当前协作
        /// </summary>
        public CollaborationGroup? GetCharacterActiveCollaboration(uint characterId)
        {
            if (!_characterStates.TryGetValue(characterId, out var state))
                return null;

            return _collaborationGroups.TryGetValue(state.CurrentTaskId, out var group) ? group : null;
        }

        /// <summary>
        /// 获取协作统计信息
        /// </summary>
        public CollaborationStats GetStats()
        {
            var activeGroups = _collaborationGroups.Values.Count(g => g.Status == CollaborationStatus.Active);
            var totalParticipants = _collaborationGroups.Values.Sum(g => g.Participants.Count);
            var activeReservations = _resourceReservations.Values.Count(r => r.IsActive);

            return new CollaborationStats
            {
                TotalCollaborationGroups = _collaborationGroups.Count,
                ActiveCollaborationGroups = activeGroups,
                TotalParticipants = totalParticipants,
                ActiveResourceReservations = activeReservations,
                CharactersInCollaboration = _characterStates.Count
            };
        }

        #region Private Methods

        private bool CanStartCollaboration(CollaborationGroup group)
        {
            // 检查是否有足够的参与者
            if (group.Participants.Count < GetMinimumParticipants(group.Type))
                return false;

            // 检查是否有必要的角色
            return HasRequiredRoles(group);
        }

        private bool CanContinueCollaboration(CollaborationGroup group)
        {
            return group.Participants.Count >= GetMinimumParticipants(group.Type);
        }

        private int GetMinimumParticipants(CollaborationType type)
        {
            return type switch
            {
                CollaborationType.Construction => 2,
                CollaborationType.Research => 1,
                CollaborationType.Mining => 1,
                CollaborationType.Defense => 2,
                _ => 1
            };
        }

        private bool HasRequiredRoles(CollaborationGroup group)
        {
            // 根据协作类型检查必要角色
            return group.Type switch
            {
                CollaborationType.Construction => group.Participants.Any(p => p.Role == CollaborationRole.Leader),
                CollaborationType.Research => group.Participants.Any(p => p.Role == CollaborationRole.Specialist),
                _ => true
            };
        }

        private CollaborationConflict CheckCollaborationConflict(CollaborationGroup group1, CollaborationGroup group2)
        {
            var conflict = new CollaborationConflict();

            // 检查时间冲突
            if (HasTimeConflict(group1, group2))
            {
                conflict.HasConflict = true;
                conflict.ConflictReason = "时间冲突";
                return conflict;
            }

            // 检查资源冲突
            if (HasResourceConflict(group1, group2))
            {
                conflict.HasConflict = true;
                conflict.ConflictReason = "资源冲突";
                return conflict;
            }

            return conflict;
        }

        private bool HasTimeConflict(CollaborationGroup group1, CollaborationGroup group2)
        {
            // 简化的时间冲突检测
            return group1.Status == CollaborationStatus.Active && group2.Status == CollaborationStatus.Active;
        }

        private bool HasResourceConflict(CollaborationGroup group1, CollaborationGroup group2)
        {
            var task1 = _taskManager.GetTask(group1.TaskId);
            var task2 = _taskManager.GetTask(group2.TaskId);

            if (task1?.Definition.TargetPosition == null || task2?.Definition.TargetPosition == null)
                return false;

            var distance = Vector3.Distance(task1.Definition.TargetPosition.Value, task2.Definition.TargetPosition.Value);
            return distance < 5.0f; // 5单位内认为有资源冲突
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

        private List<CollaborationAssignment> AssignCharactersToCollaborations(List<CharacterEntity> characters, List<ITask> tasks)
        {
            var assignments = new List<CollaborationAssignment>();

            foreach (var task in tasks)
            {
                var suitableCharacters = characters
                    .Where(c => task.CanExecute(c))
                    .OrderByDescending(c => CalculateCollaborationScore(c, task))
                    .Take(task.Definition.MaxAssignedCharacters)
                    .ToList();

                for (int i = 0; i < suitableCharacters.Count; i++)
                {
                    var character = suitableCharacters[i];
                    var role = i == 0 ? CollaborationRole.Leader : CollaborationRole.Worker;

                    assignments.Add(new CollaborationAssignment
                    {
                        TaskId = task.Id,
                        CharacterId = character.Id,
                        Role = role,
                        Score = CalculateCollaborationScore(character, task)
                    });
                }
            }

            return assignments;
        }

        private float CalculateCollaborationScore(CharacterEntity character, ITask task)
        {
            float score = 0f;

            // 技能匹配分数
            if (character.Skills != null)
            {
                foreach (var requirement in task.Definition.SkillRequirements)
                {
                    var skill = character.Skills.GetSkill(requirement.SkillType);
                    if (skill.Level >= requirement.MinLevel)
                    {
                        score += skill.Level * requirement.Weight;
                    }
                }
            }

            // 协作经验分数（简化）
            score += GetCollaborationExperience(character.Id) * 10f;

            return score;
        }

        private float GetCollaborationExperience(uint characterId)
        {
            // 简化的协作经验计算
            return 1.0f; // 默认经验值
        }

        private List<AssignmentConflict> DetectAssignmentConflicts(List<CollaborationAssignment> assignments)
        {
            var conflicts = new List<AssignmentConflict>();

            // 检测角色重复分配
            var characterAssignments = assignments.GroupBy(a => a.CharacterId);
            foreach (var group in characterAssignments)
            {
                if (group.Count() > 1)
                {
                    conflicts.Add(new AssignmentConflict
                    {
                        Type = ConflictType.CharacterOverassignment,
                        CharacterId = group.Key,
                        ConflictingAssignments = group.ToList()
                    });
                }
            }

            return conflicts;
        }

        private List<ConflictResolution> ResolveConflicts(List<AssignmentConflict> conflicts)
        {
            var resolutions = new List<ConflictResolution>();

            foreach (var conflict in conflicts)
            {
                if (conflict.Type == ConflictType.CharacterOverassignment)
                {
                    // 选择分数最高的分配
                    var bestAssignment = conflict.ConflictingAssignments
                        .OrderByDescending(a => a.Score)
                        .First();

                    resolutions.Add(new ConflictResolution
                    {
                        ConflictType = conflict.Type,
                        Resolution = $"保留分数最高的分配: 任务 {bestAssignment.TaskId}",
                        SelectedAssignment = bestAssignment
                    });
                }
            }

            return resolutions;
        }

        private void UpdateCollaborationGroup(CollaborationGroup group, DateTime currentTime)
        {
            // 检查协作组是否应该完成
            var task = _taskManager.GetTask(group.TaskId);
            if (task?.Status == TaskStatus.Completed)
            {
                group.Status = CollaborationStatus.Completed;
                group.EndTime = currentTime;
                
                // 清理参与者状态
                foreach (var participant in group.Participants)
                {
                    _characterStates.Remove(participant.CharacterId);
                }
            }
        }

        private void CleanupExpiredReservations(DateTime currentTime)
        {
            var expiredKeys = _resourceReservations
                .Where(kvp => kvp.Value.ExpirationTime <= currentTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _resourceReservations.Remove(key);
            }
        }

        private void NotifyCollaborationSuspended(CollaborationGroup group)
        {
            // 通知相关系统协作被暂停
            Console.WriteLine($"协作组 {group.TaskId} 被暂停，参与者不足");
        }

        private Vector3 NormalizePosition(Vector3 position)
        {
            // 将位置标准化到网格点，用于资源预订
            return new Vector3(
                (float)Math.Floor(position.X),
                (float)Math.Floor(position.Y),
                (float)Math.Floor(position.Z)
            );
        }

        #endregion
    }
}