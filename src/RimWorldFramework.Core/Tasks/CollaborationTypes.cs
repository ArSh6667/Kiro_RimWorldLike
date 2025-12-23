using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.Characters.Components;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 协作类型
    /// </summary>
    public enum CollaborationType
    {
        General,        // 一般协作
        Construction,   // 建造协作
        Research,       // 研究协作
        Mining,         // 挖掘协作
        Defense,        // 防御协作
        Crafting,       // 制作协作
        Hauling         // 搬运协作
    }

    /// <summary>
    /// 协作状态
    /// </summary>
    public enum CollaborationStatus
    {
        Forming,        // 组建中
        Active,         // 活跃
        Suspended,      // 暂停
        Completed,      // 完成
        Failed          // 失败
    }

    /// <summary>
    /// 协作角色
    /// </summary>
    public enum CollaborationRole
    {
        Leader,         // 领导者
        Worker,         // 工人
        Specialist,     // 专家
        Assistant,      // 助手
        Observer        // 观察者
    }

    /// <summary>
    /// 参与者状态
    /// </summary>
    public enum ParticipantStatus
    {
        Active,         // 活跃
        Idle,           // 空闲
        Busy,           // 忙碌
        Unavailable     // 不可用
    }

    /// <summary>
    /// 资源类型
    /// </summary>
    public enum ResourceType
    {
        WorkArea,       // 工作区域
        Material,       // 材料
        Tool,           // 工具
        Equipment,      // 设备
        Storage         // 存储
    }

    /// <summary>
    /// 冲突类型
    /// </summary>
    public enum ConflictType
    {
        CharacterOverassignment,    // 角色重复分配
        ResourceConflict,           // 资源冲突
        TimeConflict,              // 时间冲突
        SkillConflict              // 技能冲突
    }

    /// <summary>
    /// 协作组
    /// </summary>
    public class CollaborationGroup
    {
        public TaskId TaskId { get; set; }
        public CollaborationType Type { get; set; }
        public CollaborationStatus Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int MaxParticipants { get; set; }
        public List<CollaborationParticipant> Participants { get; set; } = new();
        public List<SkillRequirement> RequiredSkills { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();

        public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : null;

        public bool IsActive => Status == CollaborationStatus.Active;
        public bool CanAcceptMoreParticipants => Participants.Count < MaxParticipants;

        public override string ToString()
        {
            return $"协作组 {TaskId}: {Type} ({Status}) - {Participants.Count}/{MaxParticipants} 参与者";
        }
    }

    /// <summary>
    /// 协作参与者
    /// </summary>
    public class CollaborationParticipant
    {
        public uint CharacterId { get; set; }
        public CollaborationRole Role { get; set; }
        public ParticipantStatus Status { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public float ContributionScore { get; set; }
        public Dictionary<string, object> RoleData { get; set; } = new();

        public TimeSpan ParticipationTime => LeaveTime?.Subtract(JoinTime) ?? DateTime.Now.Subtract(JoinTime);

        public override string ToString()
        {
            return $"角色 {CharacterId}: {Role} ({Status})";
        }
    }

    /// <summary>
    /// 协作状态
    /// </summary>
    public class CollaborationState
    {
        public uint CharacterId { get; set; }
        public TaskId CurrentTaskId { get; set; }
        public CollaborationRole Role { get; set; }
        public DateTime JoinTime { get; set; }
        public float EfficiencyMultiplier { get; set; } = 1.0f;
        public Dictionary<string, object> StateData { get; set; } = new();
    }

    /// <summary>
    /// 资源预订
    /// </summary>
    public class ResourceReservation
    {
        public Vector3 Position { get; set; }
        public uint CharacterId { get; set; }
        public ResourceType ResourceType { get; set; }
        public DateTime ReservationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> ReservationData { get; set; } = new();

        public TimeSpan RemainingTime => ExpirationTime > DateTime.Now 
            ? ExpirationTime - DateTime.Now 
            : TimeSpan.Zero;

        public bool IsExpired => DateTime.Now >= ExpirationTime;

        public override string ToString()
        {
            return $"资源预订 {Position}: {ResourceType} by {CharacterId} (剩余: {RemainingTime})";
        }
    }

    /// <summary>
    /// 协作结果
    /// </summary>
    public class CollaborationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public CollaborationGroup? Group { get; set; }
        public Dictionary<string, object> ResultData { get; set; } = new();

        public static CollaborationResult Success(string message, CollaborationGroup? group = null)
        {
            return new CollaborationResult
            {
                IsSuccess = true,
                Message = message,
                Group = group
            };
        }

        public static CollaborationResult Failure(string message)
        {
            return new CollaborationResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// 资源预订结果
    /// </summary>
    public class ResourceReservationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public ResourceReservation? Reservation { get; set; }

        public static ResourceReservationResult Success(ResourceReservation reservation)
        {
            return new ResourceReservationResult
            {
                IsSuccess = true,
                Message = "资源预订成功",
                Reservation = reservation
            };
        }

        public static ResourceReservationResult Failure(string message)
        {
            return new ResourceReservationResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// 资源冲突结果
    /// </summary>
    public class ResourceConflictResult
    {
        public bool HasConflict { get; set; }
        public List<ResourceReservation> ConflictingReservations { get; set; } = new();
        public float ConflictRadius { get; set; }
        public string ConflictReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 协作冲突
    /// </summary>
    public class CollaborationConflict
    {
        public bool HasConflict { get; set; }
        public string ConflictReason { get; set; } = string.Empty;
        public List<CollaborationGroup> ConflictingGroups { get; set; } = new();
    }

    /// <summary>
    /// 任务协调结果
    /// </summary>
    public class TaskCoordinationResult
    {
        public List<CollaborationGroup> CreatedGroups { get; set; } = new();
        public List<CollaborationAssignment> Assignments { get; set; } = new();
        public List<ConflictResolution> ConflictResolutions { get; set; } = new();
        public bool IsSuccess => ConflictResolutions.All(r => r.IsResolved);
    }

    /// <summary>
    /// 协作分配
    /// </summary>
    public class CollaborationAssignment
    {
        public TaskId TaskId { get; set; }
        public uint CharacterId { get; set; }
        public CollaborationRole Role { get; set; }
        public float Score { get; set; }
        public DateTime AssignmentTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 分配冲突
    /// </summary>
    public class AssignmentConflict
    {
        public ConflictType Type { get; set; }
        public uint CharacterId { get; set; }
        public List<CollaborationAssignment> ConflictingAssignments { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 冲突解决方案
    /// </summary>
    public class ConflictResolution
    {
        public ConflictType ConflictType { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public CollaborationAssignment? SelectedAssignment { get; set; }
        public bool IsResolved { get; set; } = true;
        public DateTime ResolutionTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 协作统计信息
    /// </summary>
    public class CollaborationStats
    {
        public int TotalCollaborationGroups { get; set; }
        public int ActiveCollaborationGroups { get; set; }
        public int TotalParticipants { get; set; }
        public int ActiveResourceReservations { get; set; }
        public int CharactersInCollaboration { get; set; }
        public Dictionary<CollaborationType, int> GroupsByType { get; set; } = new();
        public Dictionary<CollaborationRole, int> ParticipantsByRole { get; set; } = new();

        public float CollaborationEfficiency => TotalCollaborationGroups > 0 
            ? (float)ActiveCollaborationGroups / TotalCollaborationGroups 
            : 0f;

        public override string ToString()
        {
            return $"协作统计: {ActiveCollaborationGroups}/{TotalCollaborationGroups} 活跃组, " +
                   $"{TotalParticipants} 参与者, {ActiveResourceReservations} 资源预订, " +
                   $"效率: {CollaborationEfficiency:P}";
        }
    }
}