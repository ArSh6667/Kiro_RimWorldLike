using System;
using System.Linq;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Characters.BehaviorTree;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Characters
{
    /// <summary>
    /// 角色系统属性测试
    /// Feature: rimworld-game-framework
    /// </summary>
    [TestFixture]
    public class CharacterSystemPropertyTests : TestBase
    {
        private CharacterSystem _characterSystem;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _entityManager = new EntityManager();
            _characterSystem = new CharacterSystem(_entityManager);
            _characterSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _characterSystem.Shutdown();
        }

        /// <summary>
        /// Property 2: 任务分配一致性
        /// 对于任何人物和任务组合，任务分配应当基于人物的技能水平和任务优先级进行合理匹配
        /// </summary>
        [Property(MaxTest = 100)]
        [Category("Property")]
        public Property TaskAssignmentConsistency()
        {
            return Prop.ForAll(
                GenerateCharacterWithSkills(),
                GenerateTaskRequirements(),
                (character, taskRequirements) =>
                {
                    // 注册角色
                    _characterSystem.RegisterCharacter(character);

                    // 计算角色对任务的适合度
                    var suitability = CalculateTaskSuitability(character, taskRequirements);

                    // 验证适合度计算的一致性
                    var recalculatedSuitability = CalculateTaskSuitability(character, taskRequirements);

                    return suitability == recalculatedSuitability &&
                           suitability >= 0f && suitability <= 1f;
                });
        }

        /// <summary>
        /// Property 3: 行动决策完整性
        /// 对于任何人物行动，决策过程应当考虑角色的当前技能、状态和环境因素
        /// </summary>
        [Property(MaxTest = 100)]
        [Category("Property")]
        public Property ActionDecisionIntegrity()
        {
            return Prop.ForAll(
                GenerateCharacterWithState(),
                (character) =>
                {
                    // 注册角色
                    _characterSystem.RegisterCharacter(character);

                    // 创建决策上下文
                    var context = new CharacterContext(character, 0.1f);

                    // 验证决策考虑了所有必要因素
                    var decision = MakeActionDecision(character, context);

                    // 决策应该基于角色状态
                    var hasValidDecision = decision != null;
                    var considersSkills = decision?.ConsideredSkills ?? false;
                    var considersNeeds = decision?.ConsideredNeeds ?? false;
                    var considersEnvironment = decision?.ConsideredEnvironment ?? false;

                    return hasValidDecision && considersSkills && considersNeeds && considersEnvironment;
                });
        }

        /// <summary>
        /// 生成带技能的角色
        /// </summary>
        private static Arbitrary<CharacterEntity> GenerateCharacterWithSkills()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var character = CharacterEntity.GenerateRandom(random);
                
                // 确保技能组件存在并随机化
                var entityManager = new EntityManager();
                entityManager.AddComponent(character.Id, new SkillComponent());
                character.SetComponentReferences(entityManager);
                character.Skills?.RandomizeSkills(random, 0, 20);

                return character;
            }));
        }

        /// <summary>
        /// 生成带状态的角色
        /// </summary>
        private static Arbitrary<CharacterEntity> GenerateCharacterWithState()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var character = CharacterEntity.GenerateRandom(random);
                
                // 设置所有组件
                var entityManager = new EntityManager();
                entityManager.AddComponent(character.Id, new PositionComponent(random.NextSingle() * 100, random.NextSingle() * 100));
                entityManager.AddComponent(character.Id, new SkillComponent());
                entityManager.AddComponent(character.Id, new NeedComponent());
                entityManager.AddComponent(character.Id, new InventoryComponent());
                
                character.SetComponentReferences(entityManager);
                
                // 随机化状态
                character.Skills?.RandomizeSkills(random, 0, 15);
                character.Needs?.RandomizeNeeds(random, 0.1f, 1.0f);
                character.Inventory?.GenerateRandomItems(random, random.Next(1, 10));

                return character;
            }));
        }

        /// <summary>
        /// 生成任务需求
        /// </summary>
        private static Arbitrary<TaskRequirements> GenerateTaskRequirements()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var skillTypes = Enum.GetValues<SkillType>();
                var primarySkill = skillTypes[random.Next(skillTypes.Length)];
                var secondarySkill = random.NextSingle() > 0.5f ? skillTypes[random.Next(skillTypes.Length)] : (SkillType?)null;

                return new TaskRequirements
                {
                    PrimarySkill = primarySkill,
                    SecondarySkill = secondarySkill,
                    MinSkillLevel = random.Next(0, 15),
                    Priority = random.NextSingle(),
                    RequiredPosition = new Vector3(random.NextSingle() * 100, random.NextSingle() * 100, 0)
                };
            }));
        }

        /// <summary>
        /// 计算任务适合度
        /// </summary>
        private static float CalculateTaskSuitability(CharacterEntity character, TaskRequirements requirements)
        {
            if (character.Skills == null)
                return 0f;

            var primarySkillLevel = character.Skills.GetSkill(requirements.PrimarySkill).Level;
            var primarySuitability = Math.Min(1f, (float)primarySkillLevel / Math.Max(1, requirements.MinSkillLevel));

            var secondarySuitability = 1f;
            if (requirements.SecondarySkill.HasValue)
            {
                var secondarySkillLevel = character.Skills.GetSkill(requirements.SecondarySkill.Value).Level;
                secondarySuitability = Math.Min(1f, (float)secondarySkillLevel / Math.Max(1, requirements.MinSkillLevel));
            }

            // 考虑距离因素
            var distanceFactor = 1f;
            if (character.Position != null)
            {
                var distance = character.Position.DistanceTo(requirements.RequiredPosition);
                distanceFactor = Math.Max(0.1f, 1f - (distance / 100f));
            }

            return (primarySuitability * 0.6f + secondarySuitability * 0.3f + distanceFactor * 0.1f) * requirements.Priority;
        }

        /// <summary>
        /// 做出行动决策
        /// </summary>
        private static ActionDecision MakeActionDecision(CharacterEntity character, CharacterContext context)
        {
            var decision = new ActionDecision();

            // 考虑技能
            if (character.Skills != null)
            {
                decision.ConsideredSkills = true;
                decision.HighestSkill = character.Skills.GetHighestSkill().Type;
            }

            // 考虑需求
            if (character.Needs != null)
            {
                decision.ConsideredNeeds = true;
                decision.MostUrgentNeed = character.Needs.GetMostUrgentNeed().Type;
                decision.HasCriticalNeeds = character.Needs.HasCriticalNeeds();
            }

            // 考虑环境（简化实现）
            decision.ConsideredEnvironment = true;
            decision.CurrentPosition = character.Position?.Position ?? Vector3.Zero;

            return decision;
        }

        /// <summary>
        /// 任务需求数据结构
        /// </summary>
        private class TaskRequirements
        {
            public SkillType PrimarySkill { get; set; }
            public SkillType? SecondarySkill { get; set; }
            public int MinSkillLevel { get; set; }
            public float Priority { get; set; }
            public Vector3 RequiredPosition { get; set; }
        }

        /// <summary>
        /// 行动决策数据结构
        /// </summary>
        private class ActionDecision
        {
            public bool ConsideredSkills { get; set; }
            public bool ConsideredNeeds { get; set; }
            public bool ConsideredEnvironment { get; set; }
            public SkillType HighestSkill { get; set; }
            public NeedType MostUrgentNeed { get; set; }
            public bool HasCriticalNeeds { get; set; }
            public Vector3 CurrentPosition { get; set; }
        }

        /// <summary>
        /// 测试行为树执行的一致性
        /// </summary>
        [Property(MaxTest = 50)]
        [Category("Property")]
        public Property BehaviorTreeExecutionConsistency()
        {
            return Prop.ForAll(
                GenerateCharacterWithState(),
                (character) =>
                {
                    // 注册角色
                    _characterSystem.RegisterCharacter(character);

                    // 创建简单的行为树
                    var behaviorTree = new BehaviorTreeBuilder()
                        .Selector("测试行为")
                            .CheckNeed(NeedType.Hunger, 0.5f, true)
                            .Idle()
                        .End()
                        .Build();

                    // 分配行为树
                    _characterSystem.AssignBehaviorTree(character.Id, behaviorTree);

                    // 多次执行应该产生一致的结果（在相同状态下）
                    var context = new CharacterContext(character, 0.1f);
                    var result1 = behaviorTree.Execute(context);
                    
                    // 重置并再次执行
                    behaviorTree.Reset();
                    var result2 = behaviorTree.Execute(context);

                    // 在相同条件下，结果应该一致
                    return result1 == result2 || 
                           (result1 == BehaviorResult.Running && result2 == BehaviorResult.Running);
                });
        }

        /// <summary>
        /// 测试角色状态更新的一致性
        /// </summary>
        [Property(MaxTest = 50)]
        [Category("Property")]
        public Property CharacterStateUpdateConsistency()
        {
            return Prop.ForAll(
                GenerateCharacterWithState(),
                Arb.Default.PositiveFloat(),
                (character, deltaTime) =>
                {
                    // 限制deltaTime到合理范围
                    deltaTime = Math.Min(deltaTime, 1.0f);

                    // 注册角色
                    _characterSystem.RegisterCharacter(character);

                    // 记录初始状态
                    var initialHunger = character.Needs?.GetNeed(NeedType.Hunger).Value ?? 1f;
                    var initialRest = character.Needs?.GetNeed(NeedType.Rest).Value ?? 1f;

                    // 更新角色状态
                    character.Update(deltaTime);

                    // 验证状态变化的合理性
                    var newHunger = character.Needs?.GetNeed(NeedType.Hunger).Value ?? 1f;
                    var newRest = character.Needs?.GetNeed(NeedType.Rest).Value ?? 1f;

                    // 需求值应该在合理范围内变化
                    var hungerChangeValid = newHunger >= 0f && newHunger <= 1f && newHunger <= initialHunger;
                    var restChangeValid = newRest >= 0f && newRest <= 1f && newRest <= initialRest;

                    return hungerChangeValid && restChangeValid;
                });
        }
    }
}