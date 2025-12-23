using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldFramework.Core.Performance;
using RimWorldFramework.Core.Resources;

namespace RimWorldFramework.Tests.Performance
{
    /// <summary>
    /// 性能管理系统集成测试
    /// 验证性能监控、资源管理和自动降级的完整工作流程
    /// </summary>
    [TestFixture]
    public class PerformanceIntegrationTests
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

        [Test]
        public async Task CompletePerformanceWorkflow_ShouldWorkEndToEnd()
        {
            // 1. 启动性能监控
            await _performanceMonitor.StartMonitoringAsync();
            _performanceMonitor.EnableAutoDegradation(true);

            // 2. 设置性能阈值
            _performanceMonitor.SetPerformanceThresholds(new PerformanceThresholds
            {
                MinAcceptableFPS = 30.0,
                WarningFPS = 45.0,
                MaxCPUUsage = 80.0,
                MaxMemoryUsageMB = 1024.0,
                MaxGCPressure = 0.1
            });

            // 3. 记录正常性能数据
            for (int i = 0; i < 10; i++)
            {
                _performanceMonitor.RecordFrameTime(16.67); // 60 FPS
                _performanceMonitor.RecordCustomMetric("CPUUsage", 50.0);
                _performanceMonitor.RecordCustomMetric("MemoryUsage", 512.0);
                await Task.Delay(10);
            }

            // 4. 验证初始性能指标
            var initialMetrics = _performanceMonitor.GetCurrentMetrics();
            Assert.That(initialMetrics.CurrentFPS, Is.GreaterThan(50));

            // 5. 模拟性能下降
            var warningTriggered = false;
            var degradationTriggered = false;

            _performanceMonitor.PerformanceWarning += (sender, args) => warningTriggered = true;
            _performanceMonitor.PerformanceDegradation += (sender, args) => degradationTriggered = true;

            // 记录低性能数据
            for (int i = 0; i < 20; i++)
            {
                _performanceMonitor.RecordFrameTime(50.0); // 20 FPS
                _performanceMonitor.RecordCustomMetric("CPUUsage", 90.0);
                _performanceMonitor.RecordCustomMetric("MemoryUsage", 1200.0);
                await Task.Delay(10);
            }

            // 6. 等待监控系统处理
            await Task.Delay(1500);

            // 7. 验证警告和降级被触发
            Assert.That(warningTriggered, Is.True, "Performance warning should be triggered");

            // 8. 手动触发降级
            await _performanceMonitor.TriggerDegradationAsync(DegradationLevel.Moderate);

            // 9. 验证降级设置
            var degradedSettings = _performanceMonitor.GetRecommendedSettings();
            Assert.That(degradedSettings.TargetFPS, Is.LessThan(60));
            Assert.That(degradedSettings.RenderQuality, Is.LessThan(QualityLevel.High));

            // 10. 恢复性能
            await _performanceMonitor.RestorePerformanceAsync();

            // 11. 验证设置恢复
            var restoredSettings = _performanceMonitor.GetRecommendedSettings();
            Assert.That(restoredSettings.TargetFPS, Is.EqualTo(60));

            // 12. 停止监控
            await _performanceMonitor.StopMonitoringAsync();
        }

        [Test]
        public async Task ResourceManagement_ShouldHandleMemoryPressure()
        {
            // 1. 设置较低的内存限制
            _resourceManager.SetMemoryLimit(256); // 256 MB

            var lowMemoryDetected = false;
            _resourceManager.LowMemoryDetected += (sender, args) =>
            {
                lowMemoryDetected = true;
                Assert.That(args.CurrentMemoryMB, Is.GreaterThan(0));
                Assert.That(args.SuggestedActions, Is.Not.Empty);
            };

            // 2. 加载大量资源
            var resourcePaths = Enumerable.Range(0, 50)
                .Select(i => $"test_resource_{i}.dat")
                .ToList();

            await _resourceManager.PreloadResourcesAsync(resourcePaths);

            // 3. 验证资源加载
            var stats = _resourceManager.GetResourceStatistics();
            Assert.That(stats.LoadedResourceCount, Is.GreaterThan(0));

            // 4. 检查内存使用
            var memoryUsage = _resourceManager.GetMemoryUsage();
            Assert.That(memoryUsage.UsedMemoryBytes, Is.GreaterThan(0));

            // 5. 触发清理
            await _resourceManager.CleanupUnusedResourcesAsync();

            // 6. 验证清理效果
            var statsAfterCleanup = _resourceManager.GetResourceStatistics();
            Assert.That(statsAfterCleanup.LoadedResourceCount, Is.LessThanOrEqualTo(stats.LoadedResourceCount));
        }

        [Test]
        public void ObjectPool_ShouldReuseObjects()
        {
            // 1. 获取对象池
            var pool = _resourceManager.GetObjectPool<TestGameObject>();

            // 2. 预热池
            pool.Warmup(10);
            Assert.That(pool.AvailableCount, Is.EqualTo(10));

            // 3. 获取对象
            var obj1 = pool.Get();
            var obj2 = pool.Get();
            Assert.That(pool.AvailableCount, Is.EqualTo(8));

            // 4. 返回对象
            pool.Return(obj1);
            pool.Return(obj2);
            Assert.That(pool.AvailableCount, Is.EqualTo(10));

            // 5. 再次获取对象（应该重用）
            var obj3 = pool.Get();
            Assert.That(obj3, Is.EqualTo(obj1).Or.EqualTo(obj2));

            // 6. 验证统计信息
            Assert.That(pool.TotalCount, Is.EqualTo(10));
            Assert.That(pool.GetCount, Is.GreaterThan(0));
        }

        [Test]
        public void MemoryPool_ShouldManageCapacity()
        {
            // 1. 获取内存池
            var pool = _resourceManager.GetMemoryPool<TestGameObject>();

            // 2. 获取多个对象
            var objects = new TestGameObject[150]; // 超过默认容量
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = pool.Get();
            }

            // 3. 返回对象
            for (int i = 0; i < objects.Length; i++)
            {
                pool.Return(objects[i]);
            }

            // 4. 验证池容量限制
            Assert.That(pool.AvailableCount, Is.LessThanOrEqualTo(pool.Capacity));
        }

        [Test]
        public async Task PerformanceHistory_ShouldTrackMetrics()
        {
            // 1. 启动监控
            await _performanceMonitor.StartMonitoringAsync();

            // 2. 记录性能数据
            for (int i = 0; i < 30; i++)
            {
                _performanceMonitor.RecordFrameTime(16.67 + (i * 0.5)); // 逐渐增加帧时间
                await Task.Delay(50);
            }

            // 3. 等待数据收集
            await Task.Delay(1000);

            // 4. 获取历史数据
            var history = _performanceMonitor.GetPerformanceHistory(TimeSpan.FromMinutes(1)).ToList();
            Assert.That(history.Count, Is.GreaterThan(0));

            // 5. 验证历史数据包含性能等级
            Assert.That(history.All(h => h.Level != PerformanceLevel.Excellent || h.Level != PerformanceLevel.Good), Is.True);

            // 6. 停止监控
            await _performanceMonitor.StopMonitoringAsync();
        }

        [Test]
        public async Task AutoDegradation_ShouldRespondToPerformanceIssues()
        {
            // 1. 启动监控并启用自动降级
            await _performanceMonitor.StartMonitoringAsync();
            _performanceMonitor.EnableAutoDegradation(true);

            // 2. 设置严格的阈值
            _performanceMonitor.SetPerformanceThresholds(new PerformanceThresholds
            {
                MinAcceptableFPS = 50.0,
                MaxCPUUsage = 60.0,
                MaxMemoryUsageMB = 512.0
            });

            var degradationEvents = new System.Collections.Generic.List<PerformanceDegradationEventArgs>();
            _performanceMonitor.PerformanceDegradation += (sender, args) => degradationEvents.Add(args);

            // 3. 模拟严重性能问题
            for (int i = 0; i < 50; i++)
            {
                _performanceMonitor.RecordFrameTime(100.0); // 10 FPS
                _performanceMonitor.RecordCustomMetric("CPUUsage", 95.0);
                _performanceMonitor.RecordCustomMetric("MemoryUsage", 800.0);
                await Task.Delay(20);
            }

            // 4. 等待自动降级触发
            await Task.Delay(2000);

            // 5. 验证降级事件
            Assert.That(degradationEvents.Count, Is.GreaterThan(0));
            Assert.That(degradationEvents.Any(e => e.Level != DegradationLevel.None), Is.True);

            // 6. 停止监控
            await _performanceMonitor.StopMonitoringAsync();
        }

        [Test]
        public async Task IntegratedPerformanceAndResourceManagement_ShouldWorkTogether()
        {
            // 1. 启动性能监控
            await _performanceMonitor.StartMonitoringAsync();

            // 2. 设置资源管理器内存限制
            _resourceManager.SetMemoryLimit(512);

            var performanceWarnings = new System.Collections.Generic.List<PerformanceWarningEventArgs>();
            var lowMemoryEvents = new System.Collections.Generic.List<LowMemoryEventArgs>();

            _performanceMonitor.PerformanceWarning += (sender, args) => performanceWarnings.Add(args);
            _resourceManager.LowMemoryDetected += (sender, args) => lowMemoryEvents.Add(args);

            // 3. 同时施加性能和内存压力
            var loadTasks = Enumerable.Range(0, 100)
                .Select(async i =>
                {
                    await _resourceManager.LoadResourceAsync<string>($"resource_{i}.dat");
                    _performanceMonitor.RecordFrameTime(50.0); // 20 FPS
                    await Task.Delay(10);
                });

            await Task.WhenAll(loadTasks);

            // 4. 等待系统响应
            await Task.Delay(1000);

            // 5. 验证两个系统都检测到了问题
            var memoryUsage = _resourceManager.GetMemoryUsage();
            var performanceMetrics = _performanceMonitor.GetCurrentMetrics();

            Assert.That(memoryUsage.UsedMemoryBytes, Is.GreaterThan(0));
            Assert.That(performanceMetrics.CurrentFPS, Is.LessThan(60));

            // 6. 触发清理和降级
            await _resourceManager.CleanupUnusedResourcesAsync();
            await _performanceMonitor.TriggerDegradationAsync(DegradationLevel.Moderate);

            // 7. 验证系统状态改善
            var cleanedMemoryUsage = _resourceManager.GetMemoryUsage();
            var degradedSettings = _performanceMonitor.GetRecommendedSettings();

            Assert.That(degradedSettings.TargetFPS, Is.LessThan(60));
            Assert.That(degradedSettings.RenderQuality, Is.LessThan(QualityLevel.High));

            // 8. 停止监控
            await _performanceMonitor.StopMonitoringAsync();
        }

        #region 测试辅助类

        public class TestGameObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = "TestObject";
            public float[] Data { get; set; } = new float[256]; // 1KB 数据

            public void Reset()
            {
                Id = 0;
                Name = "TestObject";
                Array.Clear(Data, 0, Data.Length);
            }
        }

        #endregion
    }
}