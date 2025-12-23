using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Performance
{
    /// <summary>
    /// 性能监控器接口
    /// 负责监控游戏性能指标并提供自动降级机制
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// 性能警告事件
        /// </summary>
        event EventHandler<PerformanceWarningEventArgs> PerformanceWarning;

        /// <summary>
        /// 性能降级事件
        /// </summary>
        event EventHandler<PerformanceDegradationEventArgs> PerformanceDegradation;

        /// <summary>
        /// 开始监控
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// 停止监控
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// 获取当前性能指标
        /// </summary>
        /// <returns>性能指标</returns>
        PerformanceMetrics GetCurrentMetrics();

        /// <summary>
        /// 获取性能历史数据
        /// </summary>
        /// <param name="duration">历史数据时长</param>
        /// <returns>性能历史数据</returns>
        IEnumerable<PerformanceSnapshot> GetPerformanceHistory(TimeSpan duration);

        /// <summary>
        /// 记录帧时间
        /// </summary>
        /// <param name="frameTime">帧时间（毫秒）</param>
        void RecordFrameTime(double frameTime);

        /// <summary>
        /// 记录自定义性能指标
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        void RecordCustomMetric(string metricName, double value);

        /// <summary>
        /// 设置性能阈值
        /// </summary>
        /// <param name="thresholds">性能阈值配置</param>
        void SetPerformanceThresholds(PerformanceThresholds thresholds);

        /// <summary>
        /// 启用自动降级
        /// </summary>
        /// <param name="enabled">是否启用</param>
        void EnableAutoDegradation(bool enabled);

        /// <summary>
        /// 手动触发性能降级
        /// </summary>
        /// <param name="level">降级级别</param>
        Task TriggerDegradationAsync(DegradationLevel level);

        /// <summary>
        /// 恢复性能设置
        /// </summary>
        Task RestorePerformanceAsync();

        /// <summary>
        /// 获取建议的性能设置
        /// </summary>
        /// <returns>建议的性能设置</returns>
        PerformanceSettings GetRecommendedSettings();
    }

    /// <summary>
    /// 性能指标
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// 当前帧率
        /// </summary>
        public double CurrentFPS { get; set; }

        /// <summary>
        /// 平均帧率
        /// </summary>
        public double AverageFPS { get; set; }

        /// <summary>
        /// 最小帧率
        /// </summary>
        public double MinFPS { get; set; }

        /// <summary>
        /// 最大帧率
        /// </summary>
        public double MaxFPS { get; set; }

        /// <summary>
        /// 帧时间（毫秒）
        /// </summary>
        public double FrameTime { get; set; }

        /// <summary>
        /// CPU使用率（百分比）
        /// </summary>
        public double CPUUsage { get; set; }

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public double MemoryUsageMB { get; set; }

        /// <summary>
        /// GC压力指标
        /// </summary>
        public double GCPressure { get; set; }

        /// <summary>
        /// 渲染时间（毫秒）
        /// </summary>
        public double RenderTime { get; set; }

        /// <summary>
        /// 更新时间（毫秒）
        /// </summary>
        public double UpdateTime { get; set; }

        /// <summary>
        /// 自定义指标
        /// </summary>
        public Dictionary<string, double> CustomMetrics { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// 测量时间
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 性能快照
    /// </summary>
    public class PerformanceSnapshot
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 性能指标
        /// </summary>
        public PerformanceMetrics Metrics { get; set; }

        /// <summary>
        /// 性能等级
        /// </summary>
        public PerformanceLevel Level { get; set; }
    }

    /// <summary>
    /// 性能阈值配置
    /// </summary>
    public class PerformanceThresholds
    {
        /// <summary>
        /// 最低可接受帧率
        /// </summary>
        public double MinAcceptableFPS { get; set; } = 30.0;

        /// <summary>
        /// 警告帧率阈值
        /// </summary>
        public double WarningFPS { get; set; } = 45.0;

        /// <summary>
        /// 最大CPU使用率
        /// </summary>
        public double MaxCPUUsage { get; set; } = 80.0;

        /// <summary>
        /// 最大内存使用量（MB）
        /// </summary>
        public double MaxMemoryUsageMB { get; set; } = 1024.0;

        /// <summary>
        /// 最大GC压力
        /// </summary>
        public double MaxGCPressure { get; set; } = 0.1;

        /// <summary>
        /// 最大帧时间（毫秒）
        /// </summary>
        public double MaxFrameTime { get; set; } = 33.33; // ~30 FPS
    }

    /// <summary>
    /// 性能设置
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// 目标帧率
        /// </summary>
        public int TargetFPS { get; set; } = 60;

        /// <summary>
        /// 渲染质量级别
        /// </summary>
        public QualityLevel RenderQuality { get; set; } = QualityLevel.High;

        /// <summary>
        /// 阴影质量
        /// </summary>
        public QualityLevel ShadowQuality { get; set; } = QualityLevel.High;

        /// <summary>
        /// 纹理质量
        /// </summary>
        public QualityLevel TextureQuality { get; set; } = QualityLevel.High;

        /// <summary>
        /// 抗锯齿级别
        /// </summary>
        public int AntiAliasingLevel { get; set; } = 4;

        /// <summary>
        /// 视距
        /// </summary>
        public float ViewDistance { get; set; } = 1000.0f;

        /// <summary>
        /// 粒子效果质量
        /// </summary>
        public QualityLevel ParticleQuality { get; set; } = QualityLevel.High;

        /// <summary>
        /// 是否启用垂直同步
        /// </summary>
        public bool VSync { get; set; } = true;

        /// <summary>
        /// 自定义设置
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 性能警告事件参数
    /// </summary>
    public class PerformanceWarningEventArgs : EventArgs
    {
        /// <summary>
        /// 警告类型
        /// </summary>
        public PerformanceWarningType WarningType { get; set; }

        /// <summary>
        /// 当前指标值
        /// </summary>
        public double CurrentValue { get; set; }

        /// <summary>
        /// 阈值
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 建议的操作
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 性能降级事件参数
    /// </summary>
    public class PerformanceDegradationEventArgs : EventArgs
    {
        /// <summary>
        /// 降级级别
        /// </summary>
        public DegradationLevel Level { get; set; }

        /// <summary>
        /// 降级原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 应用的设置变更
        /// </summary>
        public Dictionary<string, object> AppliedChanges { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 预期的性能改善
        /// </summary>
        public string ExpectedImprovement { get; set; }
    }

    /// <summary>
    /// 性能等级
    /// </summary>
    public enum PerformanceLevel
    {
        /// <summary>
        /// 优秀
        /// </summary>
        Excellent,

        /// <summary>
        /// 良好
        /// </summary>
        Good,

        /// <summary>
        /// 一般
        /// </summary>
        Fair,

        /// <summary>
        /// 较差
        /// </summary>
        Poor,

        /// <summary>
        /// 很差
        /// </summary>
        Critical
    }

    /// <summary>
    /// 质量级别
    /// </summary>
    public enum QualityLevel
    {
        /// <summary>
        /// 低
        /// </summary>
        Low,

        /// <summary>
        /// 中
        /// </summary>
        Medium,

        /// <summary>
        /// 高
        /// </summary>
        High,

        /// <summary>
        /// 超高
        /// </summary>
        Ultra
    }

    /// <summary>
    /// 降级级别
    /// </summary>
    public enum DegradationLevel
    {
        /// <summary>
        /// 无降级
        /// </summary>
        None,

        /// <summary>
        /// 轻微降级
        /// </summary>
        Minor,

        /// <summary>
        /// 中等降级
        /// </summary>
        Moderate,

        /// <summary>
        /// 严重降级
        /// </summary>
        Severe,

        /// <summary>
        /// 极端降级
        /// </summary>
        Extreme
    }

    /// <summary>
    /// 性能警告类型
    /// </summary>
    public enum PerformanceWarningType
    {
        /// <summary>
        /// 低帧率
        /// </summary>
        LowFrameRate,

        /// <summary>
        /// 高CPU使用率
        /// </summary>
        HighCPUUsage,

        /// <summary>
        /// 高内存使用率
        /// </summary>
        HighMemoryUsage,

        /// <summary>
        /// 高GC压力
        /// </summary>
        HighGCPressure,

        /// <summary>
        /// 长帧时间
        /// </summary>
        LongFrameTime,

        /// <summary>
        /// 自定义警告
        /// </summary>
        Custom
    }
}