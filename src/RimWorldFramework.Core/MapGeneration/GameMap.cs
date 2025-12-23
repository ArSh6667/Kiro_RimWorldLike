using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 游戏地图类
    /// </summary>
    public class GameMap
    {
        /// <summary>
        /// 地图宽度
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 地图高度
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 地形类型数组
        /// </summary>
        public TerrainType[,] Terrain { get; }

        /// <summary>
        /// 高度图
        /// </summary>
        public float[,] HeightMap { get; }

        /// <summary>
        /// 资源点列表
        /// </summary>
        public List<ResourcePoint> Resources { get; }

        /// <summary>
        /// 地图生成种子
        /// </summary>
        public int Seed { get; }

        public GameMap(int width, int height, int seed)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Terrain = new TerrainType[width, height];
            HeightMap = new float[width, height];
            Resources = new List<ResourcePoint>();
        }

        /// <summary>
        /// 获取指定位置的地形类型
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>地形类型</returns>
        public TerrainType GetTerrain(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return TerrainType.Rock; // 边界外视为岩石

            return Terrain[x, y];
        }

        /// <summary>
        /// 设置指定位置的地形类型
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="terrain">地形类型</param>
        public void SetTerrain(int x, int y, TerrainType terrain)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Terrain[x, y] = terrain;
            }
        }

        /// <summary>
        /// 检查指定位置是否可行走
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>是否可行走</returns>
        public bool IsWalkable(int x, int y)
        {
            var terrain = GetTerrain(x, y);
            return terrain != TerrainType.Water && terrain != TerrainType.Rock;
        }
    }
}