using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 版本迁移器接口
    /// </summary>
    public interface IVersionMigrator
    {
        /// <summary>
        /// 支持的源版本
        /// </summary>
        int SourceVersion { get; }

        /// <summary>
        /// 目标版本
        /// </summary>
        int TargetVersion { get; }

        /// <summary>
        /// 迁移游戏状态
        /// </summary>
        /// <param name="gameState">源游戏状态</param>
        /// <returns>迁移后的游戏状态</returns>
        Task<GameState> MigrateAsync(GameState gameState);

        /// <summary>
        /// 验证是否可以迁移
        /// </summary>
        /// <param name="gameState">游戏状态</param>
        /// <returns>是否可以迁移</returns>
        bool CanMigrate(GameState gameState);
    }

    /// <summary>
    /// 版本兼容性管理器接口
    /// </summary>
    public interface IVersionCompatibilityManager
    {
        /// <summary>
        /// 当前支持的版本
        /// </summary>
        int CurrentVersion { get; }

        /// <summary>
        /// 注册版本迁移器
        /// </summary>
        /// <param name="migrator">迁移器</param>
        void RegisterMigrator(IVersionMigrator migrator);

        /// <summary>
        /// 迁移游戏状态到当前版本
        /// </summary>
        /// <param name="gameState">源游戏状态</param>
        /// <returns>迁移后的游戏状态</returns>
        Task<GameState> MigrateToCurrentVersionAsync(GameState gameState);

        /// <summary>
        /// 检查版本兼容性
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>兼容性信息</returns>
        VersionCompatibilityInfo CheckCompatibility(int version);

        /// <summary>
        /// 获取迁移路径
        /// </summary>
        /// <param name="sourceVersion">源版本</param>
        /// <param name="targetVersion">目标版本</param>
        /// <returns>迁移路径</returns>
        List<IVersionMigrator> GetMigrationPath(int sourceVersion, int targetVersion);
    }

    /// <summary>
    /// 版本兼容性信息
    /// </summary>
    public class VersionCompatibilityInfo
    {
        /// <summary>
        /// 是否兼容
        /// </summary>
        public bool IsCompatible { get; set; }

        /// <summary>
        /// 是否需要迁移
        /// </summary>
        public bool RequiresMigration { get; set; }

        /// <summary>
        /// 兼容性级别
        /// </summary>
        public CompatibilityLevel Level { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 警告信息
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// 兼容性级别
    /// </summary>
    public enum CompatibilityLevel
    {
        /// <summary>
        /// 完全兼容
        /// </summary>
        FullyCompatible,

        /// <summary>
        /// 向后兼容
        /// </summary>
        BackwardCompatible,

        /// <summary>
        /// 需要迁移
        /// </summary>
        RequiresMigration,

        /// <summary>
        /// 不兼容
        /// </summary>
        Incompatible
    }
}