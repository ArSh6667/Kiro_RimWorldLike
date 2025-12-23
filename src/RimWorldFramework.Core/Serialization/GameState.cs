using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Configuration;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 游戏状态类，包含所有需要持久化的游戏数据
    /// </summary>
    [Serializable]
    public class GameState
    {
        /// <summary>
        /// 游戏状态版本
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后保存时间
        /// </summary>
        public DateTime LastSavedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 游戏时间（游戏内经过的时间）
        /// </summary>
        public TimeSpan GameTime { get; set; }

        /// <summary>
        /// 游戏配置
        /// </summary>
        public GameConfig Configuration { get; set; }

        /// <summary>
        /// 角色实体列表
        /// </summary>
        public List<CharacterEntityData> Characters { get; set; } = new List<CharacterEntityData>();

        /// <summary>
        /// 任务状态数据
        /// </summary>
        public TaskTreeState TaskState { get; set; } = new TaskTreeState();

        /// <summary>
        /// 地图数据
        /// </summary>
        public GameMapData MapData { get; set; }

        /// <summary>
        /// 系统状态数据
        /// </summary>
        public Dictionary<string, object> SystemStates { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 自定义数据（用于模组扩展）
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 校验和（用于数据完整性验证）
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// 计算并更新校验和
        /// </summary>
        public void UpdateChecksum()
        {
            // 简单的校验和计算，实际应用中可以使用更复杂的算法
            var hash = HashCode.Combine(
                Version,
                CreatedAt,
                GameTime,
                Characters?.Count ?? 0,
                TaskState?.GetHashCode() ?? 0,
                MapData?.GetHashCode() ?? 0
            );
            Checksum = hash.ToString("X");
        }

        /// <summary>
        /// 验证校验和
        /// </summary>
        /// <returns>校验和是否有效</returns>
        public bool ValidateChecksum()
        {
            var currentChecksum = Checksum;
            UpdateChecksum();
            var calculatedChecksum = Checksum;
            Checksum = currentChecksum;
            return currentChecksum == calculatedChecksum;
        }
    }
}