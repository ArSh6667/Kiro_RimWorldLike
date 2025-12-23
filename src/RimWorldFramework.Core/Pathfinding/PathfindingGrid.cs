using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Pathfinding
{
    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainType
    {
        Walkable,       // 可行走
        Blocked,        // 阻塞
        Difficult,      // 困难地形（移动慢）
        Water,          // 水域
        Swamp,          // 沼泽
        Mountain,       // 山地
        Road            // 道路（移动快）
    }

    /// <summary>
    /// 网格节点
    /// </summary>
    public class GridNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public TerrainType TerrainType { get; set; } = TerrainType.Walkable;
        public float MovementCost { get; set; } = 1.0f;
        public bool IsBlocked { get; set; } = false;
        public bool HasDynamicObstacle { get; set; } = false;

        // A*算法相关
        public float GCost { get; set; } = float.MaxValue; // 从起点到当前节点的实际代价
        public float HCost { get; set; } = 0f; // 从当前节点到终点的启发式代价
        public float FCost => GCost + HCost; // 总代价
        public GridNode? Parent { get; set; }

        public GridNode(int x, int y)
        {
            X = x;
            Y = y;
            UpdateMovementCost();
        }

        /// <summary>
        /// 根据地形类型更新移动代价
        /// </summary>
        public void UpdateMovementCost()
        {
            MovementCost = TerrainType switch
            {
                TerrainType.Walkable => 1.0f,
                TerrainType.Blocked => float.MaxValue,
                TerrainType.Difficult => 2.0f,
                TerrainType.Water => 3.0f,
                TerrainType.Swamp => 2.5f,
                TerrainType.Mountain => 4.0f,
                TerrainType.Road => 0.5f,
                _ => 1.0f
            };

            IsBlocked = TerrainType == TerrainType.Blocked || MovementCost >= float.MaxValue;
        }

        /// <summary>
        /// 检查节点是否可通行
        /// </summary>
        public bool IsWalkable()
        {
            return !IsBlocked && !HasDynamicObstacle && MovementCost < float.MaxValue;
        }

        /// <summary>
        /// 重置A*算法数据
        /// </summary>
        public void ResetPathfindingData()
        {
            GCost = float.MaxValue;
            HCost = 0f;
            Parent = null;
        }

        public override string ToString()
        {
            return $"({X}, {Y}) - {TerrainType} - Cost: {MovementCost}";
        }
    }

    /// <summary>
    /// 路径寻找网格
    /// </summary>
    public class PathfindingGrid
    {
        private readonly GridNode[,] _grid;
        private readonly int _width;
        private readonly int _height;

        public int Width => _width;
        public int Height => _height;

        public PathfindingGrid(int width, int height)
        {
            _width = width;
            _height = height;
            _grid = new GridNode[width, height];

            InitializeGrid();
        }

        /// <summary>
        /// 初始化网格
        /// </summary>
        private void InitializeGrid()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y] = new GridNode(x, y);
                }
            }
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        public GridNode? GetNode(int x, int y)
        {
            if (IsValidPosition(x, y))
                return _grid[x, y];
            return null;
        }

        /// <summary>
        /// 获取节点（使用Vector3）
        /// </summary>
        public GridNode? GetNode(Vector3 worldPosition)
        {
            var gridPos = WorldToGrid(worldPosition);
            return GetNode(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// 设置地形类型
        /// </summary>
        public void SetTerrainType(int x, int y, TerrainType terrainType)
        {
            var node = GetNode(x, y);
            if (node != null)
            {
                node.TerrainType = terrainType;
                node.UpdateMovementCost();
            }
        }

        /// <summary>
        /// 设置动态障碍物
        /// </summary>
        public void SetDynamicObstacle(int x, int y, bool hasObstacle)
        {
            var node = GetNode(x, y);
            if (node != null)
            {
                node.HasDynamicObstacle = hasObstacle;
            }
        }

        /// <summary>
        /// 获取邻居节点
        /// </summary>
        public List<GridNode> GetNeighbors(GridNode node)
        {
            var neighbors = new List<GridNode>();

            // 8方向移动
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // 跳过自己

                    int x = node.X + dx;
                    int y = node.Y + dy;

                    var neighbor = GetNode(x, y);
                    if (neighbor != null && neighbor.IsWalkable())
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 获取4方向邻居（不包括对角线）
        /// </summary>
        public List<GridNode> GetCardinalNeighbors(GridNode node)
        {
            var neighbors = new List<GridNode>();
            var directions = new[] { (0, 1), (1, 0), (0, -1), (-1, 0) };

            foreach (var (dx, dy) in directions)
            {
                int x = node.X + dx;
                int y = node.Y + dy;

                var neighbor = GetNode(x, y);
                if (neighbor != null && neighbor.IsWalkable())
                {
                    neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// 计算两个节点之间的距离
        /// </summary>
        public float GetDistance(GridNode nodeA, GridNode nodeB)
        {
            int dx = Math.Abs(nodeA.X - nodeB.X);
            int dy = Math.Abs(nodeA.Y - nodeB.Y);

            // 使用对角线距离（允许8方向移动）
            if (dx > dy)
                return 1.4f * dy + 1.0f * (dx - dy); // 1.4f ≈ √2
            else
                return 1.4f * dx + 1.0f * (dy - dx);
        }

        /// <summary>
        /// 计算曼哈顿距离
        /// </summary>
        public float GetManhattanDistance(GridNode nodeA, GridNode nodeB)
        {
            return Math.Abs(nodeA.X - nodeB.X) + Math.Abs(nodeA.Y - nodeB.Y);
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public (int x, int y) WorldToGrid(Vector3 worldPosition)
        {
            return ((int)Math.Floor(worldPosition.X), (int)Math.Floor(worldPosition.Y));
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x + 0.5f, y + 0.5f, 0f); // 网格中心点
        }

        /// <summary>
        /// 检查位置是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        /// <summary>
        /// 检查世界位置是否有效
        /// </summary>
        public bool IsValidWorldPosition(Vector3 worldPosition)
        {
            var (x, y) = WorldToGrid(worldPosition);
            return IsValidPosition(x, y);
        }

        /// <summary>
        /// 重置所有节点的路径寻找数据
        /// </summary>
        public void ResetPathfindingData()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _grid[x, y].ResetPathfindingData();
                }
            }
        }

        /// <summary>
        /// 获取网格统计信息
        /// </summary>
        public GridStats GetStats()
        {
            int walkableCount = 0;
            int blockedCount = 0;
            int dynamicObstacleCount = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var node = _grid[x, y];
                    if (node.IsBlocked)
                        blockedCount++;
                    else if (node.HasDynamicObstacle)
                        dynamicObstacleCount++;
                    else
                        walkableCount++;
                }
            }

            return new GridStats
            {
                Width = _width,
                Height = _height,
                TotalNodes = _width * _height,
                WalkableNodes = walkableCount,
                BlockedNodes = blockedCount,
                DynamicObstacles = dynamicObstacleCount
            };
        }

        /// <summary>
        /// 创建测试网格
        /// </summary>
        public static PathfindingGrid CreateTestGrid(int width, int height, Random? random = null)
        {
            var grid = new PathfindingGrid(width, height);
            random ??= new Random();

            // 随机放置一些障碍物
            int obstacleCount = (width * height) / 10; // 10%的障碍物
            for (int i = 0; i < obstacleCount; i++)
            {
                int x = random.Next(width);
                int y = random.Next(height);
                grid.SetTerrainType(x, y, TerrainType.Blocked);
            }

            // 添加一些困难地形
            int difficultCount = (width * height) / 20; // 5%的困难地形
            for (int i = 0; i < difficultCount; i++)
            {
                int x = random.Next(width);
                int y = random.Next(height);
                if (grid.GetNode(x, y)?.TerrainType == TerrainType.Walkable)
                {
                    grid.SetTerrainType(x, y, TerrainType.Difficult);
                }
            }

            return grid;
        }
    }

    /// <summary>
    /// 网格统计信息
    /// </summary>
    public class GridStats
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TotalNodes { get; set; }
        public int WalkableNodes { get; set; }
        public int BlockedNodes { get; set; }
        public int DynamicObstacles { get; set; }

        public float WalkablePercentage => TotalNodes > 0 ? (float)WalkableNodes / TotalNodes * 100f : 0f;
        public float BlockedPercentage => TotalNodes > 0 ? (float)BlockedNodes / TotalNodes * 100f : 0f;

        public override string ToString()
        {
            return $"网格 {Width}x{Height}: 可行走 {WalkableNodes} ({WalkablePercentage:F1}%), " +
                   $"阻塞 {BlockedNodes} ({BlockedPercentage:F1}%), 动态障碍 {DynamicObstacles}";
        }
    }
}