using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Characters.BehaviorTree;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Characters
{
    /// <summary>
    /// 角色系统集成测试
    /// </summary>
    [TestFixture]
    public class CharacterSystemIntegrationTests : TestBase
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

        [Test]
        public void RegisterCharacter_ShouldAddCharacterWithAllComponents()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");

            // Act
            _characterSystem.RegisterCharacter(character);

            // Assert
            var retrievedCharacter = _characterSystem.GetCharacter(character.Id);
            Assert.That(retrievedCharacter, Is.Not.Null);
            Assert.That(retrievedCharacter.Name, Is.EqualTo("测试角色"));
            Assert.That(retrievedCharacter.Position, Is.Not.Null);
            Assert.That(retrievedCharacter.Skills, Is.Not.Null);
            Assert.That(retrievedCharacter.Needs, Is.Not.Null);
            Assert.That(retrievedCharacter.Inventory, Is.Not.Null);
        }

        [Test]
        public void UnregisterCharacter_ShouldRemoveCharacterAndComponents()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _characterSystem.RegisterCharacter(character);

            // Act
            _characterSystem.UnregisterCharacter(character.Id);

            // Assert
            var retrievedCharacter = _characterSystem.GetCharacter(character.Id);
            Assert.That(retrievedCharacter, Is.Null);
        }

        [Test]
        public void AssignBehaviorTree_ShouldSetCharacterBehaviorTree()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _characterSystem.RegisterCharacter(character);

            var behaviorTree = new BehaviorTreeBuilder()
                .Selector("测试行为")
                    .Idle()
                .End()
                .Build();

            // Act
            _characterSystem.AssignBehaviorTree(character.Id, behaviorTree);

            // Assert
            Assert.That(character.BehaviorTree, Is.Not.Null);
            var status = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(status, Is.Not.Null);
            Assert.That(status.IsActive, Is.True);
        }

        [Test]
        public void UpdateCharacterSystem_ShouldUpdateAllCharacters()
        {
            // Arrange
            var character1 = new CharacterEntity("角色1");
            var character2 = new CharacterEntity("角色2");
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);

            // 设置初始需求值
            character1.Needs?.SetNeedValue(NeedType.Hunger, 1.0f);
            character2.Needs?.SetNeedValue(NeedType.Hunger, 1.0f);

            // Act
            _characterSystem.Update(1.0f); // 更新1秒

            // Assert
            // 需求值应该有所下降
            var hunger1 = character1.Needs?.GetNeed(NeedType.Hunger).Value ?? 1f;
            var hunger2 = character2.Needs?.GetNeed(NeedType.Hunger).Value ?? 1f;
            
            Assert.That(hunger1, Is.LessThan(1.0f));
            Assert.That(hunger2, Is.LessThan(1.0f));
        }

        [Test]
        public void CreateRandomCharacter_ShouldGenerateValidCharacter()
        {
            // Arrange
            var random = new Random(42); // 固定种子以确保可重现性

            // Act
            var character = _characterSystem.CreateRandomCharacter(random);

            // Assert
            Assert.That(character.Name, Is.Not.Empty);
            Assert.That(character.Age, Is.GreaterThan(0));
            Assert.That(character.Height, Is.GreaterThan(0));
        }

        [Test]
        public void BehaviorTreeExecution_ShouldHandleNeedBasedDecisions()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _characterSystem.RegisterCharacter(character);

            // 设置低饥饿值
            character.Needs?.SetNeedValue(NeedType.Hunger, 0.2f);

            var behaviorTree = new BehaviorTreeBuilder()
                .Selector("需求处理")
                    .Sequence("处理饥饿")
                        .CheckNeed(NeedType.Hunger, 0.3f, true)
                        .SatisfyNeed(NeedType.Hunger, 0.5f, 0.1f) // 快速满足
                    .End()
                    .Idle()
                .End()
                .Build();

            _characterSystem.AssignBehaviorTree(character.Id, behaviorTree);

            // Act
            _characterSystem.Update(0.2f); // 更新足够长时间完成满足需求

            // Assert
            var hungerAfter = character.Needs?.GetNeed(NeedType.Hunger).Value ?? 0f;
            Assert.That(hungerAfter, Is.GreaterThan(0.2f)); // 饥饿值应该有所提升
        }

        [Test]
        public void GetSystemStats_ShouldReturnValidStatistics()
        {
            // Arrange
            var character1 = new CharacterEntity("角色1");
            var character2 = new CharacterEntity("角色2");
            
            _characterSystem.RegisterCharacter(character1);
            _characterSystem.RegisterCharacter(character2);

            // Act
            var stats = _characterSystem.GetStats();

            // Assert
            Assert.That(stats.TotalCharacters, Is.EqualTo(2));
            Assert.That(stats.ActiveBehaviorTrees, Is.EqualTo(2));
            Assert.That(stats.AverageHappiness, Is.GreaterThanOrEqualTo(0f));
            Assert.That(stats.AverageHappiness, Is.LessThanOrEqualTo(1f));
            Assert.That(stats.AvailableTemplates, Is.GreaterThan(0));
        }

        [Test]
        public void BehaviorTreeTemplates_ShouldBeAvailable()
        {
            // Act
            var templates = _characterSystem.GetBehaviorTreeTemplates().ToList();

            // Assert
            Assert.That(templates, Contains.Item("default"));
            Assert.That(templates, Contains.Item("worker"));
            Assert.That(templates.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void CharacterMovement_ShouldUpdatePosition()
        {
            // Arrange
            var character = new CharacterEntity("移动角色");
            _characterSystem.RegisterCharacter(character);

            var startPosition = new Vector3(0, 0, 0);
            var targetPosition = new Vector3(10, 10, 0);

            character.Position?.StartMovementTo(targetPosition, 0f);

            // Act
            _characterSystem.Update(1.0f); // 更新1秒

            // Assert
            var currentPosition = character.Position?.Position ?? Vector3.Zero;
            var distanceFromStart = Vector3.Distance(currentPosition, startPosition);
            
            Assert.That(distanceFromStart, Is.GreaterThan(0)); // 应该已经移动了
        }

        [Test]
        public void PauseAndResumeCharacterBehavior_ShouldWork()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _characterSystem.RegisterCharacter(character);

            // Act & Assert
            var statusBefore = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(statusBefore?.IsActive, Is.True);

            _characterSystem.PauseCharacterBehavior(character.Id);
            var statusPaused = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(statusPaused?.IsActive, Is.False);

            _characterSystem.ResumeCharacterBehavior(character.Id);
            var statusResumed = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(statusResumed?.IsActive, Is.True);
        }
    }
}