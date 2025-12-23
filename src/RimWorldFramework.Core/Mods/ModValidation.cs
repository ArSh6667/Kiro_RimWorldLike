using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组验证结果
    /// </summary>
    public class ModValidationResult
    {
        /// <summary>
        /// 验证是否通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 安全级别
        /// </summary>
        public SecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();

        /// <summary>
        /// 验证详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 依赖关系检查结果
    /// </summary>
    public class DependencyCheckResult
    {
        /// <summary>
        /// 依赖关系是否满足
        /// </summary>
        public bool IsSatisfied { get; set; }

        /// <summary>
        /// 缺失的依赖项
        /// </summary>
        public List<ModDependency> MissingDependencies { get; set; } = new List<ModDependency>();

        /// <summary>
        /// 版本冲突的依赖项
        /// </summary>
        public List<VersionConflict> VersionConflicts { get; set; } = new List<VersionConflict>();

        /// <summary>
        /// 循环依赖检测结果
        /// </summary>
        public List<CircularDependency> CircularDependencies { get; set; } = new List<CircularDependency>();
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        public ValidationErrorType Type { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 错误详细信息
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 相关文件路径
        /// </summary>
        public string FilePath { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// 警告类型
        /// </summary>
        public ValidationWarningType Type { get; set; }

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 警告详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 版本冲突
    /// </summary>
    public class VersionConflict
    {
        /// <summary>
        /// 依赖的模组ID
        /// </summary>
        public string ModId { get; set; }

        /// <summary>
        /// 要求的版本
        /// </summary>
        public ModDependency RequiredDependency { get; set; }

        /// <summary>
        /// 实际的版本
        /// </summary>
        public Version ActualVersion { get; set; }

        /// <summary>
        /// 冲突描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 循环依赖
    /// </summary>
    public class CircularDependency
    {
        /// <summary>
        /// 循环依赖路径
        /// </summary>
        public List<string> DependencyPath { get; set; } = new List<string>();

        /// <summary>
        /// 循环描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 安全级别
    /// </summary>
    public enum SecurityLevel
    {
        /// <summary>
        /// 安全
        /// </summary>
        Safe,

        /// <summary>
        /// 低风险
        /// </summary>
        LowRisk,

        /// <summary>
        /// 中等风险
        /// </summary>
        MediumRisk,

        /// <summary>
        /// 高风险
        /// </summary>
        HighRisk,

        /// <summary>
        /// 危险
        /// </summary>
        Dangerous
    }

    /// <summary>
    /// 验证错误类型
    /// </summary>
    public enum ValidationErrorType
    {
        /// <summary>
        /// 文件不存在
        /// </summary>
        FileNotFound,

        /// <summary>
        /// 无效的模组清单
        /// </summary>
        InvalidManifest,

        /// <summary>
        /// 恶意代码检测
        /// </summary>
        MaliciousCode,

        /// <summary>
        /// 无效的程序集
        /// </summary>
        InvalidAssembly,

        /// <summary>
        /// 权限不足
        /// </summary>
        InsufficientPermissions,

        /// <summary>
        /// 签名验证失败
        /// </summary>
        SignatureVerificationFailed
    }

    /// <summary>
    /// 验证警告类型
    /// </summary>
    public enum ValidationWarningType
    {
        /// <summary>
        /// 性能影响
        /// </summary>
        PerformanceImpact,

        /// <summary>
        /// 兼容性问题
        /// </summary>
        CompatibilityIssue,

        /// <summary>
        /// 过时的API使用
        /// </summary>
        DeprecatedApiUsage,

        /// <summary>
        /// 资源使用过多
        /// </summary>
        ExcessiveResourceUsage
    }
}