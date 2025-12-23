using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组冲突检测结果
    /// </summary>
    public class ModConflictDetectionResult
    {
        /// <summary>
        /// 是否存在冲突
        /// </summary>
        public bool HasConflicts { get; set; }

        /// <summary>
        /// 检测到的冲突列表
        /// </summary>
        public List<ModConflict> Conflicts { get; set; } = new List<ModConflict>();

        /// <summary>
        /// 检测详细信息
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 模组冲突
    /// </summary>
    public class ModConflict
    {
        /// <summary>
        /// 冲突ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 冲突类型
        /// </summary>
        public ConflictType Type { get; set; }

        /// <summary>
        /// 冲突严重程度
        /// </summary>
        public ConflictSeverity Severity { get; set; }

        /// <summary>
        /// 涉及的模组ID列表
        /// </summary>
        public List<string> InvolvedMods { get; set; } = new List<string>();

        /// <summary>
        /// 冲突描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 冲突详细信息
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 建议的解决方案
        /// </summary>
        public List<ConflictResolution> SuggestedResolutions { get; set; } = new List<ConflictResolution>();

        /// <summary>
        /// 冲突的资源或文件
        /// </summary>
        public List<string> ConflictingResources { get; set; } = new List<string>();
    }

    /// <summary>
    /// 冲突解决结果
    /// </summary>
    public class ConflictResolutionResult
    {
        /// <summary>
        /// 是否成功解决
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// 已解决的冲突列表
        /// </summary>
        public List<ModConflict> ResolvedConflicts { get; set; } = new List<ModConflict>();

        /// <summary>
        /// 未解决的冲突列表
        /// </summary>
        public List<ModConflict> UnresolvedConflicts { get; set; } = new List<ModConflict>();

        /// <summary>
        /// 应用的解决方案
        /// </summary>
        public List<ConflictResolution> AppliedResolutions { get; set; } = new List<ConflictResolution>();

        /// <summary>
        /// 解决详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 冲突解决方案
    /// </summary>
    public class ConflictResolution
    {
        /// <summary>
        /// 解决方案ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 解决方案类型
        /// </summary>
        public ResolutionType Type { get; set; }

        /// <summary>
        /// 解决方案描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否为自动解决方案
        /// </summary>
        public bool IsAutomatic { get; set; }

        /// <summary>
        /// 解决方案参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 解决方案的副作用或风险
        /// </summary>
        public List<string> SideEffects { get; set; } = new List<string>();
    }

    /// <summary>
    /// 冲突类型
    /// </summary>
    public enum ConflictType
    {
        /// <summary>
        /// 依赖冲突
        /// </summary>
        DependencyConflict,

        /// <summary>
        /// 版本冲突
        /// </summary>
        VersionConflict,

        /// <summary>
        /// 资源冲突
        /// </summary>
        ResourceConflict,

        /// <summary>
        /// API冲突
        /// </summary>
        ApiConflict,

        /// <summary>
        /// 功能冲突
        /// </summary>
        FeatureConflict,

        /// <summary>
        /// 加载顺序冲突
        /// </summary>
        LoadOrderConflict,

        /// <summary>
        /// 兼容性冲突
        /// </summary>
        CompatibilityConflict
    }

    /// <summary>
    /// 冲突严重程度
    /// </summary>
    public enum ConflictSeverity
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
        /// 严重错误
        /// </summary>
        Critical
    }

    /// <summary>
    /// 解决方案类型
    /// </summary>
    public enum ResolutionType
    {
        /// <summary>
        /// 禁用模组
        /// </summary>
        DisableMod,

        /// <summary>
        /// 更改加载顺序
        /// </summary>
        ChangeLoadOrder,

        /// <summary>
        /// 更新模组
        /// </summary>
        UpdateMod,

        /// <summary>
        /// 安装依赖
        /// </summary>
        InstallDependency,

        /// <summary>
        /// 配置覆盖
        /// </summary>
        ConfigurationOverride,

        /// <summary>
        /// 手动解决
        /// </summary>
        ManualResolution
    }
}