using System.Collections.Generic;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 资源点类型枚举
    /// </summary>
    public enum ResourceType
    {
        Wood,       // 木材
        Stone,      // 石材
        Metal,      // 金属
        Food,       // 食物
        Water       // 水源
    }

    /// <summary>
    /// 资源点类
    /// </summary>
    public class ResourcePoint
    {
        /// <summary>
        /// 资源位置
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public ResourceType Type { get; }

        /// <summary>
        /// 资源数量
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// 资源品质（0-1）
        /// </summary>
        public float Quality { get; }

        /// <summary>
        /// 是否已被开采
        /// </summary>
        public bool IsExhausted { get; set; }

        public ResourcePoint(Vector3 position, ResourceType type, int amount, float quality = 1.0f)
        {
            Position = position;
            Type = type;
            Amount = amount;
            Quality = quality;
            IsExhausted = false;
        }

        /// <summary>
        /// 开采资源
        /// </summary>
        /// <param name="extractAmount">开采数量</param>
        /// <returns>实际开采数量</returns>
        public int Extract(int extractAmount)
        {
            if (IsExhausted) return 0;

            int actualAmount = System.Math.Min(extractAmount, Amount);
            Amount -= actualAmount;

            if (Amount <= 0)
            {
                IsExhausted = true;
            }

            return actualAmount;
        }
    }

    /// <summary>
    /// 资源配置类
    /// </summary>
    public class ResourceConfig
    {
        /// <summary>
        /// 资源密度（每平方单位的资源点数量）
        /// </summary>
        public float Density { get; set; } = 0.01f;

        /// <summary>
        /// 最小资源间距
        /// </summary>
        public float MinDistance { get; set; } = 5.0f;

        /// <summary>
        /// 各种资源类型的权重
        /// </summary>
        public Dictionary<ResourceType, float> TypeWeights { get; set; } = new()
        {
            { ResourceType.Wood, 0.3f },
            { ResourceType.Stone, 0.25f },
            { ResourceType.Metal, 0.15f },
            { ResourceType.Food, 0.2f },
            { ResourceType.Water, 0.1f }
        };

        /// <summary>
        /// 各种资源类型的数量范围
        /// </summary>
        public Dictionary<ResourceType, (int min, int max)> AmountRanges { get; set; } = new()
        {
            { ResourceType.Wood, (50, 200) },
            { ResourceType.Stone, (100, 300) },
            { ResourceType.Metal, (25, 100) },
            { ResourceType.Food, (30, 150) },
            { ResourceType.Water, (1000, 5000) }
        };
    }
}