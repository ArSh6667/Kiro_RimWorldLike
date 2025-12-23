using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 地图生成器实现
    /// </summary>
    public class MapGenerator : IMapGenerator
    {
        private readonly INoiseGenerator _noiseGenerator;
        private readonly ITerrainGenerator _terrainGenerator;
        private int _currentSeed;

        public MapGenerator(INoiseGenerator noiseGenerator = null, ITerrainGenerator terrainGenerator = null)
        {
            _noiseGenerator = noiseGenerator ?? new PerlinNoiseGenerator();
            _terrainGenerator = terrainGenerator ?? new TerrainGenerator();
            _currentSeed = Environment.TickCount;
        }

        public void SetSeed(int seed)
        {
            _currentSeed = seed;
            _noiseGenerator.SetSeed(seed);
        }

        public GameMap GenerateMap(MapGenerationConfig config)
        {
            // 设置种子
            if (config.Seed != 0)
            {
                SetSeed(config.Seed);
            }
            else
            {
                SetSeed(_currentSeed);
                config.Seed = _currentSeed;
            }

            // 创建地图对象
            var map = new GameMap(config.Width, config.Height, config.Seed);

            // 生成高度图
            var heightMap = _noiseGenerator.GenerateNoise(config.Width, config.Height, config.NoiseConfig);
            
            // 复制高度图到地图
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    map.HeightMap[x, y] = heightMap[x, y];
                }
            }

            // 生成地形
            var terrain = _terrainGenerator.GenerateTerrain(heightMap, config.TerrainConfig);
            
            // 复制地形到地图
            for (int x = 0; x < config.Width; x++)
            {
                for (int y = 0; y < config.Height; y++)
                {
                    map.Terrain[x, y] = terrain[x, y];
                }
            }

            // 放置资源
            _terrainGenerator.PlaceResources(map, config.ResourceConfig);

            // 验证地图（如果启用）
            if (config.EnableConnectivityValidation && !ValidateMap(map))
            {
                // 如果验证失败，尝试重新生成
                return RegenerateMap(config);
            }

            return map;
        }
        public bool ValidateMap(GameMap map)
        {
            // 验证地形多样性
            if (!ValidateTerrainDiversity(map))
                return false;

            // 验证连通性
            if (!ValidateConnectivity(map))
                return false;

            // 验证资源分布
            if (!ValidateResourceDistribution(map))
                return false;

            return true;
        }

        private bool ValidateTerrainDiversity(GameMap map)
        {
            var terrainCounts = new Dictionary<TerrainType, int>();
            int totalCells = map.Width * map.Height;

            // 统计各种地形的数量
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var terrain = map.GetTerrain(x, y);
                    terrainCounts[terrain] = terrainCounts.GetValueOrDefault(terrain, 0) + 1;
                }
            }

            // 检查是否至少有3种不同的地形类型
            if (terrainCounts.Count < 3)
                return false;

            // 检查每种地形是否占总面积的5%-60%
            foreach (var count in terrainCounts.Values)
            {
                float ratio = (float)count / totalCells;
                if (ratio < 0.05f || ratio > 0.6f)
                    return false;
            }

            return true;
        }

        private bool ValidateConnectivity(GameMap map)
        {
            // 使用洪水填充算法检查连通性
            var visited = new bool[map.Width, map.Height];
            var walkableCells = 0;
            var connectedCells = 0;

            // 统计可行走单元格数量
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map.IsWalkable(x, y))
                        walkableCells++;
                }
            }

            // 找到第一个可行走的单元格作为起点
            for (int x = 0; x < map.Width && connectedCells == 0; x++)
            {
                for (int y = 0; y < map.Height && connectedCells == 0; y++)
                {
                    if (map.IsWalkable(x, y) && !visited[x, y])
                    {
                        connectedCells = FloodFill(map, visited, x, y);
                    }
                }
            }

            // 检查连通的单元格是否占可行走单元格的大部分
            return connectedCells >= walkableCells * 0.9f;
        }
        private int FloodFill(GameMap map, bool[,] visited, int startX, int startY)
        {
            var stack = new Stack<(int x, int y)>();
            stack.Push((startX, startY));
            int count = 0;

            while (stack.Count > 0)
            {
                var (x, y) = stack.Pop();

                if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
                    continue;

                if (visited[x, y] || !map.IsWalkable(x, y))
                    continue;

                visited[x, y] = true;
                count++;

                // 添加相邻单元格
                stack.Push((x + 1, y));
                stack.Push((x - 1, y));
                stack.Push((x, y + 1));
                stack.Push((x, y - 1));
            }

            return count;
        }

        private bool ValidateResourceDistribution(GameMap map)
        {
            if (map.Resources.Count < 2)
                return true; // 资源太少，无需验证分布

            // 检查资源点之间的最小距离
            for (int i = 0; i < map.Resources.Count; i++)
            {
                for (int j = i + 1; j < map.Resources.Count; j++)
                {
                    var distance = Vector3.Distance(map.Resources[i].Position, map.Resources[j].Position);
                    if (distance < 3.0f) // 最小距离阈值
                        return false;
                }
            }

            return true;
        }

        private GameMap RegenerateMap(MapGenerationConfig config)
        {
            // 使用新的随机种子重新生成
            var newSeed = _currentSeed + 1;
            SetSeed(newSeed);
            config.Seed = newSeed;

            return GenerateMap(config);
        }
    }
}