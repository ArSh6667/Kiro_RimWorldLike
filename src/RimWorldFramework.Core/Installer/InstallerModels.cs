using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Installer
{
    /// <summary>
    /// 安装步骤
    /// </summary>
    public class InstallationStep
    {
        /// <summary>
        /// 步骤ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 步骤类型
        /// </summary>
        public StepType Type { get; set; }

        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// 步骤参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 条件表达式
        /// </summary>
        public string Condition { get; set; }
    }

    /// <summary>
    /// 卸载步骤
    /// </summary>
    public class UninstallationStep
    {
        /// <summary>
        /// 步骤ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 步骤描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 步骤类型
        /// </summary>
        public StepType Type { get; set; }

        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 步骤参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 系统要求
    /// </summary>
    public class SystemRequirements
    {
        /// <summary>
        /// 最低操作系统版本
        /// </summary>
        public string MinimumOSVersion { get; set; }

        /// <summary>
        /// 最低内存要求（MB）
        /// </summary>
        public int MinimumMemoryMB { get; set; } = 512;

        /// <summary>
        /// 最低磁盘空间要求（MB）
        /// </summary>
        public int MinimumDiskSpaceMB { get; set; } = 100;

        /// <summary>
        /// 必需的软件依赖
        /// </summary>
        public List<SoftwareDependency> RequiredSoftware { get; set; } = new List<SoftwareDependency>();

        /// <summary>
        /// 支持的处理器架构
        /// </summary>
        public List<string> SupportedArchitectures { get; set; } = new List<string> { "x64" };

        /// <summary>
        /// 是否需要网络连接
        /// </summary>
        public bool RequiresInternetConnection { get; set; } = false;
    }

    /// <summary>
    /// 安装程序UI配置
    /// </summary>
    public class InstallerUI
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string Theme { get; set; } = "Default";

        /// <summary>
        /// 语言
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// 支持的语言列表
        /// </summary>
        public List<string> SupportedLanguages { get; set; } = new List<string> { "en-US", "zh-CN" };

        /// <summary>
        /// 欢迎页面配置
        /// </summary>
        public WelcomePage WelcomePage { get; set; } = new WelcomePage();

        /// <summary>
        /// 许可协议页面配置
        /// </summary>
        public LicensePage LicensePage { get; set; } = new LicensePage();

        /// <summary>
        /// 安装目录选择页面配置
        /// </summary>
        public DirectoryPage DirectoryPage { get; set; } = new DirectoryPage();

        /// <summary>
        /// 组件选择页面配置
        /// </summary>
        public ComponentsPage ComponentsPage { get; set; } = new ComponentsPage();

        /// <summary>
        /// 进度页面配置
        /// </summary>
        public ProgressPage ProgressPage { get; set; } = new ProgressPage();

        /// <summary>
        /// 完成页面配置
        /// </summary>
        public FinishPage FinishPage { get; set; } = new FinishPage();
    }

    /// <summary>
    /// 文件关联
    /// </summary>
    public class FileAssociation
    {
        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// 文件类型描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 图标路径
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 打开命令
        /// </summary>
        public string OpenCommand { get; set; }
    }

    /// <summary>
    /// 安装脚本
    /// </summary>
    public class InstallScript
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
        /// 执行条件
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// 是否在失败时继续
        /// </summary>
        public bool ContinueOnError { get; set; } = false;
    }

    /// <summary>
    /// 软件依赖
    /// </summary>
    public class SoftwareDependency
    {
        /// <summary>
        /// 软件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 最低版本
        /// </summary>
        public string MinimumVersion { get; set; }

        /// <summary>
        /// 下载URL
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// 检测方法
        /// </summary>
        public string DetectionMethod { get; set; }
    }

    /// <summary>
    /// 安装选项
    /// </summary>
    public class InstallationOptions
    {
        /// <summary>
        /// 安装目录
        /// </summary>
        public string InstallDirectory { get; set; }

        /// <summary>
        /// 是否静默安装
        /// </summary>
        public bool SilentInstall { get; set; } = false;

        /// <summary>
        /// 选择的组件
        /// </summary>
        public List<string> SelectedComponents { get; set; } = new List<string>();

        /// <summary>
        /// 是否创建桌面快捷方式
        /// </summary>
        public bool CreateDesktopShortcut { get; set; } = true;

        /// <summary>
        /// 是否创建开始菜单快捷方式
        /// </summary>
        public bool CreateStartMenuShortcut { get; set; } = true;

        /// <summary>
        /// 自定义参数
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 卸载选项
    /// </summary>
    public class UninstallationOptions
    {
        /// <summary>
        /// 是否静默卸载
        /// </summary>
        public bool SilentUninstall { get; set; } = false;

        /// <summary>
        /// 是否保留用户数据
        /// </summary>
        public bool KeepUserData { get; set; } = false;

        /// <summary>
        /// 是否保留配置文件
        /// </summary>
        public bool KeepConfiguration { get; set; } = false;

        /// <summary>
        /// 自定义参数
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// UI页面基类
    /// </summary>
    public abstract class UIPage
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 页面描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 背景图片
        /// </summary>
        public string BackgroundImage { get; set; }
    }

    /// <summary>
    /// 欢迎页面
    /// </summary>
    public class WelcomePage : UIPage
    {
        /// <summary>
        /// 欢迎消息
        /// </summary>
        public string WelcomeMessage { get; set; }

        /// <summary>
        /// 产品信息
        /// </summary>
        public string ProductInfo { get; set; }
    }

    /// <summary>
    /// 许可协议页面
    /// </summary>
    public class LicensePage : UIPage
    {
        /// <summary>
        /// 许可协议文本
        /// </summary>
        public string LicenseText { get; set; }

        /// <summary>
        /// 许可协议文件路径
        /// </summary>
        public string LicenseFilePath { get; set; }

        /// <summary>
        /// 是否必须接受
        /// </summary>
        public bool MustAccept { get; set; } = true;
    }

    /// <summary>
    /// 安装目录页面
    /// </summary>
    public class DirectoryPage : UIPage
    {
        /// <summary>
        /// 默认目录
        /// </summary>
        public string DefaultDirectory { get; set; }

        /// <summary>
        /// 是否允许更改
        /// </summary>
        public bool AllowChange { get; set; } = true;

        /// <summary>
        /// 磁盘空间检查
        /// </summary>
        public bool CheckDiskSpace { get; set; } = true;
    }

    /// <summary>
    /// 组件选择页面
    /// </summary>
    public class ComponentsPage : UIPage
    {
        /// <summary>
        /// 可选组件列表
        /// </summary>
        public List<InstallComponent> Components { get; set; } = new List<InstallComponent>();
    }

    /// <summary>
    /// 进度页面
    /// </summary>
    public class ProgressPage : UIPage
    {
        /// <summary>
        /// 是否显示详细信息
        /// </summary>
        public bool ShowDetails { get; set; } = true;

        /// <summary>
        /// 是否显示文件名
        /// </summary>
        public bool ShowFileNames { get; set; } = false;
    }

    /// <summary>
    /// 完成页面
    /// </summary>
    public class FinishPage : UIPage
    {
        /// <summary>
        /// 完成消息
        /// </summary>
        public string CompletionMessage { get; set; }

        /// <summary>
        /// 是否显示启动选项
        /// </summary>
        public bool ShowLaunchOption { get; set; } = true;

        /// <summary>
        /// 启动命令
        /// </summary>
        public string LaunchCommand { get; set; }
    }

    /// <summary>
    /// 安装组件
    /// </summary>
    public class InstallComponent
    {
        /// <summary>
        /// 组件ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 组件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 组件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 组件大小（字节）
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// 是否默认选中
        /// </summary>
        public bool DefaultSelected { get; set; } = true;

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 包含的文件
        /// </summary>
        public List<string> Files { get; set; } = new List<string>();
    }

    /// <summary>
    /// 步骤类型
    /// </summary>
    public enum StepType
    {
        /// <summary>
        /// 复制文件
        /// </summary>
        CopyFiles,

        /// <summary>
        /// 创建目录
        /// </summary>
        CreateDirectory,

        /// <summary>
        /// 注册表操作
        /// </summary>
        Registry,

        /// <summary>
        /// 创建快捷方式
        /// </summary>
        CreateShortcut,

        /// <summary>
        /// 执行脚本
        /// </summary>
        ExecuteScript,

        /// <summary>
        /// 安装服务
        /// </summary>
        InstallService,

        /// <summary>
        /// 文件关联
        /// </summary>
        FileAssociation,

        /// <summary>
        /// 环境变量
        /// </summary>
        EnvironmentVariable,

        /// <summary>
        /// 自定义操作
        /// </summary>
        CustomAction
    }
}