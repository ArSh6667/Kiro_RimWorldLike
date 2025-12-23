using System;

namespace RimWorldFramework.Tests
{
    /// <summary>
    /// 基础集成测试，验证核心组件能够正常工作
    /// </summary>
    [TestFixture]
    public class BasicIntegrationTests : TestBase
    {
        [Test]
        public void GameFramework_BasicInitialization_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as Microsoft.Extensions.Logging.ILogger<GameFramework>);
            var config = CreateTestConfig();

            // 行动
            AssertDoesNotThrow(() => framework.Initialize(config));

            // 断言
            Assert.That(framework.IsInitialized, Is.True);
            Assert.That(framework.IsRunning, Is.True);
        }

        [Test]
        public void EntityManager_BasicOperations_ShouldWork()
        {
            // 安排
            var entityManager = new EntityManager();

            // 行动
            var entityId = entityManager.CreateEntity();
            var component = new TestComponent { Value = 42 };
            entityManager.AddComponent(entityId, component);

            // 断言
            Assert.That(entityManager.EntityExists(entityId), Is.True);
            Assert.That(entityManager.HasComponent<TestComponent>(entityId), Is.True);
            
            var retrievedComponent = entityManager.GetComponent<TestComponent>(entityId);
            Assert.That(retrievedComponent, Is.Not.Null);
            Assert.That(retrievedComponent!.Value, Is.EqualTo(42));
        }

        [Test]
        public void ComponentSystem_BasicOperations_ShouldWork()
        {
            // 安排
            var componentSystem = new ComponentSystem();

            // 行动
            componentSystem.RegisterComponentType<TestComponent>();
            var component = componentSystem.CreateComponent<TestComponent>();

            // 断言
            Assert.That(componentSystem.IsComponentTypeRegistered<TestComponent>(), Is.True);
            Assert.That(component, Is.Not.Null);
            Assert.That(component, Is.InstanceOf<TestComponent>());
        }

        [Test]
        public void EventBus_BasicOperations_ShouldWork()
        {
            // 安排
            var eventBus = new EventBus();
            bool eventReceived = false;
            string receivedData = string.Empty;

            eventBus.Subscribe<TestEvent>(evt =>
            {
                eventReceived = true;
                receivedData = evt.Data;
            });

            // 行动
            eventBus.Publish(new TestEvent("test data"));

            // 断言
            Assert.That(eventReceived, Is.True);
            Assert.That(receivedData, Is.EqualTo("test data"));
        }

        [Test]
        public void ConfigManager_BasicOperations_ShouldWork()
        {
            // 安排
            var configManager = new ConfigManager(Logger as Microsoft.Extensions.Logging.ILogger<ConfigManager>);
            var testConfig = CreateTestConfig();

            // 行动
            AssertDoesNotThrow(() => configManager.UpdateConfig(testConfig));
            var retrievedConfig = configManager.GetConfig();

            // 断言
            Assert.That(retrievedConfig, Is.Not.Null);
            Assert.That(retrievedConfig.Graphics.Width, Is.EqualTo(testConfig.Graphics.Width));
            Assert.That(retrievedConfig.Graphics.Height, Is.EqualTo(testConfig.Graphics.Height));
        }

        [Test]
        public void SystemManager_BasicOperations_ShouldWork()
        {
            // 安排
            var systemManager = new SystemManager(Logger as Microsoft.Extensions.Logging.ILogger<SystemManager>);
            var testSystem = new TestGameSystem();

            // 行动
            systemManager.RegisterSystem(testSystem);
            systemManager.InitializeAllSystems();

            // 断言
            Assert.That(systemManager.HasSystem<TestGameSystem>(), Is.True);
            Assert.That(systemManager.GetSystem<TestGameSystem>(), Is.Not.Null);
            Assert.That(testSystem.IsInitialized, Is.True);
        }
    }

    /// <summary>
    /// 测试用游戏系统
    /// </summary>
    public class TestGameSystem : GameSystem
    {
        public override int Priority => 100;
        public override string Name => "TestGameSystem";

        protected override void OnInitialize()
        {
            // 初始化逻辑
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 更新逻辑
        }

        protected override void OnShutdown()
        {
            // 关闭逻辑
        }
    }
}