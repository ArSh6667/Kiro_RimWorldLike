using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组验证器接口
    /// </summary>
    public interface IModValidator
    {
        /// <summary>
        /// 验证模组
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>验证结果</returns>
        Task<ModValidationResult> ValidateAsync(string modPath);
    }

    /// <summary>
    /// 模组安全管理器接口
    /// </summary>
    public interface IModSecurityManager
    {
        /// <summary>
        /// 安全地加载程序集
        /// </summary>
        /// <param name="assemblyPath">程序集路径</param>
        /// <returns>加载的程序集</returns>
        Task<System.Reflection.Assembly> LoadAssemblySecurelyAsync(string assemblyPath);

        /// <summary>
        /// 检查程序集安全性
        /// </summary>
        /// <param name="assemblyPath">程序集路径</param>
        /// <returns>安全检查结果</returns>
        Task<SecurityCheckResult> CheckAssemblySecurityAsync(string assemblyPath);

        /// <summary>
        /// 创建安全的应用程序域
        /// </summary>
        /// <param name="modId">模组ID</param>
        /// <returns>应用程序域</returns>
        System.AppDomain CreateSecureAppDomain(string modId);
    }

    /// <summary>
    /// 安全检查结果
    /// </summary>
    public class SecurityCheckResult
    {
        /// <summary>
        /// 是否安全
        /// </summary>
        public bool IsSecure { get; set; }

        /// <summary>
        /// 安全级别
        /// </summary>
        public SecurityLevel SecurityLevel { get; set; }

        /// <summary>
        /// 检测到的威胁
        /// </summary>
        public System.Collections.Generic.List<SecurityThreat> Threats { get; set; } = new System.Collections.Generic.List<SecurityThreat>();

        /// <summary>
        /// 检查详细信息
        /// </summary>
        public string Details { get; set; }
    }

    /// <summary>
    /// 安全威胁
    /// </summary>
    public class SecurityThreat
    {
        /// <summary>
        /// 威胁类型
        /// </summary>
        public ThreatType Type { get; set; }

        /// <summary>
        /// 威胁描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 威胁级别
        /// </summary>
        public ThreatLevel Level { get; set; }

        /// <summary>
        /// 相关文件或方法
        /// </summary>
        public string Location { get; set; }
    }

    /// <summary>
    /// 威胁类型
    /// </summary>
    public enum ThreatType
    {
        /// <summary>
        /// 文件系统访问
        /// </summary>
        FileSystemAccess,

        /// <summary>
        /// 网络访问
        /// </summary>
        NetworkAccess,

        /// <summary>
        /// 注册表访问
        /// </summary>
        RegistryAccess,

        /// <summary>
        /// 进程操作
        /// </summary>
        ProcessManipulation,

        /// <summary>
        /// 反射使用
        /// </summary>
        ReflectionUsage,

        /// <summary>
        /// 不安全代码
        /// </summary>
        UnsafeCode,

        /// <summary>
        /// 恶意代码模式
        /// </summary>
        MaliciousPattern
    }

    /// <summary>
    /// 威胁级别
    /// </summary>
    public enum ThreatLevel
    {
        /// <summary>
        /// 低
        /// </summary>
        Low,

        /// <summary>
        /// 中等
        /// </summary>
        Medium,

        /// <summary>
        /// 高
        /// </summary>
        High,

        /// <summary>
        /// 严重
        /// </summary>
        Critical
    }
}