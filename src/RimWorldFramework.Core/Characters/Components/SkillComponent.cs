using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Core.Characters.Components
{
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Mining,      // 挖掘
        Construction, // 建造
        Growing,     // 种植
        Cooking,     // 烹饪
        Crafting,    // 制作
        Research,    // 研究
        Medicine,    // 医疗
        Combat,      // 战斗
        Social,      // 社交
        Animals      // 动物
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    public class Skill
    {
        public SkillType Type { get; set; }
        public int Level { get; set; } = 0;
        public float Experience { get; set; } = 0f;
        public float Passion { get; set; } = 1.0f; // 热情度，影响经验获取速度
        public bool IsDisabled { get; set; } = false;

        public Skill(SkillType type)
        {
            Type = type;
        }

        /// <summary>
        /// 获取技能效率（基于等级和热情）
        /// </summary>
        public float GetEfficiency()
        {
            if (IsDisabled) return 0f;
            return (Level + 1) * Passion;
        }

        /// <summary>
        /// 添加经验
        /// </summary>
        public bool AddExperience(float amount)
        {
            if (IsDisabled) return false;

            Experience += amount * Passion;
            
            // 检查是否升级
            var requiredExp = GetRequiredExperienceForNextLevel();
            if (Experience >= requiredExp && Level < 20)
            {
                Experience -= requiredExp;
                Level++;
                return true; // 返回true表示升级了
            }
            
            return false;
        }

        /// <summary>
        /// 获取下一级所需经验
        /// </summary>
        public float GetRequiredExperienceForNextLevel()
        {
            return (Level + 1) * 1000f; // 简单的线性增长
        }

        /// <summary>
        /// 获取技能描述
        /// </summary>
        public string GetDescription()
        {
            var status = IsDisabled ? "禁用" : $"等级 {Level}";
            return $"{GetSkillName(Type)}: {status}";
        }

        private static string GetSkillName(SkillType type)
        {
            return type switch
            {
                SkillType.Mining => "挖掘",
                SkillType.Construction => "建造",
                SkillType.Growing => "种植",
                SkillType.Cooking => "烹饪",
                SkillType.Crafting => "制作",
                SkillType.Research => "研究",
                SkillType.Medicine => "医疗",
                SkillType.Combat => "战斗",
                SkillType.Social => "社交",
                SkillType.Animals => "动物",
                _ => type.ToString()
            };
        }
    }

    /// <summary>
    /// 技能组件
    /// </summary>
    [ComponentDescription("角色的技能系统，包含各种技能等级和经验")]
    public class SkillComponent : Component
    {
        private readonly Dictionary<SkillType, Skill> _skills = new();

        public SkillComponent()
        {
            InitializeSkills();
        }

        /// <summary>
        /// 初始化所有技能
        /// </summary>
        private void InitializeSkills()
        {
            foreach (SkillType skillType in Enum.GetValues<SkillType>())
            {
                _skills[skillType] = new Skill(skillType);
            }
        }

        /// <summary>
        /// 获取技能
        /// </summary>
        public Skill GetSkill(SkillType type)
        {
            return _skills.TryGetValue(type, out var skill) ? skill : new Skill(type);
        }

        /// <summary>
        /// 设置技能等级
        /// </summary>
        public void SetSkillLevel(SkillType type, int level)
        {
            var skill = GetSkill(type);
            skill.Level = Math.Max(0, Math.Min(level, 20));
            _skills[type] = skill;
        }

        /// <summary>
        /// 添加技能经验
        /// </summary>
        public bool AddSkillExperience(SkillType type, float amount)
        {
            var skill = GetSkill(type);
            var leveledUp = skill.AddExperience(amount);
            _skills[type] = skill;
            return leveledUp;
        }

        /// <summary>
        /// 获取技能效率
        /// </summary>
        public float GetSkillEfficiency(SkillType type)
        {
            return GetSkill(type).GetEfficiency();
        }

        /// <summary>
        /// 禁用/启用技能
        /// </summary>
        public void SetSkillDisabled(SkillType type, bool disabled)
        {
            var skill = GetSkill(type);
            skill.IsDisabled = disabled;
            _skills[type] = skill;
        }

        /// <summary>
        /// 设置技能热情
        /// </summary>
        public void SetSkillPassion(SkillType type, float passion)
        {
            var skill = GetSkill(type);
            skill.Passion = Math.Max(0.1f, Math.Min(passion, 3.0f));
            _skills[type] = skill;
        }

        /// <summary>
        /// 获取所有技能
        /// </summary>
        public IEnumerable<Skill> GetAllSkills()
        {
            return _skills.Values.ToList();
        }

        /// <summary>
        /// 获取最高技能
        /// </summary>
        public Skill GetHighestSkill()
        {
            return _skills.Values
                .Where(s => !s.IsDisabled)
                .OrderByDescending(s => s.Level)
                .ThenByDescending(s => s.Experience)
                .FirstOrDefault() ?? new Skill(SkillType.Mining);
        }

        /// <summary>
        /// 获取适合特定任务的技能效率
        /// </summary>
        public float GetTaskEfficiency(SkillType primarySkill, SkillType? secondarySkill = null)
        {
            var primaryEfficiency = GetSkillEfficiency(primarySkill);
            
            if (secondarySkill.HasValue)
            {
                var secondaryEfficiency = GetSkillEfficiency(secondarySkill.Value);
                return (primaryEfficiency * 0.7f) + (secondaryEfficiency * 0.3f);
            }
            
            return primaryEfficiency;
        }

        /// <summary>
        /// 随机生成技能（用于角色创建）
        /// </summary>
        public void RandomizeSkills(Random random, int minLevel = 0, int maxLevel = 10)
        {
            foreach (var skillType in Enum.GetValues<SkillType>())
            {
                var level = random.Next(minLevel, maxLevel + 1);
                var passion = random.NextSingle() * 2.0f + 0.5f; // 0.5 到 2.5
                
                SetSkillLevel(skillType, level);
                SetSkillPassion(skillType, passion);
                
                // 10% 概率禁用技能
                if (random.NextSingle() < 0.1f)
                {
                    SetSkillDisabled(skillType, true);
                }
            }
        }
    }
}