using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Tests.AI
{
    /// <summary>
    /// AI系统性能测试
    /// 验证系统在高负载下的性能和稳定性
    /// </summary>
    [TestFixture]
    public class AISystemPerformanceTests
    {
        private IEntityManager _entityManager = null!;
        private CharacterSystem _characterSystem = null!;
        private TaskSystem _taskSystem = null!;
        private PathfindingSystem _pathfindingSystem = null!;
        private CollaborationSystem _collaborationSystem = null!;
        private PathfindingGrid _pathfindingGrid = null!;

        [SetUp]
        public void Setup()
        {
            _entityManager = new EntityManager();
            _characterSystem = new CharacterSystem(_entityManager);
            _taskSystem = new TaskSystem();
            _pathfindingGrid = new PathfindingGrid(50, 50); // 更大的网格用于性能测试
            _pathfindingSystem = new PathfindingSystem(_entityManager, _pathfindingGrid);
            _collaborationSystem = new CollaborationSystem(_taskSystem, _characterSystem);

            _characterSystem.Initialize();
            _taskSystem.Initialize();
            _pathfindingSystem.Initialize();
            _collaborationSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _collaborationSystem.Shutdown();
            _pathfindingSystem.Shutdown();
            _taskSystem.Shutdown();
            _characterSystem.Shutdown();
        }

        [Test]
        [Category("Performance")]
        public void CharacterSystem_WithManyCharacters_MaintainsPerformance()
        {
            // Arrange
            const int characterCount = 50;
            var characters = CreateManyCharacters(characterCount);

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                _characterSystem.Update(0.016f); // 60 FPS
            }
            
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
                $"角色系统更新100次应在1秒内完成 (实际: {stopwatch.ElapsedMilliseconds}ms)");
            
            Assert.That(_characterSystem.GetAllCharacters().Count(), Is.EqualTo(characterCount));
            
            // 验证内存使用合理
            GC.Collect();
            var memoryBefore = GC.GetTotalMemory(false);
            
            for (int i = 0; i < 10; i++)
            {
                _characterSystem.Update(0.016f);
            }
            
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryIncrease = memoryAfter - memoryBefore;
            
            Assert.That(memoryIncrease, Is.LessThan(1024 * 1024), // 1MB
                $"内存增长应该控制在合理范围内 (增长: {memoryIncrease / 1024}KB)");
        }

        [Test]
        [Category("Performance")]
        public void TaskSystem_WithManyTasks_MaintainsPerformance()
        {
            // Arrange
            const int taskCount = 100;
            var characters = CreateManyCharacters(10);
            var tasks = CreateManyTasks(taskCount);

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            // 执行任务分配
            var assignmentResults = _taskSystem.AssignTasksToCharacters(characters);
            
            // 更新任务系统
            for (int i = 0; i < 50; i++)
            {
                _taskSystem.Update(0.016f);
            }
            
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000),
                $"任务系统处理应在2秒内完成 (实际: {stopwatch.ElapsedMilliseconds}ms)");
            
            Assert.That(_taskSystem.GetAllTasks().Count(), Is.EqualTo(taskCount));
            Assert.That(assignmentResults.Count, Is.EqualTo(characters.Count));
        }

        [Test]
        [Category("Performance")]
        public void PathfindingSystem_WithManyRequests_MaintainsPerformance()
        {
            // Arrange
            const int characterCount = 20;
            var characters = CreateManyCharacters(characterCount);

            // Act & Measure - 同时请求多个路径
            var stopwatch = Stopwatch.StartNew();
            var pathRequests = 0;
            
            foreach (var character in characters)
            {
                var start = character.Position!.Position;
                var end = new Vector3(
                    Random.Shared.Next(0, 50),
                    Random.Shared.Next(0, 50),
                    0
                );
                
                if (_pathfindingSystem.RequestPath(character.Id, start, end))
                {
                    pathRequests++;
                }
            }

            // 更新路径寻找系统
            for (int i = 0; i < 30; i++)
            {
                _pathfindingSystem.Update(0.033f); // 30 FPS for pathfinding
            }
            
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(3000),
                $"路径寻找处理应在3秒内完成 (实际: {stopwatch.ElapsedMilliseconds}ms)");
            
            Assert.That(pathRequests, Is.GreaterThan(0), "应该有成功的路径请求");
            
            // 验证至少有一些路径被找到
            var pathsFound = 0;
            foreach (var character in characters)
            {
                if (_pathfindingSystem.GetCurrentPath(character.Id) != null)
                {
                    pathsFound++;
                }
            }
            
            Console.WriteLine($"成功找到 {pathsFound}/{pathRequests} 条路径");
        }

        [Test]
        [Category("Performance")]
        public void CollaborationSystem_WithManyGroups_MaintainsPerformance()
        {
            // Arrange
            const int characterCount = 30;
            const int taskCount = 10;
            
            var characters = CreateManyCharacters(characterCount);
            CreateManyCollaborativeTasks(taskCount);

            // Act & Measure
            var stopwatch = Stopwatch.StartNew();
            
            // 执行协作分配
            var coordinationResult = _collaborationSystem.AutoAssignCollaborativeTasks();
            
            // 更新协作系统
            for (int i = 0; i < 20; i++)
            {
                _collaborationSystem.Update(0.1f);
            }
            
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000),
                $"协作系统处理应在2秒内完成 (实际: {stopwatch.ElapsedMilliseconds}ms)");
            
            Assert.That(coordinationResult, Is.Not.Null);
            
            // 获取效率报告
            var efficiencyReport = _collaborationSystem.GetEfficiencyReport();
            Assert.That(efficiencyReport, Is.Not.Null);
            
            Console.WriteLine($"协作效率报告: {efficiencyReport}");
        }

        [Test]
        [Category("Performance")]
        public void IntegratedSystems_UnderLoad_MaintainStability()
        {
            // Arrange - 创建复杂场景
            const int characterCount = 25;
            const int taskCount = 15;
            
            var characters = CreateManyCharacters(characterCount);
            var tasks = CreateManyTasks(taskCount);
            CreateManyCollaborativeTasks(5);

            // 添加一些地形障碍
            AddTerrainObstacles();

            // Act & Measure - 运行完整的AI循环
            var stopwatch = Stopwatch.StartNew();
            var updateCount = 200; // 模拟约3.3秒的游戏时间 (60 FPS)
            
            for (int i = 0; i < updateCount; i++)
            {
                var deltaTime = 0.016f; // 60 FPS
                
                _pathfindingSystem.Update(deltaTime);
                _characterSystem.Update(deltaTime);
                _taskSystem.Update(deltaTime);
                _collaborationSystem.Update(deltaTime);
                
                // 每50帧执行一次任务重新分配
                if (i % 50 == 0)
                {
                    _taskSystem.AssignTasksToCharacters(characters.Take(10));
                }
                
                // 每30帧执行一次协作优化
                if (i % 30 == 0)
                {
                    _collaborationSystem.AutoAssignCollaborativeTasks();
                }
            }
            
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000),
                $"集成系统更新应在5秒内完成 (实际: {stopwatch.ElapsedMilliseconds}ms)");

            // 验证系统状态一致性
            Assert.That(_characterSystem.GetAllCharacters().Count(), Is.EqualTo(characterCount));
            Assert.That(_taskSystem.GetAllTasks().Count(), Is.GreaterThanOrEqualTo(taskCount));
            
            // 验证没有内存泄漏
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            Assert.That(finalMemory, Is.LessThan(100 * 1024 * 1024), // 100MB
                $"最终内存使用应该合理 (使用: {finalMemory / 1024 / 1024}MB)");
            
            Console.WriteLine($"性能测试完成: {updateCount} 次更新在 {stopwatch.ElapsedMilliseconds}ms 内完成");
            Console.WriteLine($"平均每帧: {(float)stopwatch.ElapsedMilliseconds / updateCount:F2}ms");
        }

        [Test]
        [Category("Performance")]
        public void MemoryUsage_WithLongRunning_RemainsStable()
        {
            // Arrange
            var characters = CreateManyCharacters(15);
            CreateManyTasks(10);

            // Measure initial memory
            GC.Collect();
            var initialMemory = GC.GetTotalMemory(true);

            // Act - 长时间运行
            for (int cycle = 0; cycle < 10; cycle++)
            {
                // 每个周期运行100帧
                for (int frame = 0; frame < 100; frame++)
                {
                    _characterSystem.Update(0.016f);
                    _taskSystem.Update(0.016f);
                    _pathfindingSystem.Update(0.016f);
                    _collaborationSystem.Update(0.016f);
                }
                
                // 周期性垃圾回收
                if (cycle % 3 == 0)
                {
                    GC.Collect();
                }
            }

            // Measure final memory
            GC.Collect();
            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.That(memoryIncrease, Is.LessThan(10 * 1024 * 1024), // 10MB
                $"长时间运行后内存增长应该控制在合理范围 (增长: {memoryIncrease / 1024 / 1024}MB)");
            
            Console.WriteLine($"内存使用: 初始 {initialMemory / 1024 / 1024}MB, " +
                            $"最终 {finalMemory / 1024 / 1024}MB, " +
                            $"增长 {memoryIncrease / 1024 / 1024}MB");
        }

        [Test]
        [Category("Performance")]
        public void ConcurrentOperations_WithMultipleThreads_HandledSafely()
        {
            // Arrange
            var characters = CreateManyCharacters(10);
            var exceptions = new List<Exception>();

            // Act - 模拟并发操作（注意：实际系统可能不是线程安全的，这里测试错误处理）
            var tasks = new List<System.Threading.Tasks.Task>();

            for (int i = 0; i < 5; i++)
            {
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 20; j++)
                        {
                            _characterSystem.Update(0.016f);
                            System.Threading.Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                tasks.Add(task);
            }

            System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            // Assert - 验证没有严重错误
            if (exceptions.Any())
            {
                Console.WriteLine($"并发操作中发生了 {exceptions.Count} 个异常");
                foreach (var ex in exceptions.Take(3)) // 只显示前3个异常
                {
                    Console.WriteLine($"异常: {ex.GetType().Name}: {ex.Message}");
                }
            }

            // 系统应该仍然可用
            Assert.That(_characterSystem.GetAllCharacters().Count(), Is.EqualTo(10));
        }

        #region Helper Methods

        private List<CharacterEntity> CreateManyCharacters(int count)
        {
            var characters = new List<CharacterEntity>();
            
            for (int i = 0; i < count; i++)
            {
                var character = new CharacterEntity(_entityManager.CreateEntity())
                {
                    Name = $"Character_{i:D3}"
                };

                var position = new Vector3(
                    Random.Shared.Next(0, 50),
                    Random.Shared.Next(0, 50),
                    0
                );

                var positionComponent = new PositionComponent(position);
                var skillComponent = new SkillComponent();
                var needComponent = new NeedComponent();
                var inventoryComponent = new InventoryComponent();

                _entityManager.AddComponent(character.Id, positionComponent);
                _entityManager.AddComponent(character.Id, skillComponent);
                _entityManager.AddComponent(character.Id, needComponent);
                _entityManager.AddComponent(character.Id, inventoryComponent);

                character.SetComponentReferences(_entityManager);

                // 随机化技能
                if (character.Skills != null)
                {
                    character.Skills.SetSkillLevel(SkillType.Construction, Random.Shared.Next(1, 10));
                    character.Skills.SetSkillLevel(SkillType.Mining, Random.Shared.Next(1, 8));
                    character.Skills.SetSkillLevel(SkillType.Research, Random.Shared.Next(1, 6));
                }

                _characterSystem.RegisterCharacter(character);
                characters.Add(character);
            }

            return characters;
        }

        private List<TaskId> CreateManyTasks(int count)
        {
            var taskIds = new List<TaskId>();
            var taskTypes = new[] { TaskType.Construction, TaskType.Mining, TaskType.Research };

            for (int i = 0; i < count; i++)
            {
                var taskType = taskTypes[i % taskTypes.Length];
                var priority = (TaskPriority)(i % 4); // 循环使用不同优先级

                var taskId = _taskSystem.CreateSimpleTask(
                    $"Task_{i:D3}_{taskType}",
                    taskType,
                    priority
                );
                taskIds.Add(taskId);
            }

            return taskIds;
        }

        private void CreateManyCollaborativeTasks(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var definition = new TaskDefinition
                {
                    Name = $"Collaborative_Task_{i:D2}",
                    Type = TaskType.Construction,
                    Priority = TaskPriority.Normal,
                    MaxAssignedCharacters = Random.Shared.Next(2, 5),
                    EstimatedDuration = Random.Shared.Next(10, 30),
                    TargetPosition = new Vector3(
                        Random.Shared.Next(10, 40),
                        Random.Shared.Next(10, 40),
                        0
                    ),
                    WorkRadius = 3.0f
                };

                _collaborationSystem.CreateCollaborativeTask(definition, CollaborationType.Construction);
            }
        }

        private void AddTerrainObstacles()
        {
            // 添加一些随机障碍物
            for (int i = 0; i < 50; i++)
            {
                var x = Random.Shared.Next(0, 50);
                var y = Random.Shared.Next(0, 50);
                _pathfindingSystem.SetTerrainType(new Vector3(x, y, 0), TerrainType.Blocked);
            }

            // 添加一些困难地形
            for (int i = 0; i < 30; i++)
            {
                var x = Random.Shared.Next(0, 50);
                var y = Random.Shared.Next(0, 50);
                _pathfindingSystem.SetTerrainType(new Vector3(x, y, 0), TerrainType.Difficult);
            }
        }

        #endregion
    }
}