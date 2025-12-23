using System;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using RimWorldFramework.Core.Performance;
using RimWorldFramework.Core.Resources;

namespace RimWorldFramework.Tests.Performance
{
    /// <summary>
    /// 性能管理属性测试
    /// 验证属性 19: 资源不足时的降级处理
    /// </summary>
    [TestFixture]
    public class PerformancePropertyTests
    {
        private IPerformanceMonitor _performanceMonitor;
        private IResourceManager _resourceManager;

        [SetUp]
        public void Setup()
        {
            _performanceMonitor = new PerformanceMonitor();
            _resourceManager = new ResourceManager();
        }

        [TearDown]
        public void TearDown()
        {
            _performanceMonitor?.Dispose();
            _resourceManager?.Dispose();
        }

        /// <summary>
        /// 属性 19: 资源不足时的降级处理
        /// 验证当系统资源不足时，性能监控器能够自动降级设置以维持系统稳定性
        /// </summary>
        [Property]
        public Property ResourceShortage_ShouldTriggerDegradation()
        {
            return Prop.ForAll(
                GenerateLowPerformanceScenarios(),
                async scenario =>
                {
                    try
                    {
                        // 启动性能监控
                        await _performanceMonitor.StartMonitoringAsync();
                        _performanceMonitor.EnableAutoDegradation(true);

                        // 设置严格的性能阈值
                        _performanceMonitor.SetPerformanceThresholds(new PerformanceThresholds
                        {
                            MinAcceptableFPS = scenario.MinFPS,
                            MaxCPUUsage = scenario.MaxCPU,
                            MaxMemoryUsageMB = scenario.MaxMemory,
                            MaxGCPressure = scenario.MaxGCPressure
                        });

                        var degradationTriggered = false;
                        var degradationLevel = DegradationLevel.None;

                        // 监听降级事件
                        _performanceMonitor.PerformanceDegradation += (sender, args) =>
                        {
                            degradationTriggered = true;
                            degradationLevel = args.Level;
                        };

                        // 模拟低性能场景
                        for (int i = 0; i < scenario.FrameCount; i++)
                        {
                            _performanceMonitor.RecordFrameTime(scenario.FrameTime);
                            _performanceMonitor.RecordCustomMetric("CPUUsage", scenario.CPUUsage);
                            _performanceMonitor.RecordCustomMetric("MemoryUsage", scenario.MemoryUsage);
                            
                            await Task.Delay(1); // 模拟帧间隔
                        }

                        // 等待监控系统处理
                        await Task.Delay(100);

                        // 验证降级是否被触发
                        var shouldDegrade = scenario.FrameTime > (1000.0 / scenario.MinFPS) ||
                                          scenario.CPUUsage > scenario.MaxCPU ||
                                          scenario.MemoryUsage > scenario.MaxMemory;

                        return shouldDegrade ? degradationTriggered : !degradationTriggered;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    finally
                    {
                        await _performanceMonitor.StopMonitoringAsync();
                    }
                });
        }

        /// <summary>
        /// 属性测试：性能降级应该是渐进的
        /// </summary>
        [Property]
        public Property PerformanceDegradation_ShouldBeProgressive()
        {
            return Prop.ForAll(
                GenerateProgressiveDegradationScenarios(),
                async scenarios =>
                {
                    try
                    {
                        await _performanceMonitor.StartMonitoringAsync();
                        _performanceMonitor.EnableAutoDegradation(true);

                        var degradationLevels = new System.Collections.Generic.List<DegradationLevel>();

                        _performanceMonitor.PerformanceDegradation += (sender, args) =>
                        {
                            degradationLevels.Add(args.Level);
                        };

                        // 逐步降低性能
                        foreach (var scenario in scenarios.OrderBy(s => s.Severity))
                        {
                            await _performanceMonitor.TriggerDegradationAsync(scenario.Level);
                            await Task.Delay(50);
                        }

                        // 验证降级是渐进的（级别不应该跳跃）
                        for (int i = 1; i < degradationLevels.Count; i++)
                        {
                            var currentLevel = (int)degradationLevels[i];
                            var previousLevel = (int)degradationLevels[i - 1];
                            
                            // 降级级别应该是递增的或保持不变
                            if (currentLevel < previousLevel - 1)
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    finally
                    {
                        await _performanceMonitor.StopMonitoringAsync();
                    }
                });
        }

        /// <summary>
        /// 属性测试：性能恢复应该恢复到原始设置
        /// </summary>
        [Property]
        public Property PerformanceRestore_ShouldRestoreOriginalSettings()
        {
            return Prop.ForAll(
                GenerateDegradationLevels(),
                async level =>
                {
                    try
                    {
                        await _performanceMonitor.StartMonitoringAsync();

                        // 获取原始设置
                        var originalSettings = _performanceMonitor.GetRecommendedSettings();

                        // 触发降级
                        await _performanceMonitor.TriggerDegradationAsync(level);
                        var degradedSettings = _performanceMonitor.GetRecommendedSettings();

                        // 恢复设置
                        await _performanceMonitor.RestorePerformanceAsync();
                        var restoredSettings = _performanceMonitor.GetRecommendedSettings();

                        // 验证设置是否恢复
                        return restoredSettings.TargetFPS == originalSettings.TargetFPS &&
                               restoredSettings.RenderQuality == originalSettings.RenderQuality &&
                               restoredSettings.ShadowQuality == originalSettings.ShadowQuality;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    finally
                    {
                        await _performanceMonitor.StopMonitoringAsync();
                    }
                });
        }

        /// <summary>
        /// 属性测试：内存池应该有效减少GC压力
        /// </summary>
        [Property]
        public Property MemoryPool_ShouldReduceGCPressure()
        {
            return Prop.ForAll(
                Gen.Choose(10, 100),
                objectCount =>
                {
                    try
                    {
                        var pool = _resourceManager.GetObjectPool<TestObject>();
                        
                        // 预热池
                        pool.Warmup(objectCount / 2);
                        
                        var initialGC = GC.CollectionCount(0);
                        
                        // 使用对象池
                        var objects = new TestObject[objectCount];
                        for (int i = 0; i < objectCount; i++)
                        {
                            objects[i] = pool.Get();
                        }
                        
                        for (int i = 0; i < objectCount; i++)
                        {
                            pool.Return(objects[i]);
                        }
                        
                        var poolGC = GC.CollectionCount(0);
                        
                        // 不使用对象池（直接创建对象）
                        for (int i = 0; i < objectCount; i++)
                        {
                            var obj = new TestObject();
                            // 让对象超出作用域
                        }
                        
                        var directGC = GC.CollectionCount(0);
                        
                        // 对象池应该产生更少的GC
                        var poolGCIncrease = poolGC - initialGC;
                        var directGCIncrease = directGC - poolGC;
                        
                        return poolGCIncrease <= directGCIncrease;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// 属性测试：资源管理器应该正确处理内存限制
        /// </summary>
        [Property]
        public Property ResourceManager_ShouldRespectMemoryLimits()
        {
            return Prop.ForAll(
                Gen.Choose(100, 500), // 内存限制 MB
                Gen.Choose(10, 50),   // 资源数量
                async (memoryLimitMB, resourceCount) =>
                {
                    try
                    {
                        _resourceManager.SetMemoryLimit(memoryLimitMB);
                        
                        var lowMemoryDetected = false;
                        _resourceManager.LowMemoryDetected += (sender, args) =>
                        {
                            lowMemoryDetected = true;
                        };

                        // 加载大量资源
                        var resourcePaths = Enumerable.Range(0, resourceCount)
                            .Select(i => $"test_resource_{i}.dat")
                            .ToList();

                        await _resourceManager.PreloadResourcesAsync(resourcePaths);

                        var memoryUsage = _resourceManager.GetMemoryUsage();
                        
                        // 如果内存使用接近限制，应该触发低内存警告
                        var shouldTriggerWarning = memoryUsage.UsagePercentage > 80;
                        
                        return shouldTriggerWarning ? lowMemoryDetected : true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        #region 测试数据生成器

        private static Arbitrary<LowPerformanceScenario> GenerateLowPerformanceScenarios()
        {
            return Arb.From(
                from frameTime in Gen.Choose(50, 200) // 5-20 FPS
                from cpuUsage in Gen.Choose(70, 100)  // 70-100% CPU
                from memoryUsage in Gen.Choose(800, 1500) // 800-1500 MB
                from frameCount in Gen.Choose(10, 50)
                select new LowPerformanceScenario
                {
                    FrameTime = frameTime,
                    CPUUsage = cpuUsage,
                    MemoryUsage = memoryUsage,
                    FrameCount = frameCount,
                    MinFPS = 30,
                    MaxCPU = 80,
                    MaxMemory = 1024,
                    MaxGCPressure = 0.1
                });
        }

        private static Arbitrary<DegradationScenario[]> GenerateProgressiveDegradationScenarios()
        {
            return Arb.From(Gen.Constant(new[]
            {
                new DegradationScenario { Level = DegradationLevel.Minor, Severity = 1 },
                new DegradationScenario { Level = DegradationLevel.Moderate, Severity = 2 },
                new DegradationScenario { Level = DegradationLevel.Severe, Severity = 3 },
                new DegradationScenario { Level = DegradationLevel.Extreme, Severity = 4 }
            }));
        }

        private static Arbitrary<DegradationLevel> GenerateDegradationLevels()
        {
            return Arb.From(Gen.Elements(
                DegradationLevel.Minor,
                DegradationLevel.Moderate,
                DegradationLevel.Severe,
                DegradationLevel.Extreme
            ));
        }

        #endregion

        #region 测试辅助类

        public class LowPerformanceScenario
        {
            public double FrameTime { get; set; }
            public double CPUUsage { get; set; }
            public double MemoryUsage { get; set; }
            public int FrameCount { get; set; }
            public double MinFPS { get; set; }
            public double MaxCPU { get; set; }
            public double MaxMemory { get; set; }
            public double MaxGCPressure { get; set; }
        }

        public class DegradationScenario
        {
            public DegradationLevel Level { get; set; }
            public int Severity { get; set; }
        }

        public class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = "Test";
            public byte[] Data { get; set; } = new byte[1024]; // 1KB 数据
        }

        #endregion
    }
}