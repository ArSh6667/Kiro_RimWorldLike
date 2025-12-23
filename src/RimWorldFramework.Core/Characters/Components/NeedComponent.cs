using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Core.Characters.Components
{
    /// <summary>
    /// 需求类型枚举
    /// </summary>
    public enum NeedType
    {
        Hunger,      // 饥饿
        Rest,        // 休息
        Recreation,  // 娱乐
        Comfort,     // 舒适
        Beauty,      // 美观
        Space,       // 空间
        Temperature, // 温度
        Safety       // 安全
    }

    /// <summary>
    /// 需求数据
    /// </summary>
    public class Need
    {
        public NeedType Type { get; set; }
        public float Value { get; set; } = 1.0f; // 0.0 到 1.0，1.0 表示完全满足
        public float DecayRate { get; set; } = 0.1f; // 每秒衰减率
        public float Priority { get; set; } = 1.0f; // 优先级
        public bool IsCritical { get; set; } = false; // 是否为关键需求

        public Need(NeedType type)
        {
            Type = type;
            SetDefaultValues();
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        private void SetDefaultValues()
        {
            switch (Type)
            {
                case NeedType.Hunger:
                    DecayRate = 0.05f;
                    Priority = 10.0f;
                    IsCritical = true;
                    break;
                case NeedType.Rest:
                    DecayRate = 0.03f;
                    Priority = 8.0f;
                    IsCritical = true;
                    break;
                case NeedType.Recreation:
                    DecayRate = 0.02f;
                    Priority = 3.0f;
                    break;
                case NeedType.Comfort:
                    DecayRate = 0.01f;
                    Priority = 2.0f;
                    break;
                case NeedType.Beauty:
                    DecayRate = 0.005f;
                    Priority = 1.0f;
                    break;
                case NeedType.Space:
                    DecayRate = 0.008f;
                    Priority = 2.5f;
                    break;
                case NeedType.Temperature:
                    DecayRate = 0.04f;
                    Priority = 6.0f;
                    break;
                case NeedType.Safety:
                    DecayRate = 0.01f;
                    Priority = 7.0f;
                    break;
            }
        }

        /// <summary>
        /// 更新需求值
        /// </summary>
        public void Update(float deltaTime)
        {
            Value = Math.Max(0f, Value - (DecayRate * deltaTime));
        }

        /// <summary>
        /// 满足需求
        /// </summary>
        public void Satisfy(float amount)
        {
            Value = Math.Min(1.0f, Value + amount);
        }

        /// <summary>
        /// 获取需求状态
        /// </summary>
        public NeedStatus GetStatus()
        {
            return Value switch
            {
                >= 0.8f => NeedStatus.Satisfied,
                >= 0.6f => NeedStatus.Good,
                >= 0.4f => NeedStatus.Moderate,
                >= 0.2f => NeedStatus.Low,
                _ => NeedStatus.Critical
            };
        }

        /// <summary>
        /// 获取需求紧急程度（用于AI决策）
        /// </summary>
        public float GetUrgency()
        {
            var urgency = (1.0f - Value) * Priority;
            if (IsCritical && Value < 0.3f)
            {
                urgency *= 2.0f; // 关键需求在低值时紧急程度翻倍
            }
            return urgency;
        }

        /// <summary>
        /// 获取需求名称
        /// </summary>
        public string GetName()
        {
            return Type switch
            {
                NeedType.Hunger => "饥饿",
                NeedType.Rest => "休息",
                NeedType.Recreation => "娱乐",
                NeedType.Comfort => "舒适",
                NeedType.Beauty => "美观",
                NeedType.Space => "空间",
                NeedType.Temperature => "温度",
                NeedType.Safety => "安全",
                _ => Type.ToString()
            };
        }
    }

    /// <summary>
    /// 需求状态枚举
    /// </summary>
    public enum NeedStatus
    {
        Critical,   // 危急
        Low,        // 低
        Moderate,   // 中等
        Good,       // 良好
        Satisfied   // 满足
    }

    /// <summary>
    /// 需求组件
    /// </summary>
    [ComponentDescription("角色的需求系统，管理饥饿、休息、娱乐等各种需求")]
    public class NeedComponent : Component
    {
        private readonly Dictionary<NeedType, Need> _needs = new();

        public NeedComponent()
        {
            InitializeNeeds();
        }

        /// <summary>
        /// 初始化所有需求
        /// </summary>
        private void InitializeNeeds()
        {
            foreach (NeedType needType in Enum.GetValues<NeedType>())
            {
                _needs[needType] = new Need(needType);
            }
        }

        /// <summary>
        /// 获取需求
        /// </summary>
        public Need GetNeed(NeedType type)
        {
            return _needs.TryGetValue(type, out var need) ? need : new Need(type);
        }

        /// <summary>
        /// 更新所有需求
        /// </summary>
        public void UpdateNeeds(float deltaTime)
        {
            foreach (var need in _needs.Values)
            {
                need.Update(deltaTime);
            }
        }

        /// <summary>
        /// 满足特定需求
        /// </summary>
        public void SatisfyNeed(NeedType type, float amount)
        {
            if (_needs.TryGetValue(type, out var need))
            {
                need.Satisfy(amount);
            }
        }

        /// <summary>
        /// 获取最紧急的需求
        /// </summary>
        public Need GetMostUrgentNeed()
        {
            return _needs.Values
                .OrderByDescending(n => n.GetUrgency())
                .First();
        }

        /// <summary>
        /// 获取关键需求列表
        /// </summary>
        public IEnumerable<Need> GetCriticalNeeds()
        {
            return _needs.Values
                .Where(n => n.IsCritical && n.GetStatus() == NeedStatus.Critical)
                .OrderByDescending(n => n.GetUrgency());
        }

        /// <summary>
        /// 获取所有需求
        /// </summary>
        public IEnumerable<Need> GetAllNeeds()
        {
            return _needs.Values.ToList();
        }

        /// <summary>
        /// 检查是否有关键需求未满足
        /// </summary>
        public bool HasCriticalNeeds()
        {
            return _needs.Values.Any(n => n.IsCritical && n.Value < 0.3f);
        }

        /// <summary>
        /// 获取整体幸福度
        /// </summary>
        public float GetOverallHappiness()
        {
            var totalWeight = _needs.Values.Sum(n => n.Priority);
            var weightedSum = _needs.Values.Sum(n => n.Value * n.Priority);
            
            return totalWeight > 0 ? weightedSum / totalWeight : 0f;
        }

        /// <summary>
        /// 获取需求摘要
        /// </summary>
        public string GetNeedsSummary()
        {
            var criticalNeeds = GetCriticalNeeds().ToList();
            if (criticalNeeds.Any())
            {
                var needNames = string.Join(", ", criticalNeeds.Select(n => n.GetName()));
                return $"关键需求: {needNames}";
            }

            var happiness = GetOverallHappiness();
            return happiness switch
            {
                >= 0.8f => "心情很好",
                >= 0.6f => "心情不错",
                >= 0.4f => "心情一般",
                >= 0.2f => "心情不好",
                _ => "心情很差"
            };
        }

        /// <summary>
        /// 设置需求值（用于测试或特殊情况）
        /// </summary>
        public void SetNeedValue(NeedType type, float value)
        {
            if (_needs.TryGetValue(type, out var need))
            {
                need.Value = Math.Max(0f, Math.Min(1f, value));
            }
        }

        /// <summary>
        /// 随机化需求值（用于角色创建）
        /// </summary>
        public void RandomizeNeeds(Random random, float minValue = 0.3f, float maxValue = 1.0f)
        {
            foreach (var need in _needs.Values)
            {
                need.Value = random.NextSingle() * (maxValue - minValue) + minValue;
            }
        }
    }
}