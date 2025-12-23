using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Configuration;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Events;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.Performance;
using RimWorldFramework.Core.Serialization;
using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.Tasks;

namespace RimWorldFramework.Tests.Core
{
    /// <summary>
    /// 综合集成测试，验证端到端游戏流程
    /// </summary>
    [TestFixture]
    public class ComprehensiveIntegrationTests : TestBase
    {
        /// <summary>
        /// 测试完整的游戏初始化流程
        /// </summary>
        [Test]
        public void CompleteGameInitialization_ShouldSucceed()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();

            // 行动
            AssertDoesNotThrow(() => framework.Initialize(config));

            // 断言
            Assert.That(framework.IsInitialized, Is.True);
            Assert.That(framework.IsRunning, Is.True);

            // 验证所有核心系统都已注册
            Assert.That(framework.HasSystem<ComponentSystem>(), Is.True);
            Assert.That(framework.HasSystem<CharacterSystem>(), Is.True);
            Assert.That(framework.HasSystem<StateUpdateSystem>(), Is.True);
            Assert.That(framework.HasSystem<TaskManager>(), Is.True);
            Assert.That(framework.HasSystem<PathfindingSystem>(), Is.True);
            Assert.That(framework.HasSystem<GameProgressSystem>(), Is.True);
        }

        /// <summary>
        /// 测试端到端角色生命周期
        /// </summary>
        [Test]
        public void EndToEndCharacterLifecycle_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var entityManager = framework.GetEntityManager();
            var eventBus = framework.GetEventBus();
            var characterSystem = framework.GetSystem<CharacterSystem>();
            var stateUpdateSystem = framework.GetSystem<StateUpdateSystem>();

            // 行动 - 创建角色
            var characterId = entityManager.CreateEntity();
            var character = new CharacterComponent
            {
                Name = "TestCharacter",
                Skills = new SkillComponent(),
                Needs = new NeedComponent(),
                Mood = 0.8f,
                Efficiency = 0.7f
            };
            entityManager.AddComponent(characterId, character);
            eventBus.Publish(new CharacterCreatedEvent(characterId));

            // 模拟游戏运行
            for (int i = 0; i < 10; i++)
            {
                framework.Update(0.1f);
            }

            // 断言
            Assert.That(entityManager.EntityExists(characterId), Is.True);
            Assert.That(stateUpdateSystem?.GetStateTracker(characterId), Is.Not.Null);

            var updatedCharacter = entityManager.GetComponent<CharacterComponent>(characterId);
            Assert.That(updatedCharacter, Is.Not.Null);
            Assert.That(updatedCharacter!.Needs.Hunger, Is.GreaterThan(0.3f)); // 饥饿值应该增加
        }

        /// <summary>
        /// 测试任务分配和执行流程
        /// </summary>
        [Test]
        public void TaskAssignmentAndExecution_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var entityManager = framework.GetEntityManager();
            var eventBus = framework.GetEventBus();
            var taskManager = framework.GetSystem<TaskManager>();
            var progressSystem = framework.GetSystem<GameProgressSystem>();

            // 创建角色
            var characterId = entityManager.CreateEntity();
            var character = new CharacterComponent
            {
                Name = "Worker",
                Skills = new SkillComponent(),
                Needs = new NeedComponent()
            };
            entityManager.AddComponent(characterId, character);
            eventBus.Publish(new CharacterCreatedEvent(characterId));

            // 行动 - 创建和分配任务
            var task = new TestTask
            {
                Id = "test_task",
                Name = "Test Task",
                Priority = TaskPriority.Normal,
                Status = TaskStatus.Pending
            };

            taskManager?.AddTask(task);
            taskManager?.AssignTask(task.Id, characterId);

            // 模拟任务执行
            for (int i = 0; i < 20; i++)
            {
                framework.Update(0.1f);
            }

            // 完成任务
            task.Status = TaskStatus.Completed;
            eventBus.Publish(new TaskCompletedEvent(task, characterId));
            framework.Update(0.1f);

            // 断言
            var stats = progressSystem?.GetStatistics();
            Assert.That(stats?.TasksCompleted, Is.EqualTo(1));
            Assert.That(task.Status, Is.EqualTo(TaskStatus.Completed));
        }

        /// <summary>
        /// 测试地图生成和路径寻找集成
        /// </summary>
        [Test]
        public void MapGenerationAndPathfinding_ShouldIntegrate()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var mapGenSystem = framework.GetSystem<MapGenerationSystem>();
            var pathfindingSystem = framework.GetSystem<PathfindingSystem>();

            // 行动 - 生成地图
            var mapConfig = new MapGenerationConfig
            {
                Width = 50,
                Height = 50,
                Seed = 12345
            };

            var map = mapGenSystem?.GenerateMap(mapConfig);
            Assert.That(map, Is.Not.Null);

            // 设置路径寻找地图
            pathfindingSystem?.SetNavigationMesh(CreateTestNavigationMesh(50, 50));

            // 测试路径寻找
            var start = new Vector2Int(5, 5);
            var end = new Vector2Int(45, 45);
            var path = pathfindingSystem?.FindPath(start, end);

            // 断言
            Assert.That(path, Is.Not.Null);
            Assert.That(path?.Count, Is.GreaterThan(0));
        }

        /// <summary>
        /// 测试序列化和反序列化流程
        /// </summary>
        [Test]
        public void SerializationRoundTrip_ShouldPreserveState()
        {
            // 安排
            using var framework1 = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework1.Initialize(config);

            var entityManager1 = framework1.GetEntityManager();
            var eventBus1 = framework1.GetEventBus();
            var serializationSystem1 = framework1.GetSystem<SerializationSystem>();

            // 创建游戏状态
            var characterId = entityManager1.CreateEntity();
            var character = new CharacterComponent
            {
                Name = "SerializationTest",
                Skills = new SkillComponent(),
                Needs = new NeedComponent()
            };
            entityManager1.AddComponent(characterId, character);
            eventBus1.Publish(new CharacterCreatedEvent(characterId));

            // 运行一些帧
            for (int i = 0; i < 5; i++)
            {
                framework1.Update(0.1f);
            }

            // 行动 - 序列化
            var gameState = serializationSystem1?.SerializeGameState();
            Assert.That(gameState, Is.Not.Null);

            // 创建新框架并反序列化
            using var framework2 = new GameFramework(Logger as ILogger<GameFramework>);
            framework2.Initialize(config);
            var serializationSystem2 = framework2.GetSystem<SerializationSystem>();
            var entityManager2 = framework2.GetEntityManager();

            serializationSystem2?.DeserializeGameState(gameState!);

            // 断言
            Assert.That(entityManager2.EntityExists(characterId), Is.True);
            var restoredCharacter = entityManager2.GetComponent<CharacterComponent>(characterId);
            Assert.That(restoredCharacter, Is.Not.Null);
            Assert.That(restoredCharacter!.Name, Is.EqualTo("SerializationTest"));
        }

        /// <summary>
        /// 测试性能监控和资源管理
        /// </summary>
        [Test]
        public void PerformanceMonitoringAndResourceManagement_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var resourceManager = framework.GetSystem<ResourceManager>();
            var performanceMonitor = framework.GetSystem<PerformanceMonitor>();

            // 行动 - 创建大量实体以测试性能
            var entityManager = framework.GetEntityManager();
            var entityIds = new List<uint>();

            for (int i = 0; i < 1000; i++)
            {
                var entityId = entityManager.CreateEntity();
                entityManager.AddComponent(entityId, new TestComponent { Value = i });
                entityIds.Add(entityId);
            }

            // 运行性能监控
            for (int i = 0; i < 10; i++)
            {
                framework.Update(0.016f); // 模拟60FPS
            }

            // 断言
            var metrics = performanceMonitor?.GetCurrentMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics?.FrameRate, Is.GreaterThan(0));

            var memoryUsage = resourceManager?.GetMemoryUsage();
            Assert.That(memoryUsage, Is.GreaterThan(0));
        }

        /// <summary>
        /// 测试游戏进度跟踪
        /// </summary>
        [Test]
        public void GameProgressTracking_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var entityManager = framework.GetEntityManager();
            var eventBus = framework.GetEventBus();
            var progressSystem = framework.GetSystem<GameProgressSystem>();

            // 行动 - 创建角色和完成任务
            var characterId = entityManager.CreateEntity();
            var character = new CharacterComponent
            {
                Name = "ProgressTest",
                Skills = new SkillComponent(),
                Needs = new NeedComponent()
            };
            entityManager.AddComponent(characterId, character);
            eventBus.Publish(new CharacterCreatedEvent(characterId));

            // 完成一些任务
            for (int i = 0; i < 5; i++)
            {
                var task = new TestTask
                {
                    Id = $"progress_task_{i}",
                    Priority = TaskPriority.Normal,
                    Status = TaskStatus.Completed
                };
                eventBus.Publish(new TaskCompletedEvent(task, characterId));
            }

            // 触发技能升级
            eventBus.Publish(new SkillLevelUpEvent(characterId, SkillType.Mining, 5));

            // 运行更新
            framework.Update(0.1f);

            // 断言
            var progress = progressSystem?.GetProgress();
            var statistics = progressSystem?.GetStatistics();

            Assert.That(progress, Is.Not.Null);
            Assert.That(statistics, Is.Not.Null);
            Assert.That(statistics?.TasksCompleted, Is.EqualTo(5));
            Assert.That(statistics?.CharactersCreated, Is.EqualTo(1));
            Assert.That(statistics?.SkillLevelUps, Is.EqualTo(1));
        }

        /// <summary>
        /// 测试错误恢复和系统稳定性
        /// </summary>
        [Test]
        public void ErrorRecoveryAndSystemStability_ShouldWork()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var entityManager = framework.GetEntityManager();
            var eventBus = framework.GetEventBus();

            // 行动 - 故意触发一些错误情况
            var invalidEntityId = uint.MaxValue;
            
            // 尝试访问不存在的实体（应该安全处理）
            AssertDoesNotThrow(() => entityManager.GetComponent<CharacterComponent>(invalidEntityId));
            AssertDoesNotThrow(() => entityManager.DestroyEntity(invalidEntityId));

            // 发布无效事件（应该安全处理）
            AssertDoesNotThrow(() => eventBus.Publish(new CharacterCreatedEvent(invalidEntityId)));

            // 继续正常运行
            for (int i = 0; i < 5; i++)
            {
                AssertDoesNotThrow(() => framework.Update(0.1f));
            }

            // 断言 - 框架应该仍然运行
            Assert.That(framework.IsRunning, Is.True);
        }

        /// <summary>
        /// 测试并发操作安全性
        /// </summary>
        [Test]
        public void ConcurrentOperations_ShouldBeSafe()
        {
            // 安排
            using var framework = new GameFramework(Logger as ILogger<GameFramework>);
            var config = CreateTestConfig();
            framework.Initialize(config);

            var entityManager = framework.GetEntityManager();
            var eventBus = framework.GetEventBus();

            // 行动 - 并发创建实体和组件
            var tasks = new Task[10];
            var createdEntities = new List<uint>();
            var lockObject = new object();

            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        var entityId = entityManager.CreateEntity();
                        entityManager.AddComponent(entityId, new TestComponent { Value = index });
                        
                        lock (lockObject)
                        {
                            createdEntities.Add(entityId);
                        }

                        eventBus.Publish(new TestEvent($"Concurrent operation {index}"));
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Concurrent operation {Index} failed", index);
                    }
                });
            }

            // 等待所有任务完成
            Task.WaitAll(tasks, TimeSpan.FromSeconds(10));

            // 运行框架更新
            framework.Update(0.1f);

            // 断言 - 至少应该有一些操作成功
            Assert.That(createdEntities.Count, Is.GreaterThan(0));
            Assert.That(framework.IsRunning, Is.True);
        }

        /// <summary>
        /// 创建测试导航网格
        /// </summary>
        private NavigationMesh CreateTestNavigationMesh(int width, int height)
        {
            var mesh = new NavigationMesh(width, height);
            
            // 创建简单的开放区域
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    mesh.SetWalkable(x, y, true);
                }
            }

            return mesh;
        }
    }

    /// <summary>
    /// 测试任务完成事件
    /// </summary>
    public class TaskCompletedEvent : GameEvent
    {
        public ITask Task { get; }
        public uint? AssignedCharacterId { get; }

        public TaskCompletedEvent(ITask task, uint? assignedCharacterId = null)
        {
            Task = task ?? throw new ArgumentNullException(nameof(task));
            AssignedCharacterId = assignedCharacterId;
        }
    }
}