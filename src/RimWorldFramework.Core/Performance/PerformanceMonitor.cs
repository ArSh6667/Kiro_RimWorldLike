using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Performance
{
    /// <summary>
    /// 性能监控器实现
    /// 负责监控游戏性能指标并提供自动降级机制
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ConcurrentQueue<PerformanceSnapshot> _performanceHistory;
        private readonly ConcurrentQueue<double> _frameTimeHistory;
        private readonly ConcurrentDictionary<string, double> _customMetrics;
        private readonly Timer _monitoringTimer;
        private readonly PerformanceCounter _cpuCounter;
        private readonly object _lockObject = new object();

        private PerformanceThresholds _thresholds;
        private PerformanceSettings _currentSettings;
        private PerformanceSettings _originalSettings;
        private bool _isMonitoring;
        private bool _autoDegradationEnabled;
        private DegradationLevel _currentDegradationLevel;
        private DateTime _lastDegradationTime;

        // 性能统计
        private double _currentFPS;
        private double _averageFPS;
        private double _minFPS = double.MaxValue;
        private double _maxFPS = double.MinValue;
        private long _frameCount;
        private DateTime _lastFPSUpdate;

        public event EventHandler<PerformanceWarningEventArgs> PerformanceWarning;
        public event EventHandler<PerformanceDegradationEventArgs> PerformanceDegradation;

        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceMonitor()
        {
            _performanceHistory = new ConcurrentQueue<PerformanceSnapshot>();
            _frameTimeHistory = new ConcurrentQueue<double>();
            _customMetrics = new ConcurrentDictionary<string, double>();
            
            _thresholds = new PerformanceThresholds();
            _currentSettings = new PerformanceSettings();
            _originalSettings = new PerformanceSettings();
            
            _isMonitoring = false;
            _autoDegradationEnabled = true;
            _currentDegradationLevel = DegradationLevel.None;
            _lastDegradationTime = DateTime.MinValue;
            
            _frameCount = 0;
            _lastFPSUpdate = DateTime.UtcNow;

            // 初始化CPU性能计数器
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // 第一次调用通常返回0，需要预热
            }
            catch (Exception)
            {
                // 如果无法创建性能计数器，使用备用方案
                _cpuCounter = null;
            }

            // 创建监控定时器（每秒更新一次）
            _monitoringTimer = new Timer(UpdatePerformanceMetrics, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 开始监控
        /// </summary>
        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _lastFPSUpdate = DateTime.UtcNow;
            
            // 启动监控定时器
            _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            
            // 停止监控定时器
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取当前性能指标
        /// </summary>
        /// <returns>性能指标</returns>
        public PerformanceMetrics GetCurrentMetrics()
        {
            var process = Process.GetCurrentProcess();
            var gcMemory = GC.GetTotalMemory(false);

            return new PerformanceMetrics
            {
                CurrentFPS = _currentFPS,
                AverageFPS = _averageFPS,
                MinFPS = _minFPS == double.MaxValue ? 0 : _minFPS,
                MaxFPS = _maxFPS == double.MinValue ? 0 : _maxFPS,
                FrameTime = _currentFPS > 0 ? 1000.0 / _currentFPS : 0,
                CPUUsage = GetCPUUsage(),
                MemoryUsageMB = gcMemory / (1024.0 * 1024.0),
                GCPressure = CalculateGCPressure(),
                RenderTime = _customMetrics.GetValueOrDefault("RenderTime", 0),
                UpdateTime = _customMetrics.GetValueOrDefault("UpdateTime", 0),
                CustomMetrics = new Dictionary<string, double>(_customMetrics),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 获取性能历史数据
        /// </summary>
        /// <param name="duration">历史数据时长</param>
        /// <returns>性能历史数据</returns>
        public IEnumerable<PerformanceSnapshot> GetPerformanceHistory(TimeSpan duration)
        {
            var cutoffTime = DateTime.UtcNow - duration;
            return _performanceHistory.Where(snapshot => snapshot.Timestamp >= cutoffTime).ToList();
        }

        /// <summary>
        /// 记录帧时间
        /// </summary>
        /// <param name="frameTime">帧时间（毫秒）</param>
        public void RecordFrameTime(double frameTime)
        {
            if (!_isMonitoring)
                return;

            // 添加到帧时间历史
            _frameTimeHistory.Enqueue(frameTime);
            
            // 保持历史记录在合理范围内
            while (_frameTimeHistory.Count > 1000)
            {
                _frameTimeHistory.TryDequeue(out _);
            }

            // 更新FPS统计
            Interlocked.Increment(ref _frameCount);
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastFPSUpdate).TotalSeconds;
            
            if (elapsed >= 1.0) // 每秒更新一次FPS
            {
                lock (_lockObject)
                {
                    _currentFPS = _frameCount / elapsed;
                    _averageFPS = (_averageFPS * 0.9) + (_currentFPS * 0.1); // 指数移动平均
                    _minFPS = Math.Min(_minFPS, _currentFPS);
                    _maxFPS = Math.Max(_maxFPS, _currentFPS);
                    
                    _frameCount = 0;
                    _lastFPSUpdate = now;
                }
            }
        }

        /// <summary>
        /// 记录自定义性能指标
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        public void RecordCustomMetric(string metricName, double value)
        {
            _customMetrics.AddOrUpdate(metricName, value, (key, oldValue) => value);
        }

        /// <summary>
        /// 设置性能阈值
        /// </summary>
        /// <param name="thresholds">性能阈值配置</param>
        public void SetPerformanceThresholds(PerformanceThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        /// <summary>
        /// 启用自动降级
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void EnableAutoDegradation(bool enabled)
        {
            _autoDegradationEnabled = enabled;
        }

        /// <summary>
        /// 手动触发性能降级
        /// </summary>
        /// <param name="level">降级级别</param>
        public async Task TriggerDegradationAsync(DegradationLevel level)
        {
            if (level == _currentDegradationLevel)
                return;

            var oldLevel = _currentDegradationLevel;
            _currentDegradationLevel = level;
            _lastDegradationTime = DateTime.UtcNow;

            // 应用降级设置
            var appliedChanges = ApplyDegradationSettings(level);

            // 触发降级事件
            OnPerformanceDegradation(new PerformanceDegradationEventArgs
            {
                Level = level,
                Reason = "Manual degradation triggered",
                AppliedChanges = appliedChanges,
                ExpectedImprovement = GetExpectedImprovement(level)
            });

            await Task.CompletedTask;
        }

        /// <summary>
        /// 恢复性能设置
        /// </summary>
        public async Task RestorePerformanceAsync()
        {
            if (_currentDegradationLevel == DegradationLevel.None)
                return;

            _currentSettings = new PerformanceSettings
            {
                TargetFPS = _originalSettings.TargetFPS,
                RenderQuality = _originalSettings.RenderQuality,
                ShadowQuality = _originalSettings.ShadowQuality,
                TextureQuality = _originalSettings.TextureQuality,
                AntiAliasingLevel = _originalSettings.AntiAliasingLevel,
                ViewDistance = _originalSettings.ViewDistance,
                ParticleQuality = _originalSettings.ParticleQuality,
                VSync = _originalSettings.VSync,
                CustomSettings = new Dictionary<string, object>(_originalSettings.CustomSettings)
            };

            _currentDegradationLevel = DegradationLevel.None;

            // 触发恢复事件
            OnPerformanceDegradation(new PerformanceDegradationEventArgs
            {
                Level = DegradationLevel.None,
                Reason = "Performance restored to original settings",
                AppliedChanges = new Dictionary<string, object> { { "Restored", true } },
                ExpectedImprovement = "Performance restored to original levels"
            });

            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取建议的性能设置
        /// </summary>
        /// <returns>建议的性能设置</returns>
        public PerformanceSettings GetRecommendedSettings()
        {
            var metrics = GetCurrentMetrics();
            var level = DeterminePerformanceLevel(metrics);

            return level switch
            {
                PerformanceLevel.Critical => GetDegradedSettings(DegradationLevel.Extreme),
                PerformanceLevel.Poor => GetDegradedSettings(DegradationLevel.Severe),
                PerformanceLevel.Fair => GetDegradedSettings(DegradationLevel.Moderate),
                PerformanceLevel.Good => GetDegradedSettings(DegradationLevel.Minor),
                _ => _originalSettings
            };
        }

        /// <summary>
        /// 更新性能指标（定时器回调）
        /// </summary>
        private void UpdatePerformanceMetrics(object state)
        {
            if (!_isMonitoring)
                return;

            try
            {
                var metrics = GetCurrentMetrics();
                var level = DeterminePerformanceLevel(metrics);

                // 添加到历史记录
                var snapshot = new PerformanceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Metrics = metrics,
                    Level = level
                };

                _performanceHistory.Enqueue(snapshot);

                // 保持历史记录在合理范围内（最多保留1小时的数据）
                while (_performanceHistory.Count > 3600)
                {
                    _performanceHistory.TryDequeue(out _);
                }

                // 检查性能警告
                CheckPerformanceWarnings(metrics);

                // 自动降级检查
                if (_autoDegradationEnabled)
                {
                    CheckAutoDegradation(metrics, level);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断监控
                Console.WriteLine($"Performance monitoring error: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        private double GetCPUUsage()
        {
            try
            {
                return _cpuCounter?.NextValue() ?? 0.0;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        /// <summary>
        /// 计算GC压力
        /// </summary>
        private double CalculateGCPressure()
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            
            // 简化的GC压力计算
            return (gen0 * 0.1 + gen1 * 0.3 + gen2 * 0.6) / 1000.0;
        }

        /// <summary>
        /// 确定性能等级
        /// </summary>
        private PerformanceLevel DeterminePerformanceLevel(PerformanceMetrics metrics)
        {
            if (metrics.CurrentFPS < _thresholds.MinAcceptableFPS * 0.5)
                return PerformanceLevel.Critical;
            
            if (metrics.CurrentFPS < _thresholds.MinAcceptableFPS)
                return PerformanceLevel.Poor;
            
            if (metrics.CurrentFPS < _thresholds.WarningFPS)
                return PerformanceLevel.Fair;
            
            if (metrics.CPUUsage > _thresholds.MaxCPUUsage || 
                metrics.MemoryUsageMB > _thresholds.MaxMemoryUsageMB)
                return PerformanceLevel.Good;
            
            return PerformanceLevel.Excellent;
        }

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private void CheckPerformanceWarnings(PerformanceMetrics metrics)
        {
            // 检查低帧率
            if (metrics.CurrentFPS < _thresholds.WarningFPS)
            {
                OnPerformanceWarning(new PerformanceWarningEventArgs
                {
                    WarningType = PerformanceWarningType.LowFrameRate,
                    CurrentValue = metrics.CurrentFPS,
                    Threshold = _thresholds.WarningFPS,
                    Message = $"Frame rate is below warning threshold: {metrics.CurrentFPS:F1} FPS",
                    SuggestedActions = new List<string>
                    {
                        "Reduce render quality",
                        "Disable expensive effects",
                        "Lower resolution"
                    }
                });
            }

            // 检查高CPU使用率
            if (metrics.CPUUsage > _thresholds.MaxCPUUsage)
            {
                OnPerformanceWarning(new PerformanceWarningEventArgs
                {
                    WarningType = PerformanceWarningType.HighCPUUsage,
                    CurrentValue = metrics.CPUUsage,
                    Threshold = _thresholds.MaxCPUUsage,
                    Message = $"CPU usage is high: {metrics.CPUUsage:F1}%",
                    SuggestedActions = new List<string>
                    {
                        "Reduce AI complexity",
                        "Optimize update loops",
                        "Enable multi-threading"
                    }
                });
            }

            // 检查高内存使用率
            if (metrics.MemoryUsageMB > _thresholds.MaxMemoryUsageMB)
            {
                OnPerformanceWarning(new PerformanceWarningEventArgs
                {
                    WarningType = PerformanceWarningType.HighMemoryUsage,
                    CurrentValue = metrics.MemoryUsageMB,
                    Threshold = _thresholds.MaxMemoryUsageMB,
                    Message = $"Memory usage is high: {metrics.MemoryUsageMB:F1} MB",
                    SuggestedActions = new List<string>
                    {
                        "Clean up unused resources",
                        "Force garbage collection",
                        "Reduce texture quality"
                    }
                });
            }
        }

        /// <summary>
        /// 检查自动降级
        /// </summary>
        private void CheckAutoDegradation(PerformanceMetrics metrics, PerformanceLevel level)
        {
            // 避免频繁降级
            if ((DateTime.UtcNow - _lastDegradationTime).TotalSeconds < 30)
                return;

            var targetLevel = level switch
            {
                PerformanceLevel.Critical => DegradationLevel.Extreme,
                PerformanceLevel.Poor => DegradationLevel.Severe,
                PerformanceLevel.Fair => DegradationLevel.Moderate,
                PerformanceLevel.Good when _currentDegradationLevel == DegradationLevel.None => DegradationLevel.Minor,
                _ => DegradationLevel.None
            };

            if (targetLevel != _currentDegradationLevel && targetLevel != DegradationLevel.None)
            {
                _ = Task.Run(() => TriggerDegradationAsync(targetLevel));
            }
            else if (level == PerformanceLevel.Excellent && _currentDegradationLevel != DegradationLevel.None)
            {
                _ = Task.Run(() => RestorePerformanceAsync());
            }
        }

        /// <summary>
        /// 应用降级设置
        /// </summary>
        private Dictionary<string, object> ApplyDegradationSettings(DegradationLevel level)
        {
            var changes = new Dictionary<string, object>();
            var newSettings = GetDegradedSettings(level);

            if (newSettings.TargetFPS != _currentSettings.TargetFPS)
            {
                changes["TargetFPS"] = $"{_currentSettings.TargetFPS} -> {newSettings.TargetFPS}";
                _currentSettings.TargetFPS = newSettings.TargetFPS;
            }

            if (newSettings.RenderQuality != _currentSettings.RenderQuality)
            {
                changes["RenderQuality"] = $"{_currentSettings.RenderQuality} -> {newSettings.RenderQuality}";
                _currentSettings.RenderQuality = newSettings.RenderQuality;
            }

            if (newSettings.ShadowQuality != _currentSettings.ShadowQuality)
            {
                changes["ShadowQuality"] = $"{_currentSettings.ShadowQuality} -> {newSettings.ShadowQuality}";
                _currentSettings.ShadowQuality = newSettings.ShadowQuality;
            }

            return changes;
        }

        /// <summary>
        /// 获取降级设置
        /// </summary>
        private PerformanceSettings GetDegradedSettings(DegradationLevel level)
        {
            return level switch
            {
                DegradationLevel.Minor => new PerformanceSettings
                {
                    TargetFPS = 45,
                    RenderQuality = QualityLevel.Medium,
                    ShadowQuality = QualityLevel.Medium,
                    TextureQuality = QualityLevel.High,
                    AntiAliasingLevel = 2,
                    ViewDistance = 800.0f,
                    ParticleQuality = QualityLevel.Medium,
                    VSync = true
                },
                DegradationLevel.Moderate => new PerformanceSettings
                {
                    TargetFPS = 30,
                    RenderQuality = QualityLevel.Medium,
                    ShadowQuality = QualityLevel.Low,
                    TextureQuality = QualityLevel.Medium,
                    AntiAliasingLevel = 0,
                    ViewDistance = 600.0f,
                    ParticleQuality = QualityLevel.Low,
                    VSync = false
                },
                DegradationLevel.Severe => new PerformanceSettings
                {
                    TargetFPS = 30,
                    RenderQuality = QualityLevel.Low,
                    ShadowQuality = QualityLevel.Low,
                    TextureQuality = QualityLevel.Low,
                    AntiAliasingLevel = 0,
                    ViewDistance = 400.0f,
                    ParticleQuality = QualityLevel.Low,
                    VSync = false
                },
                DegradationLevel.Extreme => new PerformanceSettings
                {
                    TargetFPS = 20,
                    RenderQuality = QualityLevel.Low,
                    ShadowQuality = QualityLevel.Low,
                    TextureQuality = QualityLevel.Low,
                    AntiAliasingLevel = 0,
                    ViewDistance = 200.0f,
                    ParticleQuality = QualityLevel.Low,
                    VSync = false
                },
                _ => _originalSettings
            };
        }

        /// <summary>
        /// 获取预期改善描述
        /// </summary>
        private string GetExpectedImprovement(DegradationLevel level)
        {
            return level switch
            {
                DegradationLevel.Minor => "Slight performance improvement with minimal visual impact",
                DegradationLevel.Moderate => "Moderate performance improvement with noticeable visual changes",
                DegradationLevel.Severe => "Significant performance improvement with reduced visual quality",
                DegradationLevel.Extreme => "Maximum performance improvement with minimal visual features",
                _ => "No performance changes"
            };
        }

        /// <summary>
        /// 触发性能警告事件
        /// </summary>
        private void OnPerformanceWarning(PerformanceWarningEventArgs args)
        {
            PerformanceWarning?.Invoke(this, args);
        }

        /// <summary>
        /// 触发性能降级事件
        /// </summary>
        private void OnPerformanceDegradation(PerformanceDegradationEventArgs args)
        {
            PerformanceDegradation?.Invoke(this, args);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _monitoringTimer?.Dispose();
            _cpuCounter?.Dispose();
        }
    }
}