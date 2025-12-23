using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Pathfinding
{
    /// <summary>
    /// 路径寻找结果
    /// </summary>
    public class PathfindingResult
    {
        public bool Success { get; set; }
        public List<Vector3> Path { get; set; } = new();
        public float TotalCost { get; set; }
        public int NodesExplored { get; set; }
        public TimeSpan SearchTime { get; set; }
        public string? ErrorMessage { get; set; }

        public static PathfindingResult Failure(string errorMessage)
        {
            return new PathfindingResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        public static PathfindingResult Success(List<Vector3> path, float totalCost, int nodesExplored, TimeSpan searchTime)
        {
            return new PathfindingResult
            {
                Success = true,
                Path = path,
                TotalCost = totalCost,
                NodesExplored = nodesExplored,
                SearchTime = searchTime
            };
        }
    }

    /// <summary>
    /// 路径寻找配置
    /// </summary>
    public class PathfindingConfig
    {
        public bool AllowDiagonalMovement { get; set; } = true;
        public float DiagonalCost { get; set; } = 1.4f; // √2
        public float StraightCost { get; set; } = 1.0f;
        public int MaxSearchNodes { get; set; } = 10000;
        public TimeSpan MaxSearchTime { get; set; } = TimeSpan.FromMilliseconds(100);
        public float HeuristicWeight { get; set; } = 1.0f; // A*启发式权重
        public bool UseJumpPointSearch { get; set; } = false; // 跳点搜索优化
    }

    /// <summary>
    /// A*路径寻找算法实现
    /// </summary>
    public class AStarPathfinder
    {
        private readonly PathfindingGrid _grid;
        private readonly PathfindingConfig _config;

        public AStarPathfinder(PathfindingGrid grid, PathfindingConfig? config = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _config = config ?? new PathfindingConfig();
        }

        /// <summary>
        /// 寻找路径
        /// </summary>
        public PathfindingResult FindPath(Vector3 start, Vector3 end)
        {
            var startTime = DateTime.Now;

            // 转换为网格坐标
            var startGrid = _grid.WorldToGrid(start);
            var endGrid = _grid.WorldToGrid(end);

            var startNode = _grid.GetNode(startGrid.x, startGrid.y);
            var endNode = _grid.GetNode(endGrid.x, endGrid.y);

            // 验证起点和终点
            if (startNode == null)
                return PathfindingResult.Failure("起点超出网格范围");

            if (endNode == null)
                return PathfindingResult.Failure("终点超出网格范围");

            if (!startNode.IsWalkable())
                return PathfindingResult.Failure("起点不可通行");

            if (!endNode.IsWalkable())
                return PathfindingResult.Failure("终点不可通行");

            if (startNode == endNode)
            {
                return PathfindingResult.Success(
                    new List<Vector3> { start, end },
                    0f,
                    0,
                    DateTime.Now - startTime
                );
            }

            // 重置网格数据
            _grid.ResetPathfindingData();

            // A*算法
            var openSet = new SortedSet<GridNode>(new NodeComparer());
            var closedSet = new HashSet<GridNode>();

            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristic(startNode, endNode);
            openSet.Add(startNode);

            int nodesExplored = 0;

            while (openSet.Count > 0)
            {
                // 检查搜索限制
                if (nodesExplored >= _config.MaxSearchNodes)
                    return PathfindingResult.Failure("超过最大搜索节点数");

                if (DateTime.Now - startTime > _config.MaxSearchTime)
                    return PathfindingResult.Failure("搜索超时");

                // 获取F值最小的节点
                var currentNode = openSet.Min!;
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                nodesExplored++;

                // 找到目标
                if (currentNode == endNode)
                {
                    var path = ReconstructPath(startNode, endNode);
                    var worldPath = ConvertToWorldPath(path);
                    var totalCost = endNode.GCost;
                    var searchTime = DateTime.Now - startTime;

                    return PathfindingResult.Success(worldPath, totalCost, nodesExplored, searchTime);
                }

                // 检查邻居
                var neighbors = _config.AllowDiagonalMovement 
                    ? _grid.GetNeighbors(currentNode)
                    : _grid.GetCardinalNeighbors(currentNode);

                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    var movementCost = CalculateMovementCost(currentNode, neighbor);
                    var tentativeGCost = currentNode.GCost + movementCost;

                    if (!openSet.Contains(neighbor))
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHeuristic(neighbor, endNode);
                        neighbor.Parent = currentNode;
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGCost < neighbor.GCost)
                    {
                        // 需要重新排序，先移除再添加
                        openSet.Remove(neighbor);
                        neighbor.GCost = tentativeGCost;
                        neighbor.Parent = currentNode;
                        openSet.Add(neighbor);
                    }
                }
            }

            return PathfindingResult.Failure("无法找到路径");
        }

        /// <summary>
        /// 计算启发式函数值
        /// </summary>
        private float CalculateHeuristic(GridNode from, GridNode to)
        {
            var distance = _config.AllowDiagonalMovement 
                ? _grid.GetDistance(from, to)
                : _grid.GetManhattanDistance(from, to);

            return distance * _config.HeuristicWeight;
        }

        /// <summary>
        /// 计算移动代价
        /// </summary>
        private float CalculateMovementCost(GridNode from, GridNode to)
        {
            var baseCost = to.MovementCost;

            // 对角线移动代价更高
            if (Math.Abs(from.X - to.X) == 1 && Math.Abs(from.Y - to.Y) == 1)
            {
                baseCost *= _config.DiagonalCost;
            }
            else
            {
                baseCost *= _config.StraightCost;
            }

            return baseCost;
        }

        /// <summary>
        /// 重建路径
        /// </summary>
        private List<GridNode> ReconstructPath(GridNode start, GridNode end)
        {
            var path = new List<GridNode>();
            var current = end;

            while (current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// 转换为世界坐标路径
        /// </summary>
        private List<Vector3> ConvertToWorldPath(List<GridNode> gridPath)
        {
            var worldPath = new List<Vector3>();

            foreach (var node in gridPath)
            {
                worldPath.Add(_grid.GridToWorld(node.X, node.Y));
            }

            return worldPath;
        }

        /// <summary>
        /// 平滑路径
        /// </summary>
        public List<Vector3> SmoothPath(List<Vector3> path)
        {
            if (path.Count <= 2) return path;

            var smoothedPath = new List<Vector3> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                var next = path[i + 1];

                // 检查是否可以直接从prev到next
                if (!HasLineOfSight(prev, next))
                {
                    smoothedPath.Add(current);
                }
            }

            smoothedPath.Add(path[path.Count - 1]);
            return smoothedPath;
        }

        /// <summary>
        /// 检查两点间是否有视线
        /// </summary>
        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            var fromGrid = _grid.WorldToGrid(from);
            var toGrid = _grid.WorldToGrid(to);

            // 使用Bresenham直线算法检查路径上的每个点
            var points = GetLinePoints(fromGrid.x, fromGrid.y, toGrid.x, toGrid.y);

            foreach (var (x, y) in points)
            {
                var node = _grid.GetNode(x, y);
                if (node == null || !node.IsWalkable())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取直线上的所有点（Bresenham算法）
        /// </summary>
        private List<(int x, int y)> GetLinePoints(int x0, int y0, int x1, int y1)
        {
            var points = new List<(int, int)>();
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0, y = y0;

            while (true)
            {
                points.Add((x, y));

                if (x == x1 && y == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return points;
        }

        /// <summary>
        /// 节点比较器（用于优先队列）
        /// </summary>
        private class NodeComparer : IComparer<GridNode>
        {
            public int Compare(GridNode? x, GridNode? y)
            {
                if (x == null || y == null) return 0;

                int result = x.FCost.CompareTo(y.FCost);
                if (result == 0)
                {
                    result = x.HCost.CompareTo(y.HCost);
                }
                if (result == 0)
                {
                    // 使用位置作为最后的比较标准，确保一致性
                    result = x.X.CompareTo(y.X);
                    if (result == 0)
                    {
                        result = x.Y.CompareTo(y.Y);
                    }
                }
                return result;
            }
        }
    }

    /// <summary>
    /// 路径寻找统计信息
    /// </summary>
    public class PathfindingStats
    {
        public int TotalSearches { get; set; }
        public int SuccessfulSearches { get; set; }
        public int FailedSearches { get; set; }
        public TimeSpan TotalSearchTime { get; set; }
        public int TotalNodesExplored { get; set; }
        public float AveragePathLength { get; set; }

        public float SuccessRate => TotalSearches > 0 ? (float)SuccessfulSearches / TotalSearches * 100f : 0f;
        public TimeSpan AverageSearchTime => TotalSearches > 0 ? 
            TimeSpan.FromMilliseconds(TotalSearchTime.TotalMilliseconds / TotalSearches) : TimeSpan.Zero;

        public override string ToString()
        {
            return $"路径搜索: {TotalSearches} 次, 成功率: {SuccessRate:F1}%, " +
                   $"平均时间: {AverageSearchTime.TotalMilliseconds:F1}ms, 平均路径长度: {AveragePathLength:F1}";
        }
    }
}