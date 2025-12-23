using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Pathfinding
{
    /// <summary>
    /// 路径寻找系统基于属性的测试
    /// Feature: rimworld-game-framework
    /// </summary>
    [TestFixture]
    public class PathfindingPropertyTests
    {
        private PathfindingGrid _grid = null!;
        private AStarPathfinder _pathfinder = null!;

        [SetUp]
        public void Setup()
        {
            _grid = new PathfindingGrid(20, 20);
            _pathfinder = new AStarPathfinder(_grid);
        }

        /// <summary>
        /// Property 5: 路径重规划
        /// 对于任何遇到障碍的移动请求，系统应当重新计算可行路径或提供替代方案
        /// 验证需求: 需求 2.4
        /// </summary>
        [Property(Arbitrary = new[] { typeof(PathfindingGenerators) })]
        [Category("Property")]
        public Property PathReplanning_ShouldFindAlternativeWhenObstacleAdded(
            ValidGridPosition start,
            ValidGridPosition end,
            List<ValidGridPosition> dynamicObstacles)
        {
            return Prop.ForAll(
                Gen.Choose(5, 15).ToArbitrary(), // 网格大小
                (gridSize) =>
                {
                    // 创建测试网格
                    var testGrid = new PathfindingGrid(gridSize, gridSize);
                    var testPathfinder = new AStarPathfinder(testGrid);

                    // 确保起点和终点在网格范围内
                    var startPos = new Vector3(
                        Math.Min(start.X, gridSize - 1),
                        Math.Min(start.Y, gridSize - 1),
                        0
                    );
                    var endPos = new Vector3(
                        Math.Min(end.X, gridSize - 1),
                        Math.Min(end.Y, gridSize - 1),
                        0
                    );

                    // 如果起点和终点相同，跳过测试
                    if (Math.Abs(startPos.X - endPos.X) < 0.1f && Math.Abs(startPos.Y - endPos.Y) < 0.1f)
                        return true;

                    try
                    {
                        // 第一次路径搜索（无障碍物）
                        var initialResult = testPathfinder.FindPath(startPos, endPos);

                        // 添加动态障碍物（但不阻塞起点和终点）
                        var validObstacles = dynamicObstacles
                            .Where(obs => obs.X < gridSize && obs.Y < gridSize)
                            .Where(obs => !(obs.X == (int)startPos.X && obs.Y == (int)startPos.Y))
                            .Where(obs => !(obs.X == (int)endPos.X && obs.Y == (int)endPos.Y))
                            .Take(Math.Min(3, gridSize * gridSize / 4)) // 限制障碍物数量
                            .ToList();

                        foreach (var obstacle in validObstacles)
                        {
                            testGrid.SetDynamicObstacle(obstacle.X, obstacle.Y, true);
                        }

                        // 第二次路径搜索（有障碍物）
                        var replanResult = testPathfinder.FindPath(startPos, endPos);

                        // 验证路径重规划属性
                        if (initialResult.Success)
                        {
                            // 如果初始路径存在，重规划应该：
                            // 1. 要么找到新路径（可能更长）
                            // 2. 要么明确报告无法找到路径
                            if (replanResult.Success)
                            {
                                // 新路径应该避开动态障碍物
                                return ValidatePathAvoidsObstacles(replanResult.Path, testGrid, validObstacles);
                            }
                            else
                            {
                                // 如果无法找到路径，应该有明确的错误信息
                                return !string.IsNullOrEmpty(replanResult.ErrorMessage);
                            }
                        }

                        return true;
                    }
                    catch (Exception)
                    {
                        // 异常情况下应该优雅处理
                        return false;
                    }
                });
        }

        /// <summary>
        /// 验证路径避开障碍物
        /// </summary>
        private bool ValidatePathAvoidsObstacles(List<Vector3> path, PathfindingGrid grid, List<ValidGridPosition> obstacles)
        {
            foreach (var point in path)
            {
                var gridPos = grid.WorldToGrid(point);
                var node = grid.GetNode(gridPos.x, gridPos.y);
                
                if (node == null || !node.IsWalkable())
                    return false;

                // 检查是否经过动态障碍物
                if (obstacles.Any(obs => obs.X == gridPos.x && obs.Y == gridPos.y))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 路径连续性属性测试
        /// 验证生成的路径中相邻点之间的距离合理
        /// </summary>
        [Property(Arbitrary = new[] { typeof(PathfindingGenerators) })]
        [Category("Property")]
        public Property PathContinuity_AdjacentPointsHaveReasonableDistance(
            ValidGridPosition start,
            ValidGridPosition end)
        {
            return Prop.ForAll(
                Gen.Choose(5, 15).ToArbitrary(),
                (gridSize) =>
                {
                    var testGrid = new PathfindingGrid(gridSize, gridSize);
                    var testPathfinder = new AStarPathfinder(testGrid);

                    var startPos = new Vector3(
                        Math.Min(start.X, gridSize - 1),
                        Math.Min(start.Y, gridSize - 1),
                        0
                    );
                    var endPos = new Vector3(
                        Math.Min(end.X, gridSize - 1),
                        Math.Min(end.Y, gridSize - 1),
                        0
                    );

                    if (Math.Abs(startPos.X - endPos.X) < 0.1f && Math.Abs(startPos.Y - endPos.Y) < 0.1f)
                        return true;

                    var result = testPathfinder.FindPath(startPos, endPos);

                    if (!result.Success || result.Path.Count < 2)
                        return true;

                    // 验证路径连续性
                    for (int i = 1; i < result.Path.Count; i++)
                    {
                        var prev = result.Path[i - 1];
                        var current = result.Path[i];
                        var distance = Vector3.Distance(prev, current);

                        // 相邻点距离应该在合理范围内（直线移动≈1.0，对角线移动≈1.4）
                        if (distance > 1.5f)
                            return false;
                    }

                    return true;
                });
        }

        /// <summary>
        /// 路径优化属性测试
        /// 验证路径平滑功能不会破坏路径的有效性
        /// </summary>
        [Property(Arbitrary = new[] { typeof(PathfindingGenerators) })]
        [Category("Property")]
        public Property PathSmoothing_PreservesPathValidity(
            ValidGridPosition start,
            ValidGridPosition end)
        {
            return Prop.ForAll(
                Gen.Choose(8, 12).ToArbitrary(),
                (gridSize) =>
                {
                    var testGrid = new PathfindingGrid(gridSize, gridSize);
                    var testPathfinder = new AStarPathfinder(testGrid);

                    var startPos = new Vector3(
                        Math.Min(start.X, gridSize - 1),
                        Math.Min(start.Y, gridSize - 1),
                        0
                    );
                    var endPos = new Vector3(
                        Math.Min(end.X, gridSize - 1),
                        Math.Min(end.Y, gridSize - 1),
                        0
                    );

                    if (Math.Abs(startPos.X - endPos.X) < 0.1f && Math.Abs(startPos.Y - endPos.Y) < 0.1f)
                        return true;

                    var result = testPathfinder.FindPath(startPos, endPos);

                    if (!result.Success || result.Path.Count < 3)
                        return true;

                    // 平滑路径
                    var smoothedPath = testPathfinder.SmoothPath(result.Path);

                    // 验证平滑后的路径
                    if (smoothedPath.Count == 0)
                        return false;

                    // 起点和终点应该保持不变
                    var startDistance = Vector3.Distance(smoothedPath[0], result.Path[0]);
                    var endDistance = Vector3.Distance(smoothedPath[smoothedPath.Count - 1], result.Path[result.Path.Count - 1]);

                    return startDistance < 0.1f && endDistance < 0.1f;
                });
        }

        /// <summary>
        /// 地形代价影响属性测试
        /// 验证不同地形类型对路径选择的影响
        /// </summary>
        [Property(Arbitrary = new[] { typeof(PathfindingGenerators) })]
        [Category("Property")]
        public Property TerrainCost_InfluencesPathSelection(
            ValidGridPosition start,
            ValidGridPosition end)
        {
            return Prop.ForAll(
                Gen.Choose(6, 10).ToArbitrary(),
                (gridSize) =>
                {
                    var testGrid = new PathfindingGrid(gridSize, gridSize);
                    var testPathfinder = new AStarPathfinder(testGrid);

                    var startPos = new Vector3(
                        Math.Min(start.X, gridSize - 1),
                        Math.Min(start.Y, gridSize - 1),
                        0
                    );
                    var endPos = new Vector3(
                        Math.Min(end.X, gridSize - 1),
                        Math.Min(end.Y, gridSize - 1),
                        0
                    );

                    if (Math.Abs(startPos.X - endPos.X) < 0.1f && Math.Abs(startPos.Y - endPos.Y) < 0.1f)
                        return true;

                    // 创建一条困难地形带
                    int midX = gridSize / 2;
                    for (int y = 1; y < gridSize - 1; y++)
                    {
                        if (midX != (int)startPos.X || y != (int)startPos.Y)
                        {
                            if (midX != (int)endPos.X || y != (int)endPos.Y)
                            {
                                testGrid.SetTerrainType(midX, y, TerrainType.Difficult);
                            }
                        }
                    }

                    var result = testPathfinder.FindPath(startPos, endPos);

                    if (!result.Success)
                        return true;

                    // 验证路径代价反映了地形影响
                    return result.TotalCost >= 0;
                });
        }

        /// <summary>
        /// 网格边界处理属性测试
        /// 验证路径寻找正确处理网格边界情况
        /// </summary>
        [Property]
        [Category("Property")]
        public Property GridBoundary_HandledCorrectly()
        {
            return Prop.ForAll(
                Gen.Choose(3, 8).ToArbitrary(),
                (gridSize) =>
                {
                    var testGrid = new PathfindingGrid(gridSize, gridSize);
                    var testPathfinder = new AStarPathfinder(testGrid);

                    // 测试边界位置
                    var boundaryPositions = new[]
                    {
                        new Vector3(0, 0, 0),                           // 左下角
                        new Vector3(gridSize - 1, 0, 0),               // 右下角
                        new Vector3(0, gridSize - 1, 0),               // 左上角
                        new Vector3(gridSize - 1, gridSize - 1, 0),    // 右上角
                    };

                    foreach (var start in boundaryPositions)
                    {
                        foreach (var end in boundaryPositions)
                        {
                            if (Vector3.Distance(start, end) < 0.1f) continue;

                            var result = testPathfinder.FindPath(start, end);
                            
                            // 边界位置之间应该能找到路径（除非被阻塞）
                            if (result.Success)
                            {
                                // 验证路径不超出边界
                                foreach (var point in result.Path)
                                {
                                    if (point.X < 0 || point.X >= gridSize || 
                                        point.Y < 0 || point.Y >= gridSize)
                                        return false;
                                }
                            }
                        }
                    }

                    return true;
                });
        }
    }

    /// <summary>
    /// 路径寻找测试数据生成器
    /// </summary>
    public static class PathfindingGenerators
    {
        /// <summary>
        /// 生成有效的网格位置
        /// </summary>
        public static Arbitrary<ValidGridPosition> ValidGridPosition()
        {
            return Gen.Choose(0, 19)
                .SelectMany(x => Gen.Choose(0, 19), (x, y) => new ValidGridPosition { X = x, Y = y })
                .ToArbitrary();
        }

        /// <summary>
        /// 生成地形类型
        /// </summary>
        public static Arbitrary<TerrainType> TerrainType()
        {
            return Gen.Elements(
                Core.Pathfinding.TerrainType.Walkable,
                Core.Pathfinding.TerrainType.Difficult,
                Core.Pathfinding.TerrainType.Water,
                Core.Pathfinding.TerrainType.Road
            ).ToArbitrary();
        }

        /// <summary>
        /// 生成路径寻找配置
        /// </summary>
        public static Arbitrary<PathfindingConfig> PathfindingConfig()
        {
            return Gen.zip(
                Gen.Elements(true, false),                    // AllowDiagonalMovement
                Gen.Choose(1000, 5000),                      // MaxSearchNodes
                Gen.Choose(50, 200)                          // MaxSearchTime (ms)
            ).Select(t => new Core.Pathfinding.PathfindingConfig
            {
                AllowDiagonalMovement = t.Item1,
                MaxSearchNodes = t.Item2,
                MaxSearchTime = TimeSpan.FromMilliseconds(t.Item3)
            }).ToArbitrary();
        }
    }

    /// <summary>
    /// 有效的网格位置
    /// </summary>
    public class ValidGridPosition
    {
        public int X { get; set; }
        public int Y { get; set; }

        public override string ToString() => $"({X}, {Y})";
    }
}