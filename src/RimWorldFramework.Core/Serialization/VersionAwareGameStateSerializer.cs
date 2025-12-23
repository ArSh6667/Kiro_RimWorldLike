using System;
using System.IO;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 版本感知的游戏状态序列化器
    /// </summary>
    public class VersionAwareGameStateSerializer : IGameStateSerializer
    {
        private readonly IGameStateSerializer _baseSerializer;
        private readonly IVersionCompatibilityManager _versionManager;

        public VersionAwareGameStateSerializer(
            IGameStateSerializer baseSerializer,
            IVersionCompatibilityManager versionManager)
        {
            _baseSerializer = baseSerializer ?? throw new ArgumentNullException(nameof(baseSerializer));
            _versionManager = versionManager ?? throw new ArgumentNullException(nameof(versionManager));
        }

        public async Task SerializeAsync(GameState gameState, Stream stream, SerializationOptions options = null)
        {
            // 确保游戏状态版本是当前版本
            if (gameState.Version != _versionManager.CurrentVersion)
            {
                gameState.Version = _versionManager.CurrentVersion;
            }

            await _baseSerializer.SerializeAsync(gameState, stream, options);
        }

        public async Task<GameState> DeserializeAsync(Stream stream, SerializationOptions options = null)
        {
            // 首先使用基础序列化器反序列化
            var gameState = await _baseSerializer.DeserializeAsync(stream, options);

            // 检查版本兼容性
            var compatibility = _versionManager.CheckCompatibility(gameState.Version);

            if (!compatibility.IsCompatible)
            {
                throw new InvalidOperationException($"Game state version {gameState.Version} is not compatible with current version {_versionManager.CurrentVersion}. {compatibility.Description}");
            }

            // 如果需要迁移，执行迁移
            if (compatibility.RequiresMigration)
            {
                gameState = await _versionManager.MigrateToCurrentVersionAsync(gameState);
            }

            return gameState;
        }

        public async Task<byte[]> SerializeToBytesAsync(GameState gameState, SerializationOptions options = null)
        {
            using var memoryStream = new MemoryStream();
            await SerializeAsync(gameState, memoryStream, options);
            return memoryStream.ToArray();
        }

        public async Task<GameState> DeserializeFromBytesAsync(byte[] data, SerializationOptions options = null)
        {
            using var memoryStream = new MemoryStream(data);
            return await DeserializeAsync(memoryStream, options);
        }

        public bool ValidateSerializedData(byte[] data)
        {
            return _baseSerializer.ValidateSerializedData(data);
        }

        /// <summary>
        /// 检查序列化数据的版本兼容性
        /// </summary>
        /// <param name="data">序列化数据</param>
        /// <returns>兼容性信息</returns>
        public async Task<VersionCompatibilityInfo> CheckDataCompatibilityAsync(byte[] data)
        {
            try
            {
                // 尝试反序列化以获取版本信息
                var gameState = await _baseSerializer.DeserializeFromBytesAsync(data);
                return _versionManager.CheckCompatibility(gameState.Version);
            }
            catch (Exception ex)
            {
                return new VersionCompatibilityInfo
                {
                    IsCompatible = false,
                    RequiresMigration = false,
                    Level = CompatibilityLevel.Incompatible,
                    Description = $"Failed to read version information: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取序列化数据的版本信息
        /// </summary>
        /// <param name="data">序列化数据</param>
        /// <returns>版本号，如果无法读取则返回-1</returns>
        public async Task<int> GetDataVersionAsync(byte[] data)
        {
            try
            {
                var gameState = await _baseSerializer.DeserializeFromBytesAsync(data);
                return gameState.Version;
            }
            catch
            {
                return -1;
            }
        }
    }
}