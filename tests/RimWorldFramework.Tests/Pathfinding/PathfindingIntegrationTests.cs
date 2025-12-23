using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Pathfinding
{
    /// <summary>
    /// 路径寻找系统集成测试
    /// </summary>
    [TestFixture]
    public class PathfindingIntegrationTests
    {
        private PathfindingGrid _grid = null!;
        private AStarPathfinder _pathfinder = null!;

        [SetUp]
        public void Setup()
        {
            _grid = new PathfindingGrid(10, 10);
            _pathfinder = new AStarPathfinder(_grid);
        }

        [Test]
        public void FindPath_SimpleCase_ReturnsValidPath()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(5, 5, 0);

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Path, Is.Not.Empty);
            Assert.That(result.Path.First(), Is.EqualTo(new Vector3(0.5f, 0.5f, 0f)).Within(0.1f));
            Assert.That(result.Path.Last(), Is.EqualTo(new Vector3(5.5f, 5.5f, 0f)).Within(0.1f));
            Assert.That(result.TotalCost, Is.GreaterThan(0));
        }

        [Test]
        public void FindPath_WithObstacles_FindsAlternativePath()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(2, 0, 0);

            // 在直线路径上放置障碍物
            _grid.SetTerrainType(1, 0, TerrainType.Blocked);

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Path.Count, Is.GreaterThan(3)); // 需要绕路，路径更长
            
            // 验证路径不经过障碍物
            foreach (var point in result.Path)
            {
                var gridPos = _grid.WorldToGrid(point);
                var node = _grid.GetNode(gridPos.x, gridPos.y);
                Assert.That(node?.IsWalkable(), Is.True);
            }
        }

        [Test]
        public void FindPath_NoPathAvailable_ReturnsFailure()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(2, 0, 0);

            // 完全阻塞路径
            for (int y = 0; y < _grid.Height; y++)
            {
                _grid.SetTerrainType(1, y, TerrainType.Blocked);
            }

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void FindPath_DifferentTerrainCosts_ChoosesOptimalPath()
        {
            // Arrange
            var start = new Vector3(0, 1, 0);
            var end = new Vector3(4, 1, 0);

            // 创建两条路径：一条通过困难地形，一条通过道路
            // 困难地形路径（直线）
            _grid.SetTerrainType(1, 1, TerrainType.Difficult);
            _grid.SetTerrainType(2, 1, TerrainType.Difficult);
            _grid.SetTerrainType(3, 1, TerrainType.Difficult);

            // 道路路径（绕路但更快）
            _grid.SetTerrainType(1, 0, TerrainType.Road);
            _grid.SetTerrainType(2, 0, TerrainType.Road);
            _grid.SetTerrainType(3, 0, TerrainType.Road);
            _grid.SetTerrainType(1, 2, TerrainType.Road);
            _grid.SetTerrainType(2, 2, TerrainType.Road);
            _grid.SetTerrainType(3, 2, TerrainType.Road);

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.True);
            
            // 验证路径选择了更优的道路
            bool usesRoad = result.Path.Any(p =>
            {
                var gridPos = _grid.WorldToGrid(p);
                var node = _grid.GetNode(gridPos.x, gridPos.y);
                return node?.TerrainType == TerrainType.Road;
            });
            
            Assert.That(usesRoad, Is.True, "路径应该选择代价更低的道路");
        }

        [Test]
        public void FindPath_DynamicObstacles_UpdatesPathCorrectly()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(3, 0, 0);

            // 第一次寻路（无障碍物）
            var initialResult = _pathfinder.FindPath(start, end);
            Assert.That(initialResult.Success, Is.True);

            // 添加动态障碍物
            _grid.SetDynamicObstacle(1, 0, true);
            _grid.SetDynamicObstacle(2, 0, true);

            // Act - 重新寻路
            var newResult = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(newResult.Success, Is.True);
            Assert.That(newResult.Path.Count, Is.GreaterThan(initialResult.Path.Count));
            
            // 验证新路径避开了动态障碍物
            foreach (var point in newResult.Path)
            {
                var gridPos = _grid.WorldToGrid(point);
                var node = _grid.GetNode(gridPos.x, gridPos.y);
                Assert.That(node?.HasDynamicObstacle, Is.False);
            }
        }

        [Test]
        public void SmoothPath_RemovesUnnecessaryWaypoints()
        {
            // Arrange
            var originalPath = new List<Vector3>
            {
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(1.5f, 0.5f, 0),
                new Vector3(2.5f, 0.5f, 0),
                new Vector3(3.5f, 0.5f, 0),
                new Vector3(4.5f, 0.5f, 0)
            };

            // Act
            var smoothedPath = _pathfinder.SmoothPath(originalPath);

            // Assert
            Assert.That(smoothedPath.Count, Is.LessThanOrEqualTo(originalPath.Count));
            Assert.That(smoothedPath.First(), Is.EqualTo(originalPath.First()));
            Assert.That(smoothedPath.Last(), Is.EqualTo(originalPath.Last()));
        }

        [Test]
        public void PathfindingConfig_AffectsSearchBehavior()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(5, 5, 0);

            var configNoDiagonal = new PathfindingConfig
            {
                AllowDiagonalMovement = false
            };

            var configWithDiagonal = new PathfindingConfig
            {
                AllowDiagonalMovement = true
            };

            var pathfinderNoDiagonal = new AStarPathfinder(_grid, configNoDiagonal);
            var pathfinderWithDiagonal = new AStarPathfinder(_grid, configWithDiagonal);

            // Act
            var resultNoDiagonal = pathfinderNoDiagonal.FindPath(start, end);
            var resultWithDiagonal = pathfinderWithDiagonal.FindPath(start, end);

            // Assert
            Assert.That(resultNoDiagonal.Success, Is.True);
            Assert.That(resultWithDiagonal.Success, Is.True);
            
            // 允许对角线移动的路径应该更短
            Assert.That(resultWithDiagonal.Path.Count, Is.LessThanOrEqualTo(resultNoDiagonal.Path.Count));
        }

        [Test]
        public void GridStats_ReflectsGridState()
        {
            // Arrange
            _grid.SetTerrainType(0, 0, TerrainType.Blocked);
            _grid.SetTerrainType(1, 1, TerrainType.Blocked);
            _grid.SetDynamicObstacle(2, 2, true);

            // Act
            var stats = _grid.GetStats();

            // Assert
            Assert.That(stats.Width, Is.EqualTo(10));
            Assert.That(stats.Height, Is.EqualTo(10));
            Assert.That(stats.TotalNodes, Is.EqualTo(100));
            Assert.That(stats.BlockedNodes, Is.EqualTo(2));
            Assert.That(stats.DynamicObstacles, Is.EqualTo(1));
            Assert.That(stats.WalkableNodes, Is.EqualTo(97));
        }

        [Test]
        public void CreateTestGrid_GeneratesValidGrid()
        {
            // Act
            var testGrid = PathfindingGrid.CreateTestGrid(15, 15, new Random(42));

            // Assert
            Assert.That(testGrid.Width, Is.EqualTo(15));
            Assert.That(testGrid.Height, Is.EqualTo(15));

            var stats = testGrid.GetStats();
            Assert.That(stats.BlockedNodes, Is.GreaterThan(0));
            Assert.That(stats.WalkableNodes, Is.GreaterThan(stats.BlockedNodes));
        }

        [Test]
        public void PathfindingResult_ContainsValidMetrics()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(3, 3, 0);

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TotalCost, Is.GreaterThan(0));
            Assert.That(result.NodesExplored, Is.GreaterThan(0));
            Assert.That(result.SearchTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void FindPath_InvalidPositions_HandlesGracefully()
        {
            // Test cases for invalid positions
            var testCases = new[]
            {
                (new Vector3(-1, 0, 0), new Vector3(5, 5, 0), "起点超出网格范围"),
                (new Vector3(0, 0, 0), new Vector3(15, 15, 0), "终点超出网格范围"),
            };

            foreach (var (start, end, expectedError) in testCases)
            {
                // Act
                var result = _pathfinder.FindPath(start, end);

                // Assert
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain(expectedError));
            }
        }

        [Test]
        public void FindPath_StartEqualsEnd_ReturnsDirectPath()
        {
            // Arrange
            var position = new Vector3(2, 2, 0);

            // Act
            var result = _pathfinder.FindPath(position, position);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Path.Count, Is.EqualTo(2));
            Assert.That(result.TotalCost, Is.EqualTo(0));
        }

        [Test]
        public void FindPath_BlockedStartOrEnd_ReturnsFailure()
        {
            // Arrange
            var start = new Vector3(0, 0, 0);
            var end = new Vector3(5, 5, 0);

            // Block start position
            _grid.SetTerrainType(0, 0, TerrainType.Blocked);

            // Act
            var result = _pathfinder.FindPath(start, end);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("起点不可通行"));
        }
    }
}