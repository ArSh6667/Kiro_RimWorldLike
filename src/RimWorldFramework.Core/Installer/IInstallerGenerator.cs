using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Installer
{
    /// <summary>
    /// 安装程序生成器接口
    /// 负责创建跨平台安装程序和实现安装卸载逻辑
    /// </summary>
    public interface IInstallerGenerator
    {
        /// <summary>
        /// 安装进度事件
        /// </summary>
        event EventHandler<InstallProgressEventArgs> InstallProgress;

        /// <summary>
        /// 安装完成事件
        /// </summary>
        event EventHandler<InstallCompletedEventArgs> InstallCompleted;

        /// <summary>
        /// 安装错误事件
        /// </summary>
        event EventHandler<InstallErrorEventArgs> InstallError;

        /// <summary>
        /// 创建安装程序配置
        /// </summary>
        /// <param name="packagePath">包文件路径</param>
        /// <param name="settings">安装设置</param>
        /// <returns>安装程序配置</returns>
        InstallerConfiguration CreateInstallerConfiguration(string packagePath, InstallerSettings settings);

        /// <summary>
        /// 验证安装程序配置
        /// </summary>
        /// <param name="configuration">安装程序配置</param>
        /// <returns>验证结果</returns>
        Task<InstallerValidationResult> ValidateConfigurationAsync(InstallerConfiguration configuration);

        /// <summary>
        /// 生成安装程序
        /// </summary>
        /// <param name="configuration">安装程序配置</param>
        /// <returns>生成结果</returns>
        Task<InstallerGenerationResult> GenerateInstallerAsync(InstallerConfiguration configuration);

        /// <summary>
        /// 执行安装
        /// </summary>
        /// <param name="installerPath">安装程序路径</param>
        /// <param name="installOptions">安装选项</param>
        /// <returns>安装结果</returns>
        Task<InstallationResult> InstallAsync(string installerPath, InstallationOptions installOptions);

        /// <summary>
        /// 执行卸载
        /// </summary>
        /// <param name="applicationId">应用程序ID</param>
        /// <param name="uninstallOptions">卸载选项</param>
        /// <returns>卸载结果</returns>
        Task<UninstallationResult> UninstallAsync(string applicationId, UninstallationOptions uninstallOptions);

        /// <summary>
        /// 检查安装状态
        /// </summary>
        /// <param name="applicationId">应用程序ID</param>
        /// <returns>安装状态</returns>
        Task<InstallationStatus> CheckInstallationStatusAsync(string applicationId);

        /// <summary>
        /// 获取已安装的应用程序列表
        /// </summary>
        /// <returns>已安装应用程序列表</returns>
        Task<IEnumerable<InstalledApplication>> GetInstalledApplicationsAsync();

        /// <summary>
        /// 修复安装
        /// </summary>
        /// <param name="applicationId">应用程序ID</param>
        /// <returns>修复结果</returns>
        Task<RepairResult> RepairInstallationAsync(string applicationId);

        /// <summary>
        /// 更新应用程序
        /// </summary>
        /// <param name="applicationId">应用程序ID</param>
        /// <param name="updatePackagePath">更新包路径</param>
        /// <returns>更新结果</returns>
        Task<UpdateResult> UpdateApplicationAsync(string applicationId, string updatePackagePath);
    }

    /// <summary>
    /// 安装程序配置
    /// </summary>
    public class InstallerConfiguration
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 应用程序信息
        /// </summary>
        public ApplicationInfo Application { get; set; } = new ApplicationInfo();

        /// <summary>
        /// 包文件路径
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// 安装程序类型
        /// </summary>
        public InstallerType Type { get; set; } = InstallerType.MSI;

        /// <summary>
        /// 目标平台
        /// </summary>
        public List<InstallerPlatform> TargetPlatforms { get; set; } = new List<InstallerPlatform>();

        /// <summary>
        /// 安装设置
        /// </summary>
        public InstallerSettings Settings { get; set; } = new InstallerSettings();

        /// <summary>
        /// 安装步骤
        /// </summary>
        public List<InstallationStep> InstallationSteps { get; set; } = new List<InstallationStep>();

        /// <summary>
        /// 卸载步骤
        /// </summary>
        public List<UninstallationStep> UninstallationSteps { get; set; } = new List<UninstallationStep>();

        /// <summary>
        /// 系统要求
        /// </summary>
        public SystemRequirements Requirements { get; set; } = new SystemRequirements();

        /// <summary>
        /// 用户界面配置
        /// </summary>
        public InstallerUI UIConfiguration { get; set; } = new InstallerUI();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 应用程序信息
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// 应用程序ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 应用程序名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 发布者
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 主页URL
        /// </summary>
        public string HomepageUrl { get; set; }

        /// <summary>
        /// 支持URL
        /// </summary>
        public string SupportUrl { get; set; }

        /// <summary>
        /// 图标路径
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 许可证
        /// </summary>
        public string License { get; set; }

        /// <summary>
        /// 安装大小（字节）
        /// </summary>
        public long InstallSize { get; set; }
    }

    /// <summary>
    /// 安装设置
    /// </summary>
    public class InstallerSettings
    {
        /// <summary>
        /// 默认安装目录
        /// </summary>
        public string DefaultInstallDirectory { get; set; }

        /// <summary>
        /// 是否允许用户选择安装目录
        /// </summary>
        public bool AllowCustomInstallDirectory { get; set; } = true;

        /// <summary>
        /// 是否创建桌面快捷方式
        /// </summary>
        public bool CreateDesktopShortcut { get; set; } = true;

        /// <summary>
        /// 是否创建开始菜单快捷方式
        /// </summary>
        public bool CreateStartMenuShortcut { get; set; } = true;

        /// <summary>
        /// 是否添加到系统PATH
        /// </summary>
        public bool AddToSystemPath { get; set; } = false;

        /// <summary>
        /// 是否注册文件关联
        /// </summary>
        public bool RegisterFileAssociations { get; set; } = false;

        /// <summary>
        /// 文件关联列表
        /// </summary>
        public List<FileAssociation> FileAssociations { get; set; } = new List<FileAssociation>();

        /// <summary>
        /// 是否需要管理员权限
        /// </summary>
        public bool RequireAdminRights { get; set; } = false;

        /// <summary>
        /// 是否支持静默安装
        /// </summary>
        public bool SupportSilentInstall { get; set; } = true;

        /// <summary>
        /// 安装前脚本
        /// </summary>
        public List<InstallScript> PreInstallScripts { get; set; } = new List<InstallScript>();

        /// <summary>
        /// 安装后脚本
        /// </summary>
        public List<InstallScript> PostInstallScripts { get; set; } = new List<InstallScript>();
    }

    /// <summary>
    /// 安装程序进度事件参数
    /// </summary>
    public class InstallProgressEventArgs : EventArgs
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
        /// 已完成的步骤数
        /// </summary>
        public int CompletedSteps { get; set; }

        /// <summary>
        /// 总步骤数
        /// </summary>
        public int TotalSteps { get; set; }
    }

    /// <summary>
    /// 安装完成事件参数
    /// </summary>
    public class InstallCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 安装结果
        /// </summary>
        public InstallationResult Result { get; set; }

        /// <summary>
        /// 安装时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 安装的文件列表
        /// </summary>
        public List<string> InstalledFiles { get; set; } = new List<string>();
    }

    /// <summary>
    /// 安装错误事件参数
    /// </summary>
    public class InstallErrorEventArgs : EventArgs
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
    /// 安装程序类型
    /// </summary>
    public enum InstallerType
    {
        /// <summary>
        /// MSI安装包
        /// </summary>
        MSI,

        /// <summary>
        /// NSIS安装包
        /// </summary>
        NSIS,

        /// <summary>
        /// Inno Setup安装包
        /// </summary>
        InnoSetup,

        /// <summary>
        /// 自解压可执行文件
        /// </summary>
        SelfExtracting,

        /// <summary>
        /// 跨平台安装包
        /// </summary>
        CrossPlatform
    }

    /// <summary>
    /// 安装程序平台
    /// </summary>
    public enum InstallerPlatform
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
        MacOS
    }
}