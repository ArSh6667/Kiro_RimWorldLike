using System;
using System.Collections.Generic;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Core.Characters.Components
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
    /// 角色组件，包含角色的基本信息和状态
    /// </summary>
    [ComponentDescription("角色的基本信息和状态组件")]
    public class CharacterComponent : Component
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

        // 游戏状态
        public float Mood { get; set; } = 0.8f;
        public float Efficiency { get; set; } = 0.7f;

        // 组件引用
        public SkillComponent Skills { get; set; } = new();
        public NeedComponent Needs { get; set; } = new();
        public PositionComponent? Position { get; set; }
        public InventoryComponent? Inventory { get; set; }

        public CharacterComponent()
        {
            Skills = new SkillComponent();
            Needs = new NeedComponent();
        }

        public CharacterComponent(string name) : this()
        {
            Name = name;
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
                Gender.Male => "Male",
                Gender.Female => "Female",
                Gender.Other => "Other",
                _ => "Unknown"
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
                AgeStage.Child => "Child",
                AgeStage.Teenager => "Teenager",
                AgeStage.Adult => "Adult",
                AgeStage.Elder => "Elder",
                _ => "Unknown"
            };

            return $"{Name}, {Age} year old {GetGenderDescription()} {ageStage}";
        }

        /// <summary>
        /// 获取详细信息
        /// </summary>
        public string GetDetailedInfo()
        {
            var info = GetDescription();
            
            if (!string.IsNullOrEmpty(Biography))
            {
                info += $"\nBackground: {Biography}";
            }

            if (Skills != null)
            {
                var highestSkill = Skills.GetHighestSkill();
                info += $"\nHighest Skill: {highestSkill.GetDescription()}";
            }

            if (Needs != null)
            {
                info += $"\nStatus: {Needs.GetNeedsSummary()}";
            }

            return info;
        }

        /// <summary>
        /// 随机生成角色
        /// </summary>
        public static CharacterComponent GenerateRandom(Random random, string? name = null)
        {
            var character = new CharacterComponent
            {
                Name = name ?? GenerateRandomName(random),
                Gender = (Gender)random.Next(0, 3),
                Age = random.Next(18, 65),
                Height = random.NextSingle() * 0.4f + 1.5f, // 1.5m 到 1.9m
                HairColor = GetRandomHairColor(random),
                SkinColor = GetRandomSkinColor(random),
                EyeColor = GetRandomEyeColor(random),
                Biography = GenerateRandomBiography(random),
                Mood = 0.5f + random.NextSingle() * 0.4f, // 0.5 到 0.9
                Efficiency = 0.4f + random.NextSingle() * 0.5f // 0.4 到 0.9
            };

            // 随机化技能
            character.Skills.RandomizeSkills(random, 0, 5);

            // 添加随机性格特征
            var traitNames = new[] { "Hardworking", "Lazy", "Friendly", "Aggressive", "Brave", "Coward", "Smart", "Slow" };
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
            var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Edward", "Fiona", "George", "Helen" };
            return firstNames[random.Next(firstNames.Length)];
        }

        private static string GetRandomHairColor(Random random)
        {
            var colors = new[] { "Black", "Brown", "Blonde", "Red", "Gray", "White" };
            return colors[random.Next(colors.Length)];
        }

        private static string GetRandomSkinColor(Random random)
        {
            var colors = new[] { "Light", "Medium", "Dark", "Olive" };
            return colors[random.Next(colors.Length)];
        }

        private static string GetRandomEyeColor(Random random)
        {
            var colors = new[] { "Brown", "Blue", "Green", "Gray", "Black" };
            return colors[random.Next(colors.Length)];
        }

        private static string GenerateRandomBiography(Random random)
        {
            var backgrounds = new[]
            {
                "Former engineer seeking a new start in the colony.",
                "Grew up on a farm with rich experience in planting and animals.",
                "Ex-military with extensive combat and survival skills.",
                "Medical school graduate dedicated to helping others.",
                "Artist expressing inner world through creation.",
                "Merchant skilled in trading and resource management.",
                "Researcher passionate about science and technology.",
                "Chef capable of making delicious food."
            };
            
            return backgrounds[random.Next(backgrounds.Length)];
        }

        public override string ToString()
        {
            return GetDescription();
        }
    }
}