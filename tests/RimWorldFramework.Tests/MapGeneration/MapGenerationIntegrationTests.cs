using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.MapGeneration
{
    [TestFixture]
    public class MapGenerationIntegrationTests
    {
        private IMapGenerator _mapGenerator;

        [SetUp]
        public void Setup()
        {
            _mapGenerator = new MapGenerator();
        }

        [Test]
        public void GenerateMap_WithDefaultConfig_ShouldCreateValidMap()
        {
            // Arrange
            var config = new MapGenerationConfig
            {
                Width = 100,
                Height = 100,
                Seed = 12345
            };

            // Act
            var map = _mapGenerator.GenerateMap(config);

            // Assert
            Assert.That(map, Is.Not.Null);
            Assert.That(map.Width, Is.EqualTo(100));
            Assert.That(map.Height, Is.EqualTo(100));
            Assert.That(map.Seed, Is.EqualTo(12345));
            Assert.That(map.Resources, Is.Not.Null);
            Assert.That(_mapGenerator.ValidateMap(map), Is.True);
        }

        [Test]
        public void GenerateMap_WithDifferentSeeds_ShouldProduceDifferentMaps()
        {
            // Arrange
            var config1 = new MapGenerationConfig { Width = 50, Height = 50, Seed = 1 };
            var config2 = new MapGenerationConfig { Width = 50, Height = 50, Seed = 2 };

            // Act
            var map1 = _mapGenerator.GenerateMap(config1);
            var map2 = _mapGenerator.GenerateMap(config2);

            // Assert
            bool mapsAreDifferent = false;
            for (int x = 0; x < 50 && !mapsAreDifferent; x++)
            {
                for (int y = 0; y < 50 && !mapsAreDifferent; y++)
                {
                    if (map1.GetTerrain(x, y) != map2.GetTerrain(x, y))
                    {
                        mapsAreDifferent = true;
                    }
                }
            }

            Assert.That(mapsAreDifferent, Is.True, "Maps with different seeds should be different");
        }

        [Test]
        public void GenerateMap_WithSameSeed_ShouldProduceIdenticalMaps()
        {
            // Arrange
            var config = new MapGenerationConfig { Width = 50, Height = 50, Seed = 42 };

            // Act
            var map1 = _mapGenerator.GenerateMap(config);
            var map2 = _mapGenerator.GenerateMap(config);

            // Assert
            for (int x = 0; x < 50; x++)
            {
                for (int y = 0; y < 50; y++)
                {
                    Assert.That(map1.GetTerrain(x, y), Is.EqualTo(map2.GetTerrain(x, y)),
                        $"Terrain at ({x}, {y}) should be identical");
                    Assert.That(map1.HeightMap[x, y], Is.EqualTo(map2.HeightMap[x, y]).Within(0.001f),
                        $"Height at ({x}, {y}) should be identical");
                }
            }
        }
        [Test]
        public void GenerateMap_WithResourceConfig_ShouldPlaceResourcesCorrectly()
        {
            // Arrange
            var config = new MapGenerationConfig
            {
                Width = 80,
                Height = 80,
                Seed = 999,
                ResourceConfig = new ResourceConfig
                {
                    Density = 0.02f,
                    MinDistance = 5.0f
                }
            };

            // Act
            var map = _mapGenerator.GenerateMap(config);

            // Assert
            Assert.That(map.Resources.Count, Is.GreaterThan(0));
            
            // 验证资源间距
            for (int i = 0; i < map.Resources.Count; i++)
            {
                for (int j = i + 1; j < map.Resources.Count; j++)
                {
                    var distance = Vector3.Distance(map.Resources[i].Position, map.Resources[j].Position);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(config.ResourceConfig.MinDistance),
                        $"Resources at {map.Resources[i].Position} and {map.Resources[j].Position} are too close");
                }
            }

            // 验证资源类型多样性
            var resourceTypes = map.Resources.Select(r => r.Type).Distinct().ToList();
            Assert.That(resourceTypes.Count, Is.GreaterThan(1), "Should have multiple resource types");
        }

        [Test]
        public void ValidateMap_WithValidMap_ShouldReturnTrue()
        {
            // Arrange
            var config = new MapGenerationConfig
            {
                Width = 60,
                Height = 60,
                Seed = 777,
                TerrainConfig = new TerrainConfig
                {
                    WaterThreshold = 0.2f,
                    MountainThreshold = 0.85f
                }
            };

            // Act
            var map = _mapGenerator.GenerateMap(config);
            var isValid = _mapGenerator.ValidateMap(map);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void GenerateMap_WithExtremeNoiseConfig_ShouldStillProduceValidMap()
        {
            // Arrange
            var config = new MapGenerationConfig
            {
                Width = 40,
                Height = 40,
                Seed = 555,
                NoiseConfig = new NoiseConfig
                {
                    Frequency = 0.3f,
                    Octaves = 8,
                    Persistence = 0.8f,
                    Lacunarity = 3.0f
                }
            };

            // Act
            var map = _mapGenerator.GenerateMap(config);

            // Assert
            Assert.That(map, Is.Not.Null);
            Assert.That(map.Width, Is.EqualTo(40));
            Assert.That(map.Height, Is.EqualTo(40));
            
            // 验证高度图值在合理范围内
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Assert.That(map.HeightMap[x, y], Is.InRange(0f, 1f),
                        $"Height value at ({x}, {y}) should be normalized");
                }
            }
        }

        [Test]
        public void MapGenerator_SetSeed_ShouldAffectSubsequentGeneration()
        {
            // Arrange
            var config = new MapGenerationConfig { Width = 30, Height = 30, Seed = 0 };

            // Act
            _mapGenerator.SetSeed(123);
            var map1 = _mapGenerator.GenerateMap(config);
            
            _mapGenerator.SetSeed(456);
            var map2 = _mapGenerator.GenerateMap(config);

            // Assert
            bool mapsAreDifferent = false;
            for (int x = 0; x < 30 && !mapsAreDifferent; x++)
            {
                for (int y = 0; y < 30 && !mapsAreDifferent; y++)
                {
                    if (Math.Abs(map1.HeightMap[x, y] - map2.HeightMap[x, y]) > 0.001f)
                    {
                        mapsAreDifferent = true;
                    }
                }
            }

            Assert.That(mapsAreDifferent, Is.True, "Maps should be different when using different seeds");
        }
    }
}