using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 智能任务分配器
    /// </summary>
    public class TaskAssigner
    {
        private readonly TaskManager _taskManager;
        private readonly ITaskValidator _validator;

        public TaskAssigner(TaskManager taskManager, ITaskValidator? validator = null)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
            _validator = validator ?? new DefaultTaskValidator();
        }

        /// <summary>
        /// 为角色分配最佳任务
        /// </summary>
        public TaskAssignmentResult AssignBestTask(CharacterEntity character)
        {
            if (character == null)
                return TaskAssignmentResult.Failure("角色不能为空");

            var availableTasks = _taskManager.GetAvailableTasks().ToList();
            if (!availableTasks.Any())
                return TaskAssignmentResult.Failure("没有可用任务");

            // 筛选角色可以执行的任务
            var suitableTasks = availableTasks
                .Where(task => task.CanExecute(character))
                .ToList();

            if (!suitableTasks.Any())
                return TaskAssignmentResult.Failure("没有适合的任务");

            // 计算每个任务的适合度并排序
            var taskScores = suitableTasks
                .Select(task => new TaskScore
                {
                    Task = task,
                    Score = CalculateTaskScore(task, character)
                })
                .OrderByDescending(ts => ts.Score)
                .ToList();

            // 尝试分配最佳任务
            foreach (var taskScore in taskScores)
            {
                var validationResult = _validator.ValidateAssignment(taskScore.Task, character);
                if (validationResult.IsValid)
                {
                    if (taskScore.Task.AssignCharacter(character.Id))
                    {
                        return TaskAssignmentResult.Success(taskScore.Task, taskScore.Score);
                    }
                }
            }

            return TaskAssignmentResult.Failure("无法分配任何任务");
        }

        /// <summary>
        /// 为多个角色批量分配任务
        /// </summary>
        public List<TaskAssignmentResult> AssignTasks(IEnumerable<CharacterEntity> characters)
        {
            var results = new List<TaskAssignmentResult>();
            var characterList = characters.ToList();

            // 按角色能力排序，优先分配给能力强的角色
            var sortedCharacters = characterList
                .OrderByDescending(c => GetCharacterOverallSkill(c))
                .ToList();

            foreach (var character in sortedCharacters)
            {
                var result = AssignBestTask(character);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 重新分配所有任务
        /// </summary>
        public TaskReassignmentResult ReassignAllTasks(IEnumerable<CharacterEntity> characters)
        {
            var result = new TaskReassignmentResult();
            
            // 取消所有当前分配
            var assignedTasks = _taskManager.GetTasksByStatus(TaskStatus.Assigned).ToList();
            foreach (var task in assignedTasks)
            {
                var assignedCharacters = task.AssignedCharacters.ToList();
                foreach (var characterId in assignedCharacters)
                {
                    task.UnassignCharacter(characterId);
                    result.UnassignedTasks++;
                }
            }

            // 重新分配
            var assignmentResults = AssignTasks(characters);
            result.AssignmentResults = assignmentResults;
            result.SuccessfulAssignments = assignmentResults.Count(r => r.IsSuccess);
            result.FailedAssignments = assignmentResults.Count(r => !r.IsSuccess);

            return result;
        }

        /// <summary>
        /// 获取任务推荐列表
        /// </summary>
        public List<TaskRecommendation> GetTaskRecommendations(CharacterEntity character, int maxRecommendations = 5)
        {
            if (character == null) return new List<TaskRecommendation>();

            var availableTasks = _taskManager.GetAvailableTasks()
                .Where(task => task.CanExecute(character))
                .ToList();

            var recommendations = availableTasks
                .Select(task => new TaskRecommendation
                {
                    Task = task,
                    Score = CalculateTaskScore(task, character),
                    Reason = GenerateRecommendationReason(task, character)
                })
                .OrderByDescending(r => r.Score)
                .Take(maxRecommendations)
                .ToList();

            return recommendations;
        }

        /// <summary>
        /// 计算任务适合度分数
        /// </summary>
        private float CalculateTaskScore(ITask task, CharacterEntity character)
        {
            float score = 0f;

            // 基础优先级分数
            score += GetPriorityScore(task.Definition.Priority);

            // 技能匹配分数
            score += CalculateSkillMatchScore(task, character);

            // 距离分数
            score += CalculateDistanceScore(task, character);

            // 需求状态分数
            score += CalculateNeedScore(task, character);

            // 任务紧急程度分数
            score += CalculateUrgencyScore(task);

            return Math.Max(0f, score);
        }

        private float GetPriorityScore(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Critical => 100f,
                TaskPriority.High => 75f,
                TaskPriority.Normal => 50f,
                TaskPriority.Low => 25f,
                TaskPriority.Idle => 10f,
                _ => 0f
            };
        }

        private float CalculateSkillMatchScore(ITask task, CharacterEntity character)
        {
            if (character.Skills == null) return 0f;

            float totalScore = 0f;
            float totalWeight = 0f;

            foreach (var requirement in task.Definition.SkillRequirements)
            {
                var skill = character.Skills.GetSkill(requirement.SkillType);
                var skillScore = Math.Min(100f, (float)skill.Level / requirement.MinLevel * 50f);
                
                totalScore += skillScore * requirement.Weight;
                totalWeight += requirement.Weight;
            }

            return totalWeight > 0 ? totalScore / totalWeight : 25f; // 默认分数
        }

        private float CalculateDistanceScore(ITask task, CharacterEntity character)
        {
            if (!task.Definition.TargetPosition.HasValue || character.Position == null)
                return 25f; // 默认分数

            var distance = character.Position.DistanceTo(task.Definition.TargetPosition.Value);
            var maxDistance = 100f; // 最大考虑距离
            
            return Math.Max(0f, 25f * (1f - distance / maxDistance));
        }

        private float CalculateNeedScore(ITask task, CharacterEntity character)
        {
            if (character.Needs == null) return 25f;

            // 如果角色有关键需求，降低分数
            var criticalNeeds = character.Needs.GetCriticalNeeds().ToList();
            if (criticalNeeds.Any())
            {
                return 5f; // 大幅降低分数
            }

            var happiness = character.Needs.GetOverallHappiness();
            return happiness * 25f; // 幸福度越高，分数越高
        }

        private float CalculateUrgencyScore(ITask task)
        {
            var remainingTime = task.Definition.GetRemainingTime();
            if (!remainingTime.HasValue) return 10f;

            var urgency = Math.Max(0f, 1f - (float)(remainingTime.Value.TotalHours / 24)); // 24小时内的紧急程度
            return urgency * 20f;
        }

        private float GetCharacterOverallSkill(CharacterEntity character)
        {
            if (character.Skills == null) return 0f;

            return character.Skills.GetAllSkills()
                .Where(s => !s.IsDisabled)
                .Average(s => s.Level);
        }

        private string GenerateRecommendationReason(ITask task, CharacterEntity character)
        {
            var reasons = new List<string>();

            // 优先级原因
            if (task.Definition.Priority <= TaskPriority.High)
            {
                reasons.Add($"高优先级任务 ({task.Definition.Priority})");
            }

            // 技能匹配原因
            if (character.Skills != null)
            {
                var bestSkill = task.Definition.SkillRequirements
                    .Where(req => character.Skills.GetSkill(req.SkillType).Level >= req.MinLevel)
                    .OrderByDescending(req => character.Skills.GetSkill(req.SkillType).Level - req.MinLevel)
                    .FirstOrDefault();

                if (bestSkill != null)
                {
                    var skill = character.Skills.GetSkill(bestSkill.SkillType);
                    reasons.Add($"擅长 {bestSkill.SkillType} (等级 {skill.Level})");
                }
            }

            // 距离原因
            if (task.Definition.TargetPosition.HasValue && character.Position != null)
            {
                var distance = character.Position.DistanceTo(task.Definition.TargetPosition.Value);
                if (distance < 10f)
                {
                    reasons.Add("距离较近");
                }
            }

            return reasons.Any() ? string.Join(", ", reasons) : "一般适合";
        }

        /// <summary>
        /// 任务分数内部类
        /// </summary>
        private class TaskScore
        {
            public ITask Task { get; set; } = null!;
            public float Score { get; set; }
        }
    }

    /// <summary>
    /// 任务分配结果
    /// </summary>
    public class TaskAssignmentResult
    {
        public bool IsSuccess { get; set; }
        public ITask? AssignedTask { get; set; }
        public float Score { get; set; }
        public string Message { get; set; } = string.Empty;

        public static TaskAssignmentResult Success(ITask task, float score)
        {
            return new TaskAssignmentResult
            {
                IsSuccess = true,
                AssignedTask = task,
                Score = score,
                Message = $"成功分配任务: {task.Definition.Name}"
            };
        }

        public static TaskAssignmentResult Failure(string message)
        {
            return new TaskAssignmentResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// 任务重新分配结果
    /// </summary>
    public class TaskReassignmentResult
    {
        public int UnassignedTasks { get; set; }
        public int SuccessfulAssignments { get; set; }
        public int FailedAssignments { get; set; }
        public List<TaskAssignmentResult> AssignmentResults { get; set; } = new();

        public override string ToString()
        {
            return $"取消分配: {UnassignedTasks}, 成功分配: {SuccessfulAssignments}, 失败: {FailedAssignments}";
        }
    }

    /// <summary>
    /// 任务推荐
    /// </summary>
    public class TaskRecommendation
    {
        public ITask Task { get; set; } = null!;
        public float Score { get; set; }
        public string Reason { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Task.Definition.Name} (分数: {Score:F1}, 原因: {Reason})";
        }
    }
}