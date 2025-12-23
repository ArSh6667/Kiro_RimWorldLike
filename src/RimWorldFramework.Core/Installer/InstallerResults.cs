using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Installer
{
    /// <summary>
    /// 安装程序验证结果
    /// </summary>
    public class InstallerValidationResult
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
    /// 安装程序生成结果
    /// </summary>
    public class InstallerGenerationResult
    {
        /// <summary>
        /// 生成是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 生成的安装程序路径
        /// </summary>
        public string InstallerPath { get; set; }

        /// <summary>
        /// 安装程序大小（字节）
        /// </summary>
        public long InstallerSize { get; set; }

        /// <summary>
        /// 生成时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 生成的文件列表
        /// </summary>
        public List<string> GeneratedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 生成详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 安装结果
    /// </summary>
    public class InstallationResult
    {
        /// <summary>
        /// 安装是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 安装ID
        /// </summary>
        public string InstallationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 安装开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 安装结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 安装时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 安装目录
        /// </summary>
        public string InstallDirectory { get; set; }

        /// <summary>
        /// 安装的文件列表
        /// </summary>
        public List<InstalledFile> InstalledFiles { get; set; } = new List<InstalledFile>();

        /// <summary>
        /// 创建的注册表项
        /// </summary>
        public List<RegistryEntry> RegistryEntries { get; set; } = new List<RegistryEntry>();

        /// <summary>
        /// 创建的快捷方式
        /// </summary>
        public List<Shortcut> Shortcuts { get; set; } = new List<Shortcut>();

        /// <summary>
        /// 安装的服务
        /// </summary>
        public List<InstalledService> Services { get; set; } = new List<InstalledService>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<InstallationError> Errors { get; set; } = new List<InstallationError>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<InstallationWarning> Warnings { get; set; } = new List<InstallationWarning>();

        /// <summary>
        /// 安装日志
        /// </summary>
        public List<InstallationLogEntry> Logs { get; set; } = new List<InstallationLogEntry>();
    }

    /// <summary>
    /// 卸载结果
    /// </summary>
    public class UninstallationResult
    {
        /// <summary>
        /// 卸载是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 卸载开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 卸载结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 卸载时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 删除的文件数量
        /// </summary>
        public int DeletedFilesCount { get; set; }

        /// <summary>
        /// 删除的注册表项数量
        /// </summary>
        public int DeletedRegistryEntriesCount { get; set; }

        /// <summary>
        /// 释放的磁盘空间（字节）
        /// </summary>
        public long FreedSpaceBytes { get; set; }

        /// <summary>
        /// 保留的文件列表
        /// </summary>
        public List<string> RetainedFiles { get; set; } = new List<string>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 卸载日志
        /// </summary>
        public List<InstallationLogEntry> Logs { get; set; } = new List<InstallationLogEntry>();
    }

    /// <summary>
    /// 修复结果
    /// </summary>
    public class RepairResult
    {
        /// <summary>
        /// 修复是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 修复开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 修复结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 修复时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 修复的文件数量
        /// </summary>
        public int RepairedFilesCount { get; set; }

        /// <summary>
        /// 修复的注册表项数量
        /// </summary>
        public int RepairedRegistryEntriesCount { get; set; }

        /// <summary>
        /// 修复详细信息
        /// </summary>
        public List<RepairAction> RepairActions { get; set; } = new List<RepairAction>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 更新结果
    /// </summary>
    public class UpdateResult
    {
        /// <summary>
        /// 更新是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 更新开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 更新结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 更新时长
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 旧版本
        /// </summary>
        public string OldVersion { get; set; }

        /// <summary>
        /// 新版本
        /// </summary>
        public string NewVersion { get; set; }

        /// <summary>
        /// 更新的文件数量
        /// </summary>
        public int UpdatedFilesCount { get; set; }

        /// <summary>
        /// 更新详细信息
        /// </summary>
        public List<UpdateAction> UpdateActions { get; set; } = new List<UpdateAction>();

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 安装状态
    /// </summary>
    public class InstallationStatus
    {
        /// <summary>
        /// 是否已安装
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// 安装版本
        /// </summary>
        public string InstalledVersion { get; set; }

        /// <summary>
        /// 安装目录
        /// </summary>
        public string InstallDirectory { get; set; }

        /// <summary>
        /// 安装时间
        /// </summary>
        public DateTime InstallDate { get; set; }

        /// <summary>
        /// 安装大小（字节）
        /// </summary>
        public long InstallSize { get; set; }

        /// <summary>
        /// 安装状态
        /// </summary>
        public ApplicationStatus Status { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime LastUsed { get; set; }
    }

    /// <summary>
    /// 已安装应用程序
    /// </summary>
    public class InstalledApplication
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
        /// 安装目录
        /// </summary>
        public string InstallDirectory { get; set; }

        /// <summary>
        /// 安装时间
        /// </summary>
        public DateTime InstallDate { get; set; }

        /// <summary>
        /// 安装大小（字节）
        /// </summary>
        public long InstallSize { get; set; }

        /// <summary>
        /// 卸载命令
        /// </summary>
        public string UninstallCommand { get; set; }

        /// <summary>
        /// 应用程序状态
        /// </summary>
        public ApplicationStatus Status { get; set; }
    }

    /// <summary>
    /// 已安装文件
    /// </summary>
    public class InstalledFile
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件哈希
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// 安装时间
        /// </summary>
        public DateTime InstallTime { get; set; }

        /// <summary>
        /// 文件版本
        /// </summary>
        public string FileVersion { get; set; }
    }

    /// <summary>
    /// 注册表项
    /// </summary>
    public class RegistryEntry
    {
        /// <summary>
        /// 注册表根键
        /// </summary>
        public string RootKey { get; set; }

        /// <summary>
        /// 子键路径
        /// </summary>
        public string SubKey { get; set; }

        /// <summary>
        /// 值名称
        /// </summary>
        public string ValueName { get; set; }

        /// <summary>
        /// 值数据
        /// </summary>
        public object ValueData { get; set; }

        /// <summary>
        /// 值类型
        /// </summary>
        public string ValueType { get; set; }
    }

    /// <summary>
    /// 快捷方式
    /// </summary>
    public class Shortcut
    {
        /// <summary>
        /// 快捷方式路径
        /// </summary>
        public string ShortcutPath { get; set; }

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// 工作目录
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// 图标路径
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 已安装服务
    /// </summary>
    public class InstalledService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 服务描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 可执行文件路径
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// 启动类型
        /// </summary>
        public string StartType { get; set; }

        /// <summary>
        /// 服务状态
        /// </summary>
        public string Status { get; set; }
    }

    /// <summary>
    /// 安装错误
    /// </summary>
    public class InstallationError
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
        /// 错误发生的步骤
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 安装警告
    /// </summary>
    public class InstallationWarning
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
        /// 警告发生的步骤
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 安装日志条目
    /// </summary>
    public class InstallationLogEntry
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
        /// 步骤
        /// </summary>
        public string Step { get; set; }

        /// <summary>
        /// 来源
        /// </summary>
        public string Source { get; set; }
    }

    /// <summary>
    /// 修复操作
    /// </summary>
    public class RepairAction
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// 目标路径
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public bool IsSuccess { get; set; }
    }

    /// <summary>
    /// 更新操作
    /// </summary>
    public class UpdateAction
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 旧版本
        /// </summary>
        public string OldVersion { get; set; }

        /// <summary>
        /// 新版本
        /// </summary>
        public string NewVersion { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public bool IsSuccess { get; set; }
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
    /// 应用程序状态
    /// </summary>
    public enum ApplicationStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal,

        /// <summary>
        /// 损坏
        /// </summary>
        Corrupted,

        /// <summary>
        /// 不完整
        /// </summary>
        Incomplete,

        /// <summary>
        /// 需要修复
        /// </summary>
        NeedsRepair,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown
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
}