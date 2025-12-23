using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 基于JSON的游戏状态序列化器
    /// </summary>
    public class JsonGameStateSerializer : IGameStateSerializer
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonGameStateSerializer()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task SerializeAsync(GameState gameState, Stream stream, SerializationOptions options = null)
        {
            options ??= new SerializationOptions();
            
            // 更新时间戳和校验和
            gameState.LastSavedAt = DateTime.UtcNow;
            gameState.UpdateChecksum();

            // 序列化到JSON
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(gameState, _jsonOptions);

            // 应用压缩（如果启用）
            if (options.EnableCompression)
            {
                await CompressAndWriteAsync(jsonBytes, stream, options.CompressionLevel);
            }
            else
            {
                await stream.WriteAsync(jsonBytes);
            }
        }

        public async Task<GameState> DeserializeAsync(Stream stream, SerializationOptions options = null)
        {
            options ??= new SerializationOptions();

            byte[] data;

            // 检测是否为压缩数据
            if (options.EnableCompression || IsCompressedData(stream))
            {
                data = await DecompressAsync(stream);
            }
            else
            {
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                data = memoryStream.ToArray();
            }

            // 反序列化JSON
            var gameState = JsonSerializer.Deserialize<GameState>(data, _jsonOptions);

            // 验证数据完整性
            if (!gameState.ValidateChecksum())
            {
                throw new InvalidDataException("Game state checksum validation failed");
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
            try
            {
                // 尝试解析JSON头部来验证格式
                using var memoryStream = new MemoryStream(data);
                
                // 检查是否为压缩数据
                if (IsCompressedData(memoryStream))
                {
                    var decompressed = DecompressAsync(memoryStream).Result;
                    return ValidateJsonStructure(decompressed);
                }
                else
                {
                    return ValidateJsonStructure(data);
                }
            }
            catch
            {
                return false;
            }
        }
        private async Task CompressAndWriteAsync(byte[] data, Stream stream, int compressionLevel)
        {
            using var gzipStream = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
            await gzipStream.WriteAsync(data);
        }

        private async Task<byte[]> DecompressAsync(Stream stream)
        {
            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var memoryStream = new MemoryStream();
            await gzipStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private bool IsCompressedData(Stream stream)
        {
            var originalPosition = stream.Position;
            try
            {
                // GZip文件的魔数是 0x1f, 0x8b
                var buffer = new byte[2];
                var bytesRead = stream.Read(buffer, 0, 2);
                return bytesRead == 2 && buffer[0] == 0x1f && buffer[1] == 0x8b;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private bool ValidateJsonStructure(byte[] data)
        {
            try
            {
                var jsonString = Encoding.UTF8.GetString(data);
                using var document = JsonDocument.Parse(jsonString);
                
                // 检查必需的根属性
                var root = document.RootElement;
                return root.TryGetProperty("version", out _) &&
                       root.TryGetProperty("createdAt", out _) &&
                       root.TryGetProperty("gameTime", out _);
            }
            catch
            {
                return false;
            }
        }
    }
}