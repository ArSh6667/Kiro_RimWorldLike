using System;
using System.Collections.Generic;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Characters.BehaviorTree;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Characters
{
    /// <summary>
    /// 角色性别枚举
    /// </summary>
    public enum Gender
    {
        Male,
        Female,
        Other
    }

    /// <summary>
    /// 角色年龄阶段枚举
    /// </summary>
    public enum AgeStage
    {
        Child,      // 儿童 (0-12)
        Teenager,   // 青少年 (13-17)
        Adult,      // 成年人 (18-59)
        Elder       // 老年人 (60+)
    }

    /// <summary>
    /// 角色实体类
    /// </summary>
    public class CharacterEntity : Entity
    {
        // 基本信息
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; } = Gender.Male;
        public int Age { get; set; } = 25;
        public string Biography { get; set; } = string.Empty;

        // 外观属性
        public string HairColor { get; set; } = "Brown";
        public string SkinColor { get; set; } = "Medium";
        public string EyeColor { get; set; } = "Brown";
        public float Height { get; set; } = 1.75f; // 米

        // 性格特征
        public Dictionary<string, float> Traits { get; set; } = new();

        // 组件快速访问属性
        public PositionComponent? Position { get; private set; }
        public SkillComponent? Skills { get; private set; }
        public NeedComponent? Needs { get; private set; }
        public InventoryComponent? Inventory { get; private set; }

        // 行为树相关
        public BehaviorNode? BehaviorTree { get; set; }
        public CharacterContext? BehaviorContext { get; private set; }

        public CharacterEntity()
        {
            InitializeComponents();
        }

        public CharacterEntity(string name) : this()
        {
            Name = name;
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            // 这些组件将通过EntityManager添加
            // 这里只是预留引用
        }

        /// <summary>
        /// 设置组件引用（由EntityManager调用）
        /// </summary>
        public void SetComponentReferences(IEntityManager entityManager)
        {
            Position = entityManager.GetComponent<PositionComponent>(Id);
            Skills = entityManager.GetComponent<SkillComponent>(Id);
            Needs = entityManager.GetComponent<NeedComponent>(Id);
            Inventory = entityManager.GetComponent<InventoryComponent>(Id);
            
            // 初始化行为上下文
            BehaviorContext = new CharacterContext(this, 0f);
        }

        /// <summary>
        /// 获取年龄阶段
        /// </summary>
        public AgeStage GetAgeStage()
        {
            return Age switch
            {
                < 13 => AgeStage.Child,
                < 18 => AgeStage.Teenager,
                < 60 => AgeStage.Adult,
                _ => AgeStage.Elder
            };
        }

        /// <summary>
        /// 获取性别描述
        /// </summary>
        public string GetGenderDescription()
        {
            return Gender switch
            {
                Gender.Male => "男性",
                Gender.Female => "女性",
                Gender.Other => "其他",
                _ => "未知"
            };
        }

        /// <summary>
        /// 添加性格特征
        /// </summary>
        public void AddTrait(string traitName, float intensity = 1.0f)
        {
            Traits[traitName] = Math.Max(-2.0f, Math.Min(2.0f, intensity));
        }

        /// <summary>
        /// 获取性格特征强度
        /// </summary>
        public float GetTraitIntensity(string traitName)
        {
            return Traits.TryGetValue(traitName, out var intensity) ? intensity : 0f;
        }

        /// <summary>
        /// 检查是否有特定性格特征
        /// </summary>
        public bool HasTrait(string traitName, float minIntensity = 0.5f)
        {
            return Math.Abs(GetTraitIntensity(traitName)) >= minIntensity;
        }

        /// <summary>
        /// 获取角色描述
        /// </summary>
        public string GetDescription()
        {
            var ageStage = GetAgeStage() switch
            {
                AgeStage.Child => "儿童",
                AgeStage.Teenager => "青少年",
                AgeStage.Adult => "成年人",
                AgeStage.Elder => "老年人",
                _ => "未知"
            };

            return $"{Name}，{Age}岁{GetGenderDescription()}{ageStage}";
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            var info = GetDescription();
            
            if (!string.IsNullOrEmpty(Biography))
            {
                info += $"\n背景: {Biography}";
            }

            if (Skills != null)
            {
                var highestSkill = Skills.GetHighestSkill();
                info += $"\n最高技能: {highestSkill.GetDescription()}";
            }

            if (Needs != null)
            {
                info += $"\n状态: {Needs.GetNeedsSummary()}";
            }

            return info;
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        public void Update(float deltaTime)
        {
            // 更新需求
            Needs?.UpdateNeeds(deltaTime);

            // 更新位置（如果正在移动）
            Position?.UpdateMovement(Time.time, deltaTime);

            // 更新行为树
            if (BehaviorTree != null && BehaviorContext != null)
            {
                BehaviorContext.DeltaTime = deltaTime;
                BehaviorTree.Execute(BehaviorContext);
            }
        }

        /// <summary>
        /// 随机生成角色
        /// </summary>
        public static CharacterEntity GenerateRandom(Random random, string? name = null)
        {
            var character = new CharacterEntity
            {
                Name = name ?? GenerateRandomName(random),
                Gender = (Gender)random.Next(0, 3),
                Age = random.Next(18, 65),
                Height = random.NextSingle() * 0.4f + 1.5f, // 1.5m 到 1.9m
                HairColor = GetRandomHairColor(random),
                SkinColor = GetRandomSkinColor(random),
                EyeColor = GetRandomEyeColor(random),
                Biography = GenerateRandomBiography(random)
            };

            // 添加随机性格特征
            var traitNames = new[] { "勤奋", "懒惰", "友善", "暴躁", "勇敢", "胆小", "聪明", "迟钝" };
            var traitCount = random.Next(2, 5);
            
            for (int i = 0; i < traitCount; i++)
            {
                var trait = traitNames[random.Next(traitNames.Length)];
                var intensity = random.NextSingle() * 2f - 1f; // -1 到 1
                character.AddTrait(trait, intensity);
            }

            return character;
        }

        private static string GenerateRandomName(Random random)
        {
            var firstNames = new[] { "张三", "李四", "王五", "赵六", "钱七", "孙八", "周九", "吴十" };
            return firstNames[random.Next(firstNames.Length)];
        }

        private static string GetRandomHairColor(Random random)
        {
            var colors = new[] { "黑色", "棕色", "金色", "红色", "灰色", "白色" };
            return colors[random.Next(colors.Length)];
        }

        private static string GetRandomSkinColor(Random random)
        {
            var colors = new[] { "浅色", "中等", "深色", "橄榄色" };
            return colors[random.Next(colors.Length)];
        }

        private static string GetRandomEyeColor(Random random)
        {
            var colors = new[] { "棕色", "蓝色", "绿色", "灰色", "黑色" };
            return colors[random.Next(colors.Length)];
        }

        private static string GenerateRandomBiography(Random random)
        {
            var backgrounds = new[]
            {
                "曾经是一名工程师，在殖民地寻找新的开始。",
                "从小在农场长大，对种植和动物有丰富经验。",
                "前军人，具有丰富的战斗和生存技能。",
                "医学院毕业生，致力于帮助他人。",
                "艺术家，用创作表达内心世界。",
                "商人，善于交易和管理资源。",
                "研究员，对科学和技术充满热情。",
                "厨师，能够制作美味的食物。"
            };
            
            return backgrounds[random.Next(backgrounds.Length)];
        }

        public override string ToString()
        {
            return GetDescription();
        }
    }

    /// <summary>
    /// 时间工具类（简化版）
    /// </summary>
    public static class Time
    {
        private static float _time = 0f;
        
        public static float time => _time;
        
        public static void UpdateTime(float deltaTime)
        {
            _time += deltaTime;
        }
    }
}