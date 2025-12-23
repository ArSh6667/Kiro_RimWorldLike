using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 地形生成器实现
    /// </summary>
    public class TerrainGenerator : ITerrainGenerator
    {
        private readonly Random _random;

        public TerrainGenerator(int seed = 0)
        {
            _random = new Random(seed);
        }

        public TerrainType[,] GenerateTerrain(float[,] heightMap, TerrainConfig config)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            var terrain = new TerrainType[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float heightValue = heightMap[x, y];
                    terrain[x, y] = ClassifyTerrain(heightValue, config);
                }
            }

            // 应用后处理以确保地形的连贯性
            ApplyTerrainSmoothing(terrain, width, height);

            return terrain;
        }

        public void PlaceResources(GameMap map, ResourceConfig config)
        {
            int totalResources = (int)(map.Width * map.Height * config.Density);
            var placedPositions = new List<Vector3>();

            for (int i = 0; i < totalResources; i++)
            {
                var position = FindValidResourcePosition(map, placedPositions, config.MinDistance);
                if (position != null)
                {
                    var resourceType = SelectResourceType(config.TypeWeights);
                    var amount = GenerateResourceAmount(resourceType, config.AmountRanges);
                    var quality = (float)_random.NextDouble();

                    var resource = new ResourcePoint(position.Value, resourceType, amount, quality);
                    map.Resources.Add(resource);
                    placedPositions.Add(position.Value);
                }
            }
        }

        private TerrainType ClassifyTerrain(float heightValue, TerrainConfig config)
        {
            if (heightValue < config.WaterThreshold)
                return TerrainType.Water;
            else if (heightValue < config.SandThreshold)
                return TerrainType.Sand;
            else if (heightValue < config.GrassThreshold)
                return TerrainType.Grass;
            else if (heightValue < config.ForestThreshold)
                return TerrainType.Forest;
            else if (heightValue < config.MountainThreshold)
                return TerrainType.Mountain;
            else
                return TerrainType.Rock;
        }
        private void ApplyTerrainSmoothing(TerrainType[,] terrain, int width, int height)
        {
            // 简单的地形平滑算法，减少孤立的地形块
            var smoothed = new TerrainType[width, height];
            Array.Copy(terrain, smoothed, terrain.Length);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    var neighbors = GetNeighborTerrains(terrain, x, y);
                    var dominantTerrain = GetDominantTerrain(neighbors);
                    
                    // 如果当前地形与周围主导地形差异很大，则进行平滑
                    if (ShouldSmooth(terrain[x, y], dominantTerrain))
                    {
                        smoothed[x, y] = dominantTerrain;
                    }
                }
            }

            Array.Copy(smoothed, terrain, terrain.Length);
        }

        private List<TerrainType> GetNeighborTerrains(TerrainType[,] terrain, int x, int y)
        {
            var neighbors = new List<TerrainType>();
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    neighbors.Add(terrain[x + dx, y + dy]);
                }
            }
            
            return neighbors;
        }

        private TerrainType GetDominantTerrain(List<TerrainType> terrains)
        {
            var counts = new Dictionary<TerrainType, int>();
            
            foreach (var terrain in terrains)
            {
                counts[terrain] = counts.GetValueOrDefault(terrain, 0) + 1;
            }
            
            var maxCount = 0;
            var dominantTerrain = TerrainType.Grass;
            
            foreach (var kvp in counts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    dominantTerrain = kvp.Key;
                }
            }
            
            return dominantTerrain;
        }

        private bool ShouldSmooth(TerrainType current, TerrainType dominant)
        {
            // 定义地形之间的兼容性
            var incompatiblePairs = new HashSet<(TerrainType, TerrainType)>
            {
                (TerrainType.Water, TerrainType.Mountain),
                (TerrainType.Water, TerrainType.Rock),
                (TerrainType.Sand, TerrainType.Forest),
                (TerrainType.Mountain, TerrainType.Water)
            };

            return incompatiblePairs.Contains((current, dominant)) || 
                   incompatiblePairs.Contains((dominant, current));
        }
        private Vector3? FindValidResourcePosition(GameMap map, List<Vector3> existingPositions, float minDistance)
        {
            const int maxAttempts = 100;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = _random.Next(map.Width);
                int y = _random.Next(map.Height);
                var position = new Vector3(x, y, 0);

                // 检查地形是否适合放置资源
                if (!IsValidResourceTerrain(map.GetTerrain(x, y)))
                    continue;

                // 检查与现有资源的距离
                bool validDistance = true;
                foreach (var existingPos in existingPositions)
                {
                    float distance = Vector3.Distance(position, existingPos);
                    if (distance < minDistance)
                    {
                        validDistance = false;
                        break;
                    }
                }

                if (validDistance)
                    return position;
            }

            return null; // 找不到合适位置
        }

        private bool IsValidResourceTerrain(TerrainType terrain)
        {
            // 水域和岩石不适合放置大部分资源
            return terrain != TerrainType.Water && terrain != TerrainType.Rock;
        }

        private ResourceType SelectResourceType(Dictionary<ResourceType, float> weights)
        {
            float totalWeight = 0f;
            foreach (var weight in weights.Values)
            {
                totalWeight += weight;
            }

            float randomValue = (float)_random.NextDouble() * totalWeight;
            float currentWeight = 0f;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            // 默认返回第一个类型
            return weights.Keys.First();
        }

        private int GenerateResourceAmount(ResourceType type, Dictionary<ResourceType, (int min, int max)> ranges)
        {
            if (ranges.TryGetValue(type, out var range))
            {
                return _random.Next(range.min, range.max + 1);
            }

            return 100; // 默认数量
        }
    }
}