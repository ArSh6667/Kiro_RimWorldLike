using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Build
{
    /// <summary>
    /// 构建结果
    /// </summary>
    public class BuildResult
    {
        /// <summary>
        /// 构建是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 构建ID
        /// </summary>
        public string BuildId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 构建开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 构建结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 构建时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<BuildArtifact> Artifacts { get; set; } = new List<BuildArtifact>();

        /// <summary>
        /// 构建日志
        /// </summary>
        public List<BuildLogEntry> Logs { get; set; } = new List<BuildLogEntry>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<BuildError> Errors { get; set; } = new List<BuildError>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<BuildWarning> Warnings { get; set; } = new List<BuildWarning>();

        /// <summary>
        /// 构建统计信息
        /// </summary>
        public BuildMetrics Metrics { get; set; } = new BuildMetrics();

        /// <summary>
        /// 构建配置
        /// </summary>
        public BuildConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// 构建验证结果
    /// </summary>
    public class BuildValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// 验证警告列表
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// 验证详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 依赖项验证结果
    /// </summary>
    public class DependencyValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 缺失的依赖项
        /// </summary>
        public List<BuildDependency> MissingDependencies { get; set; } = new List<BuildDependency>();

        /// <summary>
        /// 版本冲突的依赖项
        /// </summary>
        public List<DependencyConflict> VersionConflicts { get; set; } = new List<DependencyConflict>();

        /// <summary>
        /// 损坏的依赖项
        /// </summary>
        public List<BuildDependency> CorruptedDependencies { get; set; } = new List<BuildDependency>();

        /// <summary>
        /// 验证详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 清理结果
    /// </summary>
    public class CleanResult
    {
        /// <summary>
        /// 清理是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 删除的文件数量
        /// </summary>
        public int DeletedFilesCount { get; set; }

        /// <summary>
        /// 删除的目录数量
        /// </summary>
        public int DeletedDirectoriesCount { get; set; }

        /// <summary>
        /// 释放的磁盘空间（字节）
        /// </summary>
        public long FreedSpaceBytes { get; set; }

        /// <summary>
        /// 清理时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 清理错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 打包结果
    /// </summary>
    public class PackageResult
    {
        /// <summary>
        /// 打包是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 包文件路径
        /// </summary>
        public string PackageFilePath { get; set; }

        /// <summary>
        /// 包文件大小（字节）
        /// </summary>
        public long PackageSize { get; set; }

        /// <summary>
        /// 包含的文件数量
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// 压缩比率
        /// </summary>
        public double CompressionRatio { get; set; }

        /// <summary>
        /// 打包时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 包哈希值
        /// </summary>
        public string PackageHash { get; set; }

        /// <summary>
        /// 打包错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 构建产物
    /// </summary>
    public class BuildArtifact
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件哈希
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// 产物类型
        /// </summary>
        public ArtifactType Type { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 是否为主要产物
        /// </summary>
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// 构建日志条目
    /// </summary>
    public class BuildLogEntry
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 步骤
        /// </summary>
        public string Step { get; set; }
    }

    /// <summary>
    /// 构建错误
    /// </summary>
    public class BuildError
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 列号
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; }
    }

    /// <summary>
    /// 构建警告
    /// </summary>
    public class BuildWarning
    {
        /// <summary>
        /// 警告代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// 列号
        /// </summary>
        public int ColumnNumber { get; set; }
    }

    /// <summary>
    /// 构建指标
    /// </summary>
    public class BuildMetrics
    {
        /// <summary>
        /// 编译的文件数量
        /// </summary>
        public int CompiledFilesCount { get; set; }

        /// <summary>
        /// 生成的代码行数
        /// </summary>
        public int GeneratedLinesOfCode { get; set; }

        /// <summary>
        /// 使用的内存峰值（MB）
        /// </summary>
        public double PeakMemoryUsageMB { get; set; }

        /// <summary>
        /// CPU使用时间（秒）
        /// </summary>
        public double CpuTimeSeconds { get; set; }

        /// <summary>
        /// 磁盘I/O读取（MB）
        /// </summary>
        public double DiskReadMB { get; set; }

        /// <summary>
        /// 磁盘I/O写入（MB）
        /// </summary>
        public double DiskWriteMB { get; set; }

        /// <summary>
        /// 并行度
        /// </summary>
        public int ParallelismLevel { get; set; }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 相关属性
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// 警告代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 相关属性
        /// </summary>
        public string Property { get; set; }
    }

    /// <summary>
    /// 依赖项冲突
    /// </summary>
    public class DependencyConflict
    {
        /// <summary>
        /// 依赖项名称
        /// </summary>
        public string DependencyName { get; set; }

        /// <summary>
        /// 请求的版本
        /// </summary>
        public string RequestedVersion { get; set; }

        /// <summary>
        /// 已安装的版本
        /// </summary>
        public string InstalledVersion { get; set; }

        /// <summary>
        /// 冲突类型
        /// </summary>
        public ConflictType ConflictType { get; set; }

        /// <summary>
        /// 冲突描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 构建记录
    /// </summary>
    public class BuildRecord
    {
        /// <summary>
        /// 构建ID
        /// </summary>
        public string BuildId { get; set; }

        /// <summary>
        /// 构建时间
        /// </summary>
        public DateTime BuildTime { get; set; }

        /// <summary>
        /// 构建结果
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 构建时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 构建配置
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// 目标平台
        /// </summary>
        public TargetPlatform Platform { get; set; }

        /// <summary>
        /// 错误数量
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 警告数量
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// 输出大小（字节）
        /// </summary>
        public long OutputSize { get; set; }
    }

    /// <summary>
    /// 构建统计信息
    /// </summary>
    public class BuildStatistics
    {
        /// <summary>
        /// 总构建次数
        /// </summary>
        public int TotalBuilds { get; set; }

        /// <summary>
        /// 成功构建次数
        /// </summary>
        public int SuccessfulBuilds { get; set; }

        /// <summary>
        /// 失败构建次数
        /// </summary>
        public int FailedBuilds { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => TotalBuilds > 0 ? (double)SuccessfulBuilds / TotalBuilds * 100 : 0;

        /// <summary>
        /// 平均构建时长
        /// </summary>
        public TimeSpan AverageBuildDuration { get; set; }

        /// <summary>
        /// 最快构建时长
        /// </summary>
        public TimeSpan FastestBuildDuration { get; set; }

        /// <summary>
        /// 最慢构建时长
        /// </summary>
        public TimeSpan SlowestBuildDuration { get; set; }

        /// <summary>
        /// 最后构建时间
        /// </summary>
        public DateTime LastBuildTime { get; set; }

        /// <summary>
        /// 按平台分组的统计
        /// </summary>
        public Dictionary<TargetPlatform, PlatformStatistics> PlatformStats { get; set; } = new Dictionary<TargetPlatform, PlatformStatistics>();
    }

    /// <summary>
    /// 平台统计信息
    /// </summary>
    public class PlatformStatistics
    {
        /// <summary>
        /// 构建次数
        /// </summary>
        public int BuildCount { get; set; }

        /// <summary>
        /// 成功次数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 平均构建时长
        /// </summary>
        public TimeSpan AverageDuration { get; set; }
    }

    /// <summary>
    /// 产物类型
    /// </summary>
    public enum ArtifactType
    {
        /// <summary>
        /// 可执行文件
        /// </summary>
        Executable,

        /// <summary>
        /// 库文件
        /// </summary>
        Library,

        /// <summary>
        /// 资源文件
        /// </summary>
        Resource,

        /// <summary>
        /// 配置文件
        /// </summary>
        Configuration,

        /// <summary>
        /// 文档
        /// </summary>
        Documentation,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 调试
        /// </summary>
        Debug,

        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal
    }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error,

        /// <summary>
        /// 致命错误
        /// </summary>
        Fatal
    }

    /// <summary>
    /// 冲突类型
    /// </summary>
    public enum ConflictType
    {
        /// <summary>
        /// 版本不兼容
        /// </summary>
        VersionIncompatible,

        /// <summary>
        /// 版本过低
        /// </summary>
        VersionTooOld,

        /// <summary>
        /// 版本过高
        /// </summary>
        VersionTooNew,

        /// <summary>
        /// 缺失依赖
        /// </summary>
        MissingDependency,

        /// <summary>
        /// 循环依赖
        /// </summary>
        CircularDependency
    }
}