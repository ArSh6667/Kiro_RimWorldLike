using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Build
{
    /// <summary>
    /// 构建系统接口
    /// 负责自动化构建和打包流程，依赖项收集和验证
    /// </summary>
    public interface IBuildSystem
    {
        /// <summary>
        /// 构建进度事件
        /// </summary>
        event EventHandler<BuildProgressEventArgs> BuildProgress;

        /// <summary>
        /// 构建完成事件
        /// </summary>
        event EventHandler<BuildCompletedEventArgs> BuildCompleted;

        /// <summary>
        /// 构建错误事件
        /// </summary>
        event EventHandler<BuildErrorEventArgs> BuildError;

        /// <summary>
        /// 创建构建配置
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <param name="configuration">构建配置</param>
        /// <returns>构建配置实例</returns>
        BuildConfiguration CreateBuildConfiguration(string projectPath, BuildSettings configuration);

        /// <summary>
        /// 验证构建配置
        /// </summary>
        /// <param name="configuration">构建配置</param>
        /// <returns>验证结果</returns>
        Task<BuildValidationResult> ValidateBuildConfigurationAsync(BuildConfiguration configuration);

        /// <summary>
        /// 收集项目依赖项
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>依赖项列表</returns>
        Task<IEnumerable<BuildDependency>> CollectDependenciesAsync(string projectPath);

        /// <summary>
        /// 验证依赖项
        /// </summary>
        /// <param name="dependencies">依赖项列表</param>
        /// <returns>验证结果</returns>
        Task<DependencyValidationResult> ValidateDependenciesAsync(IEnumerable<BuildDependency> dependencies);

        /// <summary>
        /// 执行构建
        /// </summary>
        /// <param name="configuration">构建配置</param>
        /// <returns>构建结果</returns>
        Task<BuildResult> BuildAsync(BuildConfiguration configuration);

        /// <summary>
        /// 清理构建输出
        /// </summary>
        /// <param name="configuration">构建配置</param>
        /// <returns>清理结果</returns>
        Task<CleanResult> CleanAsync(BuildConfiguration configuration);

        /// <summary>
        /// 打包构建输出
        /// </summary>
        /// <param name="configuration">构建配置</param>
        /// <param name="packageSettings">打包设置</param>
        /// <returns>打包结果</returns>
        Task<PackageResult> PackageAsync(BuildConfiguration configuration, PackageSettings packageSettings);

        /// <summary>
        /// 获取构建历史
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>构建历史记录</returns>
        IEnumerable<BuildRecord> GetBuildHistory(string projectPath);

        /// <summary>
        /// 获取构建统计信息
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>构建统计</returns>
        BuildStatistics GetBuildStatistics(string projectPath);
    }

    /// <summary>
    /// 构建配置
    /// </summary>
    public class BuildConfiguration
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 项目路径
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// 构建模式
        /// </summary>
        public BuildMode Mode { get; set; } = BuildMode.Release;

        /// <summary>
        /// 目标平台
        /// </summary>
        public TargetPlatform Platform { get; set; } = TargetPlatform.Windows;

        /// <summary>
        /// 目标架构
        /// </summary>
        public TargetArchitecture Architecture { get; set; } = TargetArchitecture.x64;

        /// <summary>
        /// 构建设置
        /// </summary>
        public BuildSettings Settings { get; set; } = new BuildSettings();

        /// <summary>
        /// 包含的文件模式
        /// </summary>
        public List<string> IncludePatterns { get; set; } = new List<string>();

        /// <summary>
        /// 排除的文件模式
        /// </summary>
        public List<string> ExcludePatterns { get; set; } = new List<string>();

        /// <summary>
        /// 预构建脚本
        /// </summary>
        public List<BuildScript> PreBuildScripts { get; set; } = new List<BuildScript>();

        /// <summary>
        /// 后构建脚本
        /// </summary>
        public List<BuildScript> PostBuildScripts { get; set; } = new List<BuildScript>();

        /// <summary>
        /// 环境变量
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 构建设置
    /// </summary>
    public class BuildSettings
    {
        /// <summary>
        /// 是否启用优化
        /// </summary>
        public bool EnableOptimization { get; set; } = true;

        /// <summary>
        /// 是否生成调试信息
        /// </summary>
        public bool GenerateDebugInfo { get; set; } = false;

        /// <summary>
        /// 是否启用并行构建
        /// </summary>
        public bool EnableParallelBuild { get; set; } = true;

        /// <summary>
        /// 最大并行度
        /// </summary>
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// 构建超时时间（分钟）
        /// </summary>
        public int TimeoutMinutes { get; set; } = 30;

        /// <summary>
        /// 是否启用增量构建
        /// </summary>
        public bool EnableIncrementalBuild { get; set; } = true;

        /// <summary>
        /// 是否清理输出目录
        /// </summary>
        public bool CleanOutputDirectory { get; set; } = true;

        /// <summary>
        /// 编译器选项
        /// </summary>
        public Dictionary<string, string> CompilerOptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 链接器选项
        /// </summary>
        public Dictionary<string, string> LinkerOptions { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 构建依赖项
    /// </summary>
    public class BuildDependency
    {
        /// <summary>
        /// 依赖项名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 依赖项类型
        /// </summary>
        public DependencyType Type { get; set; }

        /// <summary>
        /// 源路径
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// 依赖项描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件哈希
        /// </summary>
        public string FileHash { get; set; }
    }

    /// <summary>
    /// 构建脚本
    /// </summary>
    public class BuildScript
    {
        /// <summary>
        /// 脚本名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 脚本路径
        /// </summary>
        public string ScriptPath { get; set; }

        /// <summary>
        /// 脚本参数
        /// </summary>
        public List<string> Arguments { get; set; } = new List<string>();

        /// <summary>
        /// 工作目录
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// 超时时间（分钟）
        /// </summary>
        public int TimeoutMinutes { get; set; } = 10;

        /// <summary>
        /// 是否在失败时继续
        /// </summary>
        public bool ContinueOnError { get; set; } = false;
    }

    /// <summary>
    /// 打包设置
    /// </summary>
    public class PackageSettings
    {
        /// <summary>
        /// 包名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 包版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 包格式
        /// </summary>
        public PackageFormat Format { get; set; } = PackageFormat.Zip;

        /// <summary>
        /// 压缩级别
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// 输出路径
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// 包含的文件
        /// </summary>
        public List<string> IncludeFiles { get; set; } = new List<string>();

        /// <summary>
        /// 排除的文件
        /// </summary>
        public List<string> ExcludeFiles { get; set; } = new List<string>();

        /// <summary>
        /// 包元数据
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 构建进度事件参数
    /// </summary>
    public class BuildProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 当前步骤
        /// </summary>
        public string CurrentStep { get; set; }

        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 已完成的任务数
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }
    }

    /// <summary>
    /// 构建完成事件参数
    /// </summary>
    public class BuildCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 构建结果
        /// </summary>
        public BuildResult Result { get; set; }

        /// <summary>
        /// 构建时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 输出文件列表
        /// </summary>
        public List<string> OutputFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// 构建错误事件参数
    /// </summary>
    public class BuildErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 错误发生的步骤
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public string ErrorCode { get; set; }
    }

    /// <summary>
    /// 构建模式
    /// </summary>
    public enum BuildMode
    {
        /// <summary>
        /// 调试模式
        /// </summary>
        Debug,

        /// <summary>
        /// 发布模式
        /// </summary>
        Release,

        /// <summary>
        /// 分析模式
        /// </summary>
        Profile
    }

    /// <summary>
    /// 目标平台
    /// </summary>
    public enum TargetPlatform
    {
        /// <summary>
        /// Windows
        /// </summary>
        Windows,

        /// <summary>
        /// Linux
        /// </summary>
        Linux,

        /// <summary>
        /// macOS
        /// </summary>
        MacOS,

        /// <summary>
        /// 跨平台
        /// </summary>
        CrossPlatform
    }

    /// <summary>
    /// 目标架构
    /// </summary>
    public enum TargetArchitecture
    {
        /// <summary>
        /// x86 (32位)
        /// </summary>
        x86,

        /// <summary>
        /// x64 (64位)
        /// </summary>
        x64,

        /// <summary>
        /// ARM
        /// </summary>
        ARM,

        /// <summary>
        /// ARM64
        /// </summary>
        ARM64,

        /// <summary>
        /// 任意CPU
        /// </summary>
        AnyCPU
    }

    /// <summary>
    /// 依赖项类型
    /// </summary>
    public enum DependencyType
    {
        /// <summary>
        /// 程序集
        /// </summary>
        Assembly,

        /// <summary>
        /// 本机库
        /// </summary>
        NativeLibrary,

        /// <summary>
        /// 资源文件
        /// </summary>
        Resource,

        /// <summary>
        /// 配置文件
        /// </summary>
        Configuration,

        /// <summary>
        /// 数据文件
        /// </summary>
        Data,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 包格式
    /// </summary>
    public enum PackageFormat
    {
        /// <summary>
        /// ZIP压缩包
        /// </summary>
        Zip,

        /// <summary>
        /// TAR压缩包
        /// </summary>
        Tar,

        /// <summary>
        /// 7-Zip压缩包
        /// </summary>
        SevenZip,

        /// <summary>
        /// MSI安装包
        /// </summary>
        MSI,

        /// <summary>
        /// NSIS安装包
        /// </summary>
        NSIS,

        /// <summary>
        /// 目录
        /// </summary>
        Directory
    }

    /// <summary>
    /// 压缩级别
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>
        /// 无压缩
        /// </summary>
        None,

        /// <summary>
        /// 最快
        /// </summary>
        Fastest,

        /// <summary>
        /// 最优
        /// </summary>
        Optimal,

        /// <summary>
        /// 最小
        /// </summary>
        SmallestSize
    }
}