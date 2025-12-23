using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldFramework.Core.Serialization;
using RimWorldFramework.Core.Serialization.Migrators;
using RimWorldFramework.Core.Configuration;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.MapGeneration;

namespace RimWorldFramework.Tests.Serialization
{
    [TestFixture]
    public class SerializationIntegrationTests
    {
        private IGameStateSerializer _serializer;
        private IVersionCompatibilityManager _versionManager;
        private VersionAwareGameStateSerializer _versionAwareSerializer;
        private IncrementalSaveSystem _incrementalSaveSystem;

        [SetUp]
        public void Setup()
        {
            _serializer = new JsonGameStateSerializer();
            _versionManager = new VersionCompatibilityManager(2);
            _versionManager.RegisterMigrator(new Version1To2Migrator());
            _versionAwareSerializer = new VersionAwareGameStateSerializer(_serializer, _versionManager);
            _incrementalSaveSystem = new IncrementalSaveSystem(_serializer);
        }

        [Test]
        public async Task SerializeDeserialize_CompleteGameState_ShouldPreserveAllData()
        {
            // Arrange
            var gameState = CreateCompleteGameState();

            // Act
            var serializedData = await _serializer.SerializeToBytesAsync(gameState);
            var deserializedState = await _serializer.DeserializeFromBytesAsync(serializedData);

            // Assert
            Assert.That(deserializedState, Is.Not.Null);
            Assert.That(deserializedState.Version, Is.EqualTo(gameState.Version));
            Assert.That(deserializedState.CreatedAt, Is.EqualTo(gameState.CreatedAt));
            Assert.That(deserializedState.GameTime, Is.EqualTo(gameState.GameTime));
            Assert.That(deserializedState.Characters.Count, Is.EqualTo(gameState.Characters.Count));
            Assert.That(deserializedState.TaskState.Tasks.Count, Is.EqualTo(gameState.TaskState.Tasks.Count));
            Assert.That(deserializedState.ValidateChecksum(), Is.True);
        }

        [Test]
        public async Task Compression_LargeGameState_ShouldReduceSize()
        {
            // Arrange
            var gameState = CreateLargeGameState();

            // Act
            var uncompressedData = await _serializer.SerializeToBytesAsync(gameState, 
                new SerializationOptions { EnableCompression = false });
            var compressedData = await _serializer.SerializeToBytesAsync(gameState, 
                new SerializationOptions { EnableCompression = true });

            // Assert
            Assert.That(compressedData.Length, Is.LessThan(uncompressedData.Length));
            Console.WriteLine($"Uncompressed: {uncompressedData.Length} bytes");
            Console.WriteLine($"Compressed: {compressedData.Length} bytes");
            Console.WriteLine($"Compression ratio: {(double)compressedData.Length / uncompressedData.Length:P2}");
        }

        [Test]
        public async Task VersionMigration_OldVersionToNew_ShouldMigrateSuccessfully()
        {
            // Arrange
            var oldGameState = CreateOldVersionGameState();
            var serializedData = await _serializer.SerializeToBytesAsync(oldGameState);

            // Act
            var migratedState = await _versionAwareSerializer.DeserializeFromBytesAsync(serializedData);

            // Assert
            Assert.That(migratedState.Version, Is.EqualTo(_versionManager.CurrentVersion));
            Assert.That(migratedState.CreatedAt, Is.EqualTo(oldGameState.CreatedAt));
            Assert.That(migratedState.GameTime, Is.EqualTo(oldGameState.GameTime));
            
            // 验证迁移后的技能数据包含新的默认技能
            if (migratedState.Characters.Count > 0)
            {
                var character = migratedState.Characters[0];
                Assert.That(character.Skills.SkillLevels.ContainsKey("Construction"), Is.True);
                Assert.That(character.Skills.SkillLevels.ContainsKey("Mining"), Is.True);
                Assert.That(character.Skills.SkillLevels.ContainsKey("Cooking"), Is.True);
            }
        }

        [Test]
        public async Task IncrementalSave_ModifiedGameState_ShouldCreateDelta()
        {
            // Arrange
            var baselineState = CreateCompleteGameState();
            _incrementalSaveSystem.SetBaseline(baselineState);

            var modifiedState = CreateCompleteGameState();
            modifiedState.GameTime = baselineState.GameTime.Add(TimeSpan.FromHours(1));
            modifiedState.Characters[0].Position = new Vector3(100, 100, 0);

            // Act
            using var deltaStream = new MemoryStream();
            await _incrementalSaveSystem.CreateIncrementalSaveAsync(modifiedState, deltaStream);
            
            deltaStream.Position = 0;
            var mergedState = await _incrementalSaveSystem.ApplyIncrementalSaveAsync(deltaStream);

            // Assert
            Assert.That(mergedState.GameTime, Is.EqualTo(modifiedState.GameTime));
            Assert.That(mergedState.Characters[0].Position, Is.EqualTo(modifiedState.Characters[0].Position));
        }
        [Test]
        public async Task VersionCompatibility_CheckCompatibility_ShouldReturnCorrectInfo()
        {
            // Test current version
            var currentVersionInfo = _versionManager.CheckCompatibility(_versionManager.CurrentVersion);
            Assert.That(currentVersionInfo.IsCompatible, Is.True);
            Assert.That(currentVersionInfo.RequiresMigration, Is.False);
            Assert.That(currentVersionInfo.Level, Is.EqualTo(CompatibilityLevel.FullyCompatible));

            // Test old version with migration path
            var oldVersionInfo = _versionManager.CheckCompatibility(1);
            Assert.That(oldVersionInfo.IsCompatible, Is.True);
            Assert.That(oldVersionInfo.RequiresMigration, Is.True);
            Assert.That(oldVersionInfo.Level, Is.EqualTo(CompatibilityLevel.RequiresMigration));

            // Test future version
            var futureVersionInfo = _versionManager.CheckCompatibility(10);
            Assert.That(futureVersionInfo.IsCompatible, Is.False);
            Assert.That(futureVersionInfo.Level, Is.EqualTo(CompatibilityLevel.Incompatible));
        }

        [Test]
        public void ValidateSerializedData_ValidData_ShouldReturnTrue()
        {
            // Arrange
            var gameState = CreateCompleteGameState();
            var serializedData = _serializer.SerializeToBytesAsync(gameState).Result;

            // Act
            var isValid = _serializer.ValidateSerializedData(serializedData);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void ValidateSerializedData_InvalidData_ShouldReturnFalse()
        {
            // Arrange
            var invalidData = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            var isValid = _serializer.ValidateSerializedData(invalidData);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task DataIntegrity_CorruptedChecksum_ShouldThrowException()
        {
            // Arrange
            var gameState = CreateCompleteGameState();
            gameState.Checksum = "invalid_checksum";

            // Act & Assert
            var serializedData = await _serializer.SerializeToBytesAsync(gameState);
            Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _serializer.DeserializeFromBytesAsync(serializedData);
            });
        }

        // 辅助方法
        private GameState CreateCompleteGameState()
        {
            var gameState = new GameState
            {
                Version = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                GameTime = TimeSpan.FromHours(10),
                Configuration = new GameConfig(),
                Characters = new List<CharacterEntityData>
                {
                    new CharacterEntityData
                    {
                        EntityId = 1,
                        Name = "TestCharacter",
                        Position = new Vector3(10, 20, 0),
                        Skills = new SkillData
                        {
                            SkillLevels = new Dictionary<string, int> { { "Construction", 5 } },
                            SkillExperience = new Dictionary<string, float> { { "Construction", 50.5f } }
                        },
                        Needs = new NeedData { Hunger = 0.8f, Sleep = 0.6f },
                        Inventory = new InventoryData
                        {
                            MaxCapacity = 100,
                            Items = new List<ItemData>
                            {
                                new ItemData { ItemType = "Wood", Quantity = 10, Quality = 0.8f }
                            }
                        }
                    }
                },
                TaskState = new TaskTreeState
                {
                    Tasks = new List<TaskData>
                    {
                        new TaskData
                        {
                            Id = "task1",
                            Name = "Test Task",
                            Type = TaskType.Construction,
                            Priority = TaskPriority.Normal,
                            Location = new Vector3(5, 5, 0)
                        }
                    },
                    TaskStatuses = new Dictionary<string, TaskStatus> { { "task1", TaskStatus.InProgress } }
                },
                MapData = new GameMapData
                {
                    Width = 100,
                    Height = 100,
                    Seed = 12345,
                    Terrain = new TerrainType[100, 100],
                    HeightMap = new float[100, 100],
                    Resources = new List<ResourcePointData>
                    {
                        new ResourcePointData
                        {
                            Position = new Vector3(50, 50, 0),
                            Type = ResourceType.Wood,
                            Amount = 100,
                            Quality = 0.9f
                        }
                    }
                }
            };

            gameState.UpdateChecksum();
            return gameState;
        }
        private GameState CreateLargeGameState()
        {
            var gameState = CreateCompleteGameState();
            
            // 添加大量角色数据
            for (int i = 2; i <= 100; i++)
            {
                gameState.Characters.Add(new CharacterEntityData
                {
                    EntityId = (uint)i,
                    Name = $"Character_{i}",
                    Position = new Vector3(i, i, 0),
                    Skills = new SkillData
                    {
                        SkillLevels = new Dictionary<string, int>
                        {
                            { "Construction", i % 20 },
                            { "Mining", (i + 1) % 20 },
                            { "Cooking", (i + 2) % 20 }
                        }
                    },
                    Needs = new NeedData
                    {
                        Hunger = (float)(i % 100) / 100f,
                        Sleep = (float)((i + 1) % 100) / 100f
                    },
                    Inventory = new InventoryData
                    {
                        MaxCapacity = 100,
                        Items = new List<ItemData>()
                    }
                });
            }

            // 添加大量任务数据
            for (int i = 2; i <= 50; i++)
            {
                gameState.TaskState.Tasks.Add(new TaskData
                {
                    Id = $"task{i}",
                    Name = $"Task {i}",
                    Type = (TaskType)(i % 3),
                    Priority = (TaskPriority)(i % 4),
                    Location = new Vector3(i * 2, i * 3, 0),
                    Parameters = new Dictionary<string, object>
                    {
                        { "param1", i },
                        { "param2", i * 1.5 },
                        { "description", $"This is a test task number {i} with some description text" }
                    }
                });

                gameState.TaskState.TaskStatuses[$"task{i}"] = (TaskStatus)(i % 4);
            }

            gameState.UpdateChecksum();
            return gameState;
        }

        private GameState CreateOldVersionGameState()
        {
            var gameState = CreateCompleteGameState();
            gameState.Version = 1; // 设置为旧版本
            
            // 移除一些在新版本中添加的技能
            if (gameState.Characters.Count > 0)
            {
                var character = gameState.Characters[0];
                character.Skills.SkillLevels.Remove("Research");
                character.Skills.SkillLevels.Remove("Combat");
                character.Skills.SkillExperience.Remove("Research");
                character.Skills.SkillExperience.Remove("Combat");
            }

            gameState.UpdateChecksum();
            return gameState;
        }
    }
}