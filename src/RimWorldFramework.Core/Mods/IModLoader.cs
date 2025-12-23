using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组加载器接口
    /// </summary>
    public interface IModLoader
    {
        /// <summary>
        /// 加载模组
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>加载的模组</returns>
        Task<IMod> LoadModAsync(string modPath);

        /// <summary>
        /// 卸载模组
        /// </summary>
        /// <param name="mod">要卸载的模组</param>
        Task UnloadModAsync(IMod mod);

        /// <summary>
        /// 验证模组安全性
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>验证结果</returns>
        Task<ModValidationResult> ValidateModAsync(string modPath);

        /// <summary>
        /// 获取所有已加载的模组
        /// </summary>
        /// <returns>已加载的模组列表</returns>
        IEnumerable<IMod> GetLoadedMods();

        /// <summary>
        /// 检查模组依赖关系
        /// </summary>
        /// <param name="mod">要检查的模组</param>
        /// <returns>依赖关系检查结果</returns>
        Task<DependencyCheckResult> CheckDependenciesAsync(IMod mod);
    }

    /// <summary>
    /// 模组接口
    /// </summary>
    public interface IMod
    {
        /// <summary>
        /// 模组ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 模组名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 模组版本
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// 模组描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 模组作者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 模组依赖项
        /// </summary>
        IEnumerable<ModDependency> Dependencies { get; }

        /// <summary>
        /// 模组状态
        /// </summary>
        ModStatus Status { get; }

        /// <summary>
        /// 模组路径
        /// </summary>
        string Path { get; }

        /// <summary>
        /// 初始化模组
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 启动模组
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止模组
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 清理模组资源
        /// </summary>
        Task CleanupAsync();
    }

    /// <summary>
    /// 模组依赖项
    /// </summary>
    public class ModDependency
    {
        /// <summary>
        /// 依赖的模组ID
        /// </summary>
        public string ModId { get; set; }

        /// <summary>
        /// 最小版本要求
        /// </summary>
        public Version MinVersion { get; set; }

        /// <summary>
        /// 最大版本要求
        /// </summary>
        public Version MaxVersion { get; set; }

        /// <summary>
        /// 是否为可选依赖
        /// </summary>
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// 模组状态
    /// </summary>
    public enum ModStatus
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,

        /// <summary>
        /// 正在加载
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 正在运行
        /// </summary>
        Running,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }
}