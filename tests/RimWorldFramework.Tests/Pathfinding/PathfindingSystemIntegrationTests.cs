using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Pathfinding
{
    /// <summary>
    /// 路径寻找系统集成测试
    /// </summary>
    [TestFixture]
    public class PathfindingSystemIntegrationTests
    {
        private PathfindingSystem _pathfindingSystem = null!;
        private IEntityManager _entityManager = null!;
        private PathfindingGrid _grid = null!;

        [SetUp]
        public void Setup()
        {
            _entityManager = new EntityManager();
            _grid = new PathfindingGrid(10, 10);
            _pathfindingSystem = new PathfindingSystem(_entityManager, _grid);
            _pathfindingSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _pathfindingSystem.Shutdown();
        }

        [Test]
        public void RequestPath_ValidEntity_ProcessesSuccessfully()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            var start = new Vector3(0, 0, 0);
            var destination = new Vector3(5, 5, 0);

            // Act
            var result = _pathfindingSystem.RequestPath(entityId, start, destination);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_pathfindingSystem.IsPathfinding(entityId), Is.True);
        }

        [Test]
        public void RequestPath_EntityWithoutPositionComponent_ReturnsFalse()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var start = new Vector3(0, 0, 0);
            var destination = new Vector3(5, 5, 0);

            // Act
            var result = _pathfindingSystem.RequestPath(entityId, start, destination);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(_pathfindingSystem.IsPathfinding(entityId), Is.False);
        }

        [Test]
        public void Update_ProcessesPathfindingRequest()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            var start = new Vector3(0, 0, 0);
            var destination = new Vector3(3, 3, 0);

            _pathfindingSystem.RequestPath(entityId, start, destination);

            // Act
            _pathfindingSystem.Update(0.1f);

            // Assert
            var currentPath = _pathfindingSystem.GetCurrentPath(entityId);
            Assert.That(currentPath, Is.Not.Null);
            Assert.That(currentPath.Count, Is.GreaterThan(1));
            Assert.That(positionComponent.IsMoving, Is.True);
        }

        [Test]
        public void Update_CharacterMovesAlongPath()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0))
            {
                MovementSpeed = 2.0f
            };
            _entityManager.AddComponent(entityId, positionComponent);

            var start = new Vector3(0, 0, 0);
            var destination = new Vector3(2, 0, 0);

            _pathfindingSystem.RequestPath(entityId, start, destination);
            _pathfindingSystem.Update(0.1f); // 处理路径请求

            var initialPosition = positionComponent.Position;

            // Act
            _pathfindingSystem.Update(0.5f); // 移动角色

            // Assert
            Assert.That(positionComponent.Position, Is.Not.EqualTo(initialPosition));
            Assert.That(Vector3.Distance(positionComponent.Position, initialPosition), Is.GreaterThan(0));
        }

        [Test]
        public void CancelPathRequest_StopsCharacterMovement()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            _pathfindingSystem.RequestPath(entityId, new Vector3(0, 0, 0), new Vector3(5, 5, 0));
            _pathfindingSystem.Update(0.1f); // 处理路径请求

            // Act
            _pathfindingSystem.CancelPathRequest(entityId);

            // Assert
            Assert.That(_pathfindingSystem.IsPathfinding(entityId), Is.False);
            Assert.That(_pathfindingSystem.GetCurrentPath(entityId), Is.Null);
            Assert.That(positionComponent.IsMoving, Is.False);
        }

        [Test]
        public void SetDynamicObstacle_TriggersPathReplanning()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            _pathfindingSystem.RequestPath(entityId, new Vector3(0, 0, 0), new Vector3(3, 0, 0));
            _pathfindingSystem.Update(0.1f); // 处理初始路径请求

            var initialPath = _pathfindingSystem.GetCurrentPath(entityId);
            Assert.That(initialPath, Is.Not.Null);

            // Act - 在路径上添加障碍物
            _pathfindingSystem.SetDynamicObstacle(new Vector3(1, 0, 0), true);
            _pathfindingSystem.Update(0.1f); // 处理重新规划

            // Assert
            var newPath = _pathfindingSystem.GetCurrentPath(entityId);
            if (newPath != null)
            {
                // 新路径应该避开障碍物
                Assert.That(newPath.Any(p => Vector3.Distance(p, new Vector3(1.5f, 0.5f, 0)) < 0.1f), Is.False);
            }
        }

        [Test]
        public void SetTerrainType_AffectsPathfinding()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 1, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            // 设置困难地形
            _pathfindingSystem.SetTerrainType(new Vector3(1, 1, 0), TerrainType.Difficult);
            _pathfindingSystem.SetTerrainType(new Vector3(2, 1, 0), TerrainType.Difficult);

            // 设置道路
            _pathfindingSystem.SetTerrainType(new Vector3(1, 0, 0), TerrainType.Road);
            _pathfindingSystem.SetTerrainType(new Vector3(2, 0, 0), TerrainType.Road);
            _pathfindingSystem.SetTerrainType(new Vector3(1, 2, 0), TerrainType.Road);
            _pathfindingSystem.SetTerrainType(new Vector3(2, 2, 0), TerrainType.Road);

            // Act
            _pathfindingSystem.RequestPath(entityId, new Vector3(0, 1, 0), new Vector3(3, 1, 0));
            _pathfindingSystem.Update(0.1f);

            // Assert
            var path = _pathfindingSystem.GetCurrentPath(entityId);
            Assert.That(path, Is.Not.Null);

            // 路径应该倾向于使用道路而不是困难地形
            bool usesRoad = path.Any(p =>
            {
                var gridPos = _grid.WorldToGrid(p);
                var node = _grid.GetNode(gridPos.x, gridPos.y);
                return node?.TerrainType == TerrainType.Road;
            });

            Assert.That(usesRoad, Is.True, "路径应该选择代价更低的道路");
        }

        [Test]
        public void GetStats_ReturnsValidStatistics()
        {
            // Arrange
            var entityId1 = _entityManager.CreateEntity();
            var entityId2 = _entityManager.CreateEntity();
            
            _entityManager.AddComponent(entityId1, new PositionComponent(new Vector3(0, 0, 0)));
            _entityManager.AddComponent(entityId2, new PositionComponent(new Vector3(1, 1, 0)));

            _pathfindingSystem.RequestPath(entityId1, new Vector3(0, 0, 0), new Vector3(5, 5, 0));
            _pathfindingSystem.RequestPath(entityId2, new Vector3(1, 1, 0), new Vector3(6, 6, 0));

            // Act
            var stats = _pathfindingSystem.GetStats();

            // Assert
            Assert.That(stats.GridWidth, Is.EqualTo(10));
            Assert.That(stats.GridHeight, Is.EqualTo(10));
            Assert.That(stats.ActiveRequests, Is.GreaterThanOrEqualTo(0));
            Assert.That(stats.GridStats, Is.Not.Null);
        }

        [Test]
        public void PathfindingSystem_HandlesMultipleEntities()
        {
            // Arrange
            var entities = new uint[3];
            for (int i = 0; i < 3; i++)
            {
                entities[i] = _entityManager.CreateEntity();
                _entityManager.AddComponent(entities[i], new PositionComponent(new Vector3(i, 0, 0)));
            }

            // Act - 为所有实体请求路径
            for (int i = 0; i < 3; i++)
            {
                _pathfindingSystem.RequestPath(entities[i], new Vector3(i, 0, 0), new Vector3(i + 5, 5, 0));
            }

            _pathfindingSystem.Update(0.1f); // 处理所有请求

            // Assert
            for (int i = 0; i < 3; i++)
            {
                var path = _pathfindingSystem.GetCurrentPath(entities[i]);
                Assert.That(path, Is.Not.Null, $"实体 {entities[i]} 应该有路径");
                
                var positionComponent = _entityManager.GetComponent<PositionComponent>(entities[i]);
                Assert.That(positionComponent?.IsMoving, Is.True, $"实体 {entities[i]} 应该正在移动");
            }
        }

        [Test]
        public void PathfindingSystem_HandlesInvalidPath()
        {
            // Arrange
            var entityId = _entityManager.CreateEntity();
            var positionComponent = new PositionComponent(new Vector3(0, 0, 0));
            _entityManager.AddComponent(entityId, positionComponent);

            // 创建完全阻塞的路径
            for (int y = 0; y < _grid.Height; y++)
            {
                _pathfindingSystem.SetTerrainType(new Vector3(2, y, 0), TerrainType.Blocked);
            }

            // Act
            _pathfindingSystem.RequestPath(entityId, new Vector3(0, 0, 0), new Vector3(5, 0, 0));
            _pathfindingSystem.Update(0.1f);

            // Assert
            var path = _pathfindingSystem.GetCurrentPath(entityId);
            Assert.That(path, Is.Null, "应该无法找到路径");
            Assert.That(positionComponent.IsMoving, Is.False, "角色不应该移动");
        }
    }
}