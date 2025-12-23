using System;
using System.IO;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 游戏状态序列化器接口
    /// </summary>
    public interface IGameStateSerializer
    {
        /// <summary>
        /// 序列化游戏状态到流
        /// </summary>
        /// <param name="gameState">游戏状态</param>
        /// <param name="stream">目标流</param>
        /// <param name="options">序列化选项</param>
        Task SerializeAsync(GameState gameState, Stream stream, SerializationOptions options = null);

        /// <summary>
        /// 从流反序列化游戏状态
        /// </summary>
        /// <param name="stream">源流</param>
        /// <param name="options">反序列化选项</param>
        /// <returns>游戏状态</returns>
        Task<GameState> DeserializeAsync(Stream stream, SerializationOptions options = null);

        /// <summary>
        /// 序列化游戏状态到字节数组
        /// </summary>
        /// <param name="gameState">游戏状态</param>
        /// <param name="options">序列化选项</param>
        /// <returns>序列化后的字节数组</returns>
        Task<byte[]> SerializeToBytesAsync(GameState gameState, SerializationOptions options = null);

        /// <summary>
        /// 从字节数组反序列化游戏状态
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="options">反序列化选项</param>
        /// <returns>游戏状态</returns>
        Task<GameState> DeserializeFromBytesAsync(byte[] data, SerializationOptions options = null);

        /// <summary>
        /// 验证序列化数据的完整性
        /// </summary>
        /// <param name="data">序列化数据</param>
        /// <returns>是否有效</returns>
        bool ValidateSerializedData(byte[] data);
    }

    /// <summary>
    /// 序列化选项
    /// </summary>
    public class SerializationOptions
    {
        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// 压缩级别 (0-9)
        /// </summary>
        public int CompressionLevel { get; set; } = 6;

        /// <summary>
        /// 是否启用增量保存
        /// </summary>
        public bool EnableIncrementalSave { get; set; } = false;

        /// <summary>
        /// 基准状态（用于增量保存）
        /// </summary>
        public GameState BaselineState { get; set; }

        /// <summary>
        /// 是否包含调试信息
        /// </summary>
        public bool IncludeDebugInfo { get; set; } = false;

        /// <summary>
        /// 序列化格式版本
        /// </summary>
        public int FormatVersion { get; set; } = 1;
    }
}