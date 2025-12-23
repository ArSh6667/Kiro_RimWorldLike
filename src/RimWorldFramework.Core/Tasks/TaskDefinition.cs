using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务ID类型
    /// </summary>
    public readonly struct TaskId : IEquatable<TaskId>
    {
        public uint Value { get; }

        public TaskId(uint value)
        {
            Value = value;
        }

        public bool Equals(TaskId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is TaskId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"Task_{Value}";

        public static implicit operator TaskId(uint value) => new(value);
        public static implicit operator uint(TaskId taskId) => taskId.Value;
        
        public static bool operator ==(TaskId left, TaskId right) => left.Equals(right);
        public static bool operator !=(TaskId left, TaskId right) => !left.Equals(right);
    }

    /// <summary>
    /// 任务类型枚举
    /// </summary>
    public enum TaskType
    {
        Construction,   // 建造
        Mining,         // 挖掘
        Growing,        // 种植
        Cooking,        // 烹饪
        Crafting,       // 制作
        Research,       // 研究
        Hauling,        // 搬运
        Cleaning,       // 清洁
        Hunting,        // 狩猎
        Social,         // 社交
        Medical,        // 医疗
        Art,            // 艺术
        Maintenance,    // 维护
        Defense         // 防御
    }

    /// <summary>
    /// 任务优先级枚举
    /// </summary>
    public enum TaskPriority
    {
        Critical = 1,   // 关键 - 立即执行
        High = 2,       // 高 - 优先执行
        Normal = 3,     // 普通 - 正常执行
        Low = 4,        // 低 - 有空时执行
        Idle = 5        // 空闲 - 无事可做时执行
    }

    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        Pending,        // 等待中
        Available,      // 可执行
        Assigned,       // 已分配
        InProgress,     // 执行中
        Completed,      // 已完成
        Failed,         // 失败
        Cancelled,      // 已取消
        Blocked         // 被阻塞
    }

    /// <summary>
    /// 任务执行结果
    /// </summary>
    public enum TaskResult
    {
        Success,        // 成功
        Failure,        // 失败
        InProgress,     // 进行中
        Cancelled,      // 取消
        Blocked         // 阻塞
    }

    /// <summary>
    /// 任务技能需求
    /// </summary>
    public class TaskSkillRequirement
    {
        public SkillType SkillType { get; set; }
        public int MinLevel { get; set; }
        public float Weight { get; set; } = 1.0f; // 权重，用于计算适合度

        public TaskSkillRequirement(SkillType skillType, int minLevel, float weight = 1.0f)
        {
            SkillType = skillType;
            MinLevel = minLevel;
            Weight = weight;
        }
    }

    /// <summary>
    /// 任务定义 - 描述任务的基本信息和要求
    /// </summary>
    public class TaskDefinition
    {
        public TaskId Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskType Type { get; set; }
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        
        // 位置相关
        public Vector3? TargetPosition { get; set; }
        public float WorkRadius { get; set; } = 1.0f;
        
        // 技能需求
        public List<TaskSkillRequirement> SkillRequirements { get; set; } = new();
        
        // 依赖关系
        public List<TaskId> Prerequisites { get; set; } = new();
        public List<TaskId> Dependents { get; set; } = new();
        
        // 时间估算
        public float EstimatedDuration { get; set; } = 1.0f;
        public float MaxDuration { get; set; } = float.MaxValue;
        
        // 资源需求
        public Dictionary<string, int> RequiredItems { get; set; } = new();
        public Dictionary<string, int> ProducedItems { get; set; } = new();
        
        // 其他属性
        public bool CanBeInterrupted { get; set; } = true;
        public bool RequiresTools { get; set; } = false;
        public int MaxAssignedCharacters { get; set; } = 1;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime? Deadline { get; set; }
        
        // 自定义属性
        public Dictionary<string, object> CustomProperties { get; set; } = new();

        /// <summary>
        /// 添加技能需求
        /// </summary>
        public void AddSkillRequirement(SkillType skillType, int minLevel, float weight = 1.0f)
        {
            SkillRequirements.Add(new TaskSkillRequirement(skillType, minLevel, weight));
        }

        /// <summary>
        /// 添加前置任务
        /// </summary>
        public void AddPrerequisite(TaskId prerequisiteId)
        {
            if (!Prerequisites.Contains(prerequisiteId))
            {
                Prerequisites.Add(prerequisiteId);
            }
        }

        /// <summary>
        /// 添加依赖任务
        /// </summary>
        public void AddDependent(TaskId dependentId)
        {
            if (!Dependents.Contains(dependentId))
            {
                Dependents.Add(dependentId);
            }
        }

        /// <summary>
        /// 检查是否有技能需求
        /// </summary>
        public bool HasSkillRequirement(SkillType skillType)
        {
            return SkillRequirements.Exists(req => req.SkillType == skillType);
        }

        /// <summary>
        /// 获取技能需求的最低等级
        /// </summary>
        public int GetMinSkillLevel(SkillType skillType)
        {
            var requirement = SkillRequirements.Find(req => req.SkillType == skillType);
            return requirement?.MinLevel ?? 0;
        }

        /// <summary>
        /// 检查是否过期
        /// </summary>
        public bool IsExpired()
        {
            return Deadline.HasValue && DateTime.Now > Deadline.Value;
        }

        /// <summary>
        /// 获取剩余时间
        /// </summary>
        public TimeSpan? GetRemainingTime()
        {
            if (!Deadline.HasValue) return null;
            var remaining = Deadline.Value - DateTime.Now;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// 克隆任务定义
        /// </summary>
        public TaskDefinition Clone()
        {
            return new TaskDefinition
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Type = Type,
                Priority = Priority,
                TargetPosition = TargetPosition,
                WorkRadius = WorkRadius,
                SkillRequirements = new List<TaskSkillRequirement>(SkillRequirements),
                Prerequisites = new List<TaskId>(Prerequisites),
                Dependents = new List<TaskId>(Dependents),
                EstimatedDuration = EstimatedDuration,
                MaxDuration = MaxDuration,
                RequiredItems = new Dictionary<string, int>(RequiredItems),
                ProducedItems = new Dictionary<string, int>(ProducedItems),
                CanBeInterrupted = CanBeInterrupted,
                RequiresTools = RequiresTools,
                MaxAssignedCharacters = MaxAssignedCharacters,
                CreatedTime = CreatedTime,
                Deadline = Deadline,
                CustomProperties = new Dictionary<string, object>(CustomProperties)
            };
        }

        public override string ToString()
        {
            return $"{Name} ({Type}, {Priority})";
        }
    }
}