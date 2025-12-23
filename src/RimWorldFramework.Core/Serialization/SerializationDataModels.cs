using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.MapGeneration;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 角色实体序列化数据
    /// </summary>
    [Serializable]
    public class CharacterEntityData
    {
        public uint EntityId { get; set; }
        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public SkillData Skills { get; set; }
        public NeedData Needs { get; set; }
        public InventoryData Inventory { get; set; }
        public Dictionary<string, object> CustomComponents { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public Dictionary<string, int> SkillLevels { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, float> SkillExperience { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// 需求数据
    /// </summary>
    [Serializable]
    public class NeedData
    {
        public float Hunger { get; set; }
        public float Sleep { get; set; }
        public float Recreation { get; set; }
        public float Comfort { get; set; }
        public Dictionary<string, float> CustomNeeds { get; set; } = new Dictionary<string, float>();
    }

    /// <summary>
    /// 库存数据
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        public int MaxCapacity { get; set; }
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string ItemType { get; set; }
        public int Quantity { get; set; }
        public float Quality { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 任务树状态数据
    /// </summary>
    [Serializable]
    public class TaskTreeState
    {
        public List<TaskData> Tasks { get; set; } = new List<TaskData>();
        public Dictionary<string, TaskStatus> TaskStatuses { get; set; } = new Dictionary<string, TaskStatus>();
        public List<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();
        public Dictionary<string, object> TaskProgress { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 任务依赖关系数据
    /// </summary>
    [Serializable]
    public class TaskDependency
    {
        public string ParentTaskId { get; set; }
        public string ChildTaskId { get; set; }
        public DependencyType Type { get; set; }
    }

    /// <summary>
    /// 依赖类型
    /// </summary>
    public enum DependencyType
    {
        Sequential,     // 顺序依赖
        Parallel,       // 并行依赖
        Conditional     // 条件依赖
    }

    /// <summary>
    /// 游戏地图序列化数据
    /// </summary>
    [Serializable]
    public class GameMapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Seed { get; set; }
        public TerrainType[,] Terrain { get; set; }
        public float[,] HeightMap { get; set; }
        public List<ResourcePointData> Resources { get; set; } = new List<ResourcePointData>();
        public Dictionary<string, object> CustomMapData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 资源点序列化数据
    /// </summary>
    [Serializable]
    public class ResourcePointData
    {
        public Vector3 Position { get; set; }
        public ResourceType Type { get; set; }
        public int Amount { get; set; }
        public float Quality { get; set; }
        public bool IsExhausted { get; set; }
    }
}