using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组管理器接口
    /// </summary>
    public interface IModManager
    {
        /// <summary>
        /// 模组状态变化事件
        /// </summary>
        event EventHandler<ModStatusChangedEventArgs> ModStatusChanged;

        /// <summary>
        /// 模组冲突检测事件
        /// </summary>
        event EventHandler<ModConflictDetectedEventArgs> ModConflictDetected;

        /// <summary>
        /// 加载模组
        /// </summary>
        /// <param name="modPath">模组路径</param>
        /// <returns>加载的模组</returns>
        Task<IMod> LoadModAsync(string modPath);

        /// <summary>
        /// 卸载模组
        /// </summary>
        /// <param name="modId">模组ID</param>
        Task UnloadModAsync(string modId);

        /// <summary>
        /// 启用模组
        /// </summary>
        /// <param name="modId">模组ID</param>
        Task EnableModAsync(string modId);

        /// <summary>
        /// 禁用模组
        /// </summary>
        /// <param name="modId">模组ID</param>
        Task DisableModAsync(string modId);

        /// <summary>
        /// 重新加载模组（热重载）
        /// </summary>
        /// <param name="modId">模组ID</param>
        Task ReloadModAsync(string modId);

        /// <summary>
        /// 获取所有模组
        /// </summary>
        /// <returns>模组列表</returns>
        IEnumerable<IMod> GetAllMods();

        /// <summary>
        /// 获取已启用的模组
        /// </summary>
        /// <returns>已启用的模组列表</returns>
        IEnumerable<IMod> GetEnabledMods();

        /// <summary>
        /// 根据ID获取模组
        /// </summary>
        /// <param name="modId">模组ID</param>
        /// <returns>模组实例</returns>
        IMod GetMod(string modId);

        /// <summary>
        /// 检测模组冲突
        /// </summary>
        /// <returns>冲突检测结果</returns>
        Task<ModConflictDetectionResult> DetectConflictsAsync();

        /// <summary>
        /// 解决模组冲突
        /// </summary>
        /// <param name="conflicts">冲突列表</param>
        /// <returns>解决方案</returns>
        Task<ConflictResolutionResult> ResolveConflictsAsync(IEnumerable<ModConflict> conflicts);

        /// <summary>
        /// 获取模组加载顺序
        /// </summary>
        /// <returns>按加载顺序排序的模组列表</returns>
        IEnumerable<IMod> GetLoadOrder();

        /// <summary>
        /// 设置模组加载顺序
        /// </summary>
        /// <param name="modIds">按顺序排列的模组ID列表</param>
        Task SetLoadOrderAsync(IEnumerable<string> modIds);

        /// <summary>
        /// 扫描模组目录
        /// </summary>
        /// <param name="modsDirectory">模组目录路径</param>
        /// <returns>发现的模组列表</returns>
        Task<IEnumerable<ModInfo>> ScanModsDirectoryAsync(string modsDirectory);
    }

    /// <summary>
    /// 模组信息
    /// </summary>
    public class ModInfo
    {
        /// <summary>
        /// 模组路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 模组清单
        /// </summary>
        public ModManifest Manifest { get; set; }

        /// <summary>
        /// 是否已加载
        /// </summary>
        public bool IsLoaded { get; set; }

        /// <summary>
        /// 是否已启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 验证结果
        /// </summary>
        public ModValidationResult ValidationResult { get; set; }
    }

    /// <summary>
    /// 模组状态变化事件参数
    /// </summary>
    public class ModStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 模组ID
        /// </summary>
        public string ModId { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public ModStatus OldStatus { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public ModStatus NewStatus { get; set; }

        /// <summary>
        /// 状态变化原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 模组冲突检测事件参数
    /// </summary>
    public class ModConflictDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// 冲突列表
        /// </summary>
        public IEnumerable<ModConflict> Conflicts { get; set; }

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime DetectedAt { get; set; }
    }
}