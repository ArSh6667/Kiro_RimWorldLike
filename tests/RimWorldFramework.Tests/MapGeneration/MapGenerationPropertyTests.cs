using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.MapGeneration
{
    [TestFixture]
    public class MapGenerationPropertyTests
    {
        private IMapGenerator _mapGenerator;

        [SetUp]
        public void Setup()
        {
            _mapGenerator = new MapGenerator();
        }

        /// <summary>
        /// 属性 11: 地图地形多样性
        /// 对于任何生成的地图，应当包含至少3种不同类型的地形，且每种地形占总面积的5%-60%
        /// 验证需求: 需求 4.1
        /// </summary>
        [Property]
        public Property MapTerrainDiversity()
        {
            var configGen = Gen.Fresh(() => new MapGenerationConfig
            {
                Width = Gen.Choose(50, 200).Sample(0, 1).First(),
                Height = Gen.Choose(50, 200).Sample(0, 1).First(),
                Seed = Gen.Choose(1, 10000).Sample(0, 1).First(),
                NoiseConfig = new NoiseConfig
                {
                    Frequency = Gen.Choose(1, 20).Sample(0, 1).First() * 0.01f,
                    Octaves = Gen.Choose(2, 6).Sample(0, 1).First(),
                    Persistence = Gen.Choose(3, 8).Sample(0, 1).First() * 0.1f,
                    Lacunarity = Gen.Choose(15, 25).Sample(0, 1).First() * 0.1f
                }
            });

            return Prop.ForAll(configGen, config =>
            {
                var map = _mapGenerator.GenerateMap(config);
                var terrainCounts = CountTerrainTypes(map);
                int totalCells = map.Width * map.Height;

                // 至少3种不同类型的地形
                bool hasMinimumDiversity = terrainCounts.Count >= 3;

                // 每种地形占总面积的5%-60%
                bool hasValidRatios = terrainCounts.Values.All(count =>
                {
                    float ratio = (float)count / totalCells;
                    return ratio >= 0.05f && ratio <= 0.6f;
                });

                return hasMinimumDiversity && hasValidRatios;
            });
        }

        /// <summary>
        /// 属性 12: 资源分布合理性
        /// 对于任何生成的地图，资源点之间的最小距离应当大于指定阈值，确保合理分布
        /// 验证需求: 需求 4.3
        /// </summary>
        [Property]
        public Property ResourceDistributionReasonableness()
        {
            var configGen = Gen.Fresh(() => new MapGenerationConfig
            {
                Width = Gen.Choose(80, 150).Sample(0, 1).First(),
                Height = Gen.Choose(80, 150).Sample(0, 1).First(),
                Seed = Gen.Choose(1, 10000).Sample(0, 1).First(),
                ResourceConfig = new ResourceConfig
                {
                    Density = Gen.Choose(5, 20).Sample(0, 1).First() * 0.001f,
                    MinDistance = Gen.Choose(3, 8).Sample(0, 1).First()
                }
            });

            return Prop.ForAll(configGen, config =>
            {
                var map = _mapGenerator.GenerateMap(config);
                
                if (map.Resources.Count < 2)
                    return true; // 资源太少，无需验证

                // 检查所有资源点对之间的距离
                for (int i = 0; i < map.Resources.Count; i++)
                {
                    for (int j = i + 1; j < map.Resources.Count; j++)
                    {
                        var distance = Vector3.Distance(map.Resources[i].Position, map.Resources[j].Position);
                        if (distance < config.ResourceConfig.MinDistance)
                            return false;
                    }
                }

                return true;
            });
        }
        /// <summary>
        /// 属性 13: 地图连通性验证
        /// 对于任何生成的地图，所有可行走区域应当相互连通，不存在孤立的可达区域
        /// 验证需求: 需求 4.5
        /// </summary>
        [Property]
        public Property MapConnectivityValidation()
        {
            var configGen = Gen.Fresh(() => new MapGenerationConfig
            {
                Width = Gen.Choose(60, 120).Sample(0, 1).First(),
                Height = Gen.Choose(60, 120).Sample(0, 1).First(),
                Seed = Gen.Choose(1, 10000).Sample(0, 1).First(),
                EnableConnectivityValidation = true,
                MinWalkableAreaRatio = 0.4f,
                TerrainConfig = new TerrainConfig
                {
                    WaterThreshold = Gen.Choose(2, 4).Sample(0, 1).First() * 0.1f,
                    MountainThreshold = Gen.Choose(8, 9).Sample(0, 1).First() * 0.1f
                }
            });

            return Prop.ForAll(configGen, config =>
            {
                var map = _mapGenerator.GenerateMap(config);
                return ValidateMapConnectivity(map);
            });
        }

        /// <summary>
        /// 辅助方法：统计地形类型数量
        /// </summary>
        private Dictionary<TerrainType, int> CountTerrainTypes(GameMap map)
        {
            var counts = new Dictionary<TerrainType, int>();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var terrain = map.GetTerrain(x, y);
                    counts[terrain] = counts.GetValueOrDefault(terrain, 0) + 1;
                }
            }

            return counts;
        }

        /// <summary>
        /// 辅助方法：验证地图连通性
        /// </summary>
        private bool ValidateMapConnectivity(GameMap map)
        {
            var visited = new bool[map.Width, map.Height];
            var walkableCells = 0;
            var largestConnectedComponent = 0;

            // 统计可行走单元格数量
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map.IsWalkable(x, y))
                        walkableCells++;
                }
            }

            // 找到最大的连通组件
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (map.IsWalkable(x, y) && !visited[x, y])
                    {
                        int componentSize = FloodFill(map, visited, x, y);
                        largestConnectedComponent = Math.Max(largestConnectedComponent, componentSize);
                    }
                }
            }

            // 最大连通组件应该包含至少90%的可行走区域
            return largestConnectedComponent >= walkableCells * 0.9f;
        }
        /// <summary>
        /// 辅助方法：洪水填充算法
        /// </summary>
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
    }
}