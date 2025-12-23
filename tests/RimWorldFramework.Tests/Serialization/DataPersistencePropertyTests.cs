using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Serialization;
using RimWorldFramework.Core.Serialization.Migrators;
using RimWorldFramework.Core.Configuration;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.MapGeneration;

namespace RimWorldFramework.Tests.Serialization
{
    [TestFixture]
    public class DataPersistencePropertyTests
    {
        private IGameStateSerializer _serializer;
        private IVersionCompatibilityManager _versionManager;
        private VersionAwareGameStateSerializer _versionAwareSerializer;

        [SetUp]
        public void Setup()
        {
            _serializer = new JsonGameStateSerializer();
            _versionManager = new VersionCompatibilityManager(2);
            _versionManager.RegisterMigrator(new Version1To2Migrator());
            _versionAwareSerializer = new VersionAwareGameStateSerializer(_serializer, _versionManager);
        }

        /// <summary>
        /// 属性 10: 任务状态持久化往返
        /// 对于任何有效的任务树状态，保存然后加载应当产生等价的任务树状态
        /// 验证需求: 需求 3.5
        /// </summary>
        [Property]
        public Property TaskStatePersistenceRoundTrip()
        {
            var taskStateGen = Gen.Fresh(() => GenerateTaskTreeState());

            return Prop.ForAll(taskStateGen, async taskState =>
            {
                var gameState = new GameState
                {
                    Version = 2,
                    TaskState = taskState,
                    Configuration = new GameConfig()
                };

                // 序列化
                var serializedData = await _serializer.SerializeToBytesAsync(gameState);

                // 反序列化
                var deserializedState = await _serializer.DeserializeFromBytesAsync(serializedData);

                // 验证任务状态是否相等
                return AreTaskStatesEqual(taskState, deserializedState.TaskState);
            });
        }

        /// <summary>
        /// 属性 18: 游戏状态保存压缩
        /// 对于任何游戏状态保存操作，保存文件应当使用压缩格式且大小应当小于未压缩版本
        /// 验证需求: 需求 6.4
        /// </summary>
        [Property]
        public Property GameStateSaveCompression()
        {
            var gameStateGen = Gen.Fresh(() => GenerateGameState());

            return Prop.ForAll(gameStateGen, async gameState =>
            {
                // 不压缩的序列化
                var uncompressedOptions = new SerializationOptions { EnableCompression = false };
                var uncompressedData = await _serializer.SerializeToBytesAsync(gameState, uncompressedOptions);

                // 压缩的序列化
                var compressedOptions = new SerializationOptions { EnableCompression = true };
                var compressedData = await _serializer.SerializeToBytesAsync(gameState, compressedOptions);

                // 验证压缩数据小于未压缩数据
                return compressedData.Length < uncompressedData.Length;
            });
        }

        /// <summary>
        /// 属性 26: 游戏数据序列化往返
        /// 对于任何有效的游戏状态，序列化然后反序列化应当产生等价的游戏状态
        /// 验证需求: 需求 8.3
        /// </summary>
        [Property]
        public Property GameDataSerializationRoundTrip()
        {
            var gameStateGen = Gen.Fresh(() => GenerateGameState());

            return Prop.ForAll(gameStateGen, async gameState =>
            {
                // 序列化
                var serializedData = await _serializer.SerializeToBytesAsync(gameState);

                // 反序列化
                var deserializedState = await _serializer.DeserializeFromBytesAsync(serializedData);

                // 验证游戏状态是否相等
                return AreGameStatesEqual(gameState, deserializedState);
            });
        }
        /// <summary>
        /// 属性 27: 数据版本兼容性
        /// 对于任何旧版本的游戏数据文件，系统应当能够正确读取并迁移到当前版本格式
        /// 验证需求: 需求 8.4
        /// </summary>
        [Property]
        public Property DataVersionCompatibility()
        {
            var oldVersionGameStateGen = Gen.Fresh(() => GenerateOldVersionGameState());

            return Prop.ForAll(oldVersionGameStateGen, async oldGameState =>
            {
                // 使用旧版本序列化器序列化
                var oldSerializer = new JsonGameStateSerializer();
                var serializedData = await oldSerializer.SerializeToBytesAsync(oldGameState);

                // 使用版本感知序列化器反序列化（应该自动迁移）
                var migratedState = await _versionAwareSerializer.DeserializeFromBytesAsync(serializedData);

                // 验证迁移后的版本是当前版本
                return migratedState.Version == _versionManager.CurrentVersion &&
                       migratedState.CreatedAt == oldGameState.CreatedAt &&
                       migratedState.GameTime == oldGameState.GameTime;
            });
        }

        // 辅助方法：生成测试数据
        private TaskTreeState GenerateTaskTreeState()
        {
            var random = new Random();
            var taskCount = random.Next(1, 10);
            var tasks = new List<TaskData>();

            for (int i = 0; i < taskCount; i++)
            {
                tasks.Add(new TaskData
                {
                    Id = $"task_{i}",
                    Name = $"Test Task {i}",
                    Type = (TaskType)(i % 3),
                    Priority = (TaskPriority)(i % 4),
                    Location = new Vector3(random.Next(100), random.Next(100), 0),
                    Parameters = new Dictionary<string, object>
                    {
                        { "param1", random.Next(100) },
                        { "param2", random.NextDouble() }
                    },
                    Prerequisites = new List<string>()
                });
            }

            return new TaskTreeState
            {
                Tasks = tasks,
                TaskStatuses = tasks.ToDictionary(t => t.Id, t => (TaskStatus)(random.Next(4))),
                Dependencies = new List<TaskDependency>(),
                TaskProgress = tasks.ToDictionary(t => t.Id, t => (object)random.NextDouble())
            };
        }

        private GameState GenerateGameState()
        {
            var random = new Random();
            return new GameState
            {
                Version = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30)),
                GameTime = TimeSpan.FromHours(random.Next(1000)),
                Configuration = new GameConfig(),
                Characters = GenerateCharacterData(random.Next(1, 5)),
                TaskState = GenerateTaskTreeState(),
                MapData = GenerateMapData(),
                SystemStates = new Dictionary<string, object>
                {
                    { "system1", random.Next(100) },
                    { "system2", random.NextDouble() }
                },
                CustomData = new Dictionary<string, object>
                {
                    { "custom1", "test_value" },
                    { "custom2", random.Next(1000) }
                }
            };
        }

        private GameState GenerateOldVersionGameState()
        {
            var gameState = GenerateGameState();
            gameState.Version = 1; // 旧版本
            return gameState;
        }
        private List<CharacterEntityData> GenerateCharacterData(int count)
        {
            var random = new Random();
            var characters = new List<CharacterEntityData>();

            for (int i = 0; i < count; i++)
            {
                characters.Add(new CharacterEntityData
                {
                    EntityId = (uint)(i + 1),
                    Name = $"Character_{i}",
                    Position = new Vector3(random.Next(100), random.Next(100), 0),
                    Skills = new SkillData
                    {
                        SkillLevels = new Dictionary<string, int>
                        {
                            { "Construction", random.Next(20) },
                            { "Mining", random.Next(20) },
                            { "Cooking", random.Next(20) }
                        },
                        SkillExperience = new Dictionary<string, float>
                        {
                            { "Construction", (float)random.NextDouble() * 100 },
                            { "Mining", (float)random.NextDouble() * 100 },
                            { "Cooking", (float)random.NextDouble() * 100 }
                        }
                    },
                    Needs = new NeedData
                    {
                        Hunger = (float)random.NextDouble(),
                        Sleep = (float)random.NextDouble(),
                        Recreation = (float)random.NextDouble(),
                        Comfort = (float)random.NextDouble()
                    },
                    Inventory = new InventoryData
                    {
                        MaxCapacity = random.Next(50, 200),
                        Items = new List<ItemData>
                        {
                            new ItemData
                            {
                                ItemType = "Wood",
                                Quantity = random.Next(1, 100),
                                Quality = (float)random.NextDouble()
                            }
                        }
                    }
                });
            }

            return characters;
        }

        private GameMapData GenerateMapData()
        {
            var random = new Random();
            var width = 50;
            var height = 50;

            return new GameMapData
            {
                Width = width,
                Height = height,
                Seed = random.Next(10000),
                Terrain = new TerrainType[width, height],
                HeightMap = new float[width, height],
                Resources = new List<ResourcePointData>
                {
                    new ResourcePointData
                    {
                        Position = new Vector3(random.Next(width), random.Next(height), 0),
                        Type = ResourceType.Wood,
                        Amount = random.Next(50, 200),
                        Quality = (float)random.NextDouble(),
                        IsExhausted = false
                    }
                }
            };
        }
        // 辅助方法：比较对象是否相等
        private bool AreTaskStatesEqual(TaskTreeState a, TaskTreeState b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Tasks?.Count == b.Tasks?.Count &&
                   a.TaskStatuses?.Count == b.TaskStatuses?.Count &&
                   a.Dependencies?.Count == b.Dependencies?.Count &&
                   a.TaskProgress?.Count == b.TaskProgress?.Count;
        }

        private bool AreGameStatesEqual(GameState a, GameState b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Version == b.Version &&
                   a.CreatedAt.Equals(b.CreatedAt) &&
                   a.GameTime.Equals(b.GameTime) &&
                   a.Characters?.Count == b.Characters?.Count &&
                   AreTaskStatesEqual(a.TaskState, b.TaskState) &&
                   AreMapDataEqual(a.MapData, b.MapData);
        }

        private bool AreMapDataEqual(GameMapData a, GameMapData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Width == b.Width &&
                   a.Height == b.Height &&
                   a.Seed == b.Seed &&
                   a.Resources?.Count == b.Resources?.Count;
        }
    }
}