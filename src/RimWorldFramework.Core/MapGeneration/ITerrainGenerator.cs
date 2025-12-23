using System;

namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 地形生成器接口
    /// </summary>
    public interface ITerrainGenerator
    {
        /// <summary>
        /// 根据高度图生成地形类型
        /// </summary>
        /// <param name="heightMap">高度图</param>
        /// <param name="config">地形配置</param>
        /// <returns>地形类型数组</returns>
        TerrainType[,] GenerateTerrain(float[,] heightMap, TerrainConfig config);

        /// <summary>
        /// 在地图上放置资源
        /// </summary>
        /// <param name="map">游戏地图</param>
        /// <param name="config">资源配置</param>
        void PlaceResources(GameMap map, ResourceConfig config);
    }

    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainType
    {
        Water,      // 水域
        Sand,       // 沙地
        Grass,      // 草地
        Forest,     // 森林
        Mountain,   // 山地
        Rock        // 岩石
    }

    /// <summary>
    /// 地形生成配置
    /// </summary>
    public class TerrainConfig
    {
        /// <summary>
        /// 水域高度阈值
        /// </summary>
        public float WaterThreshold { get; set; } = 0.3f;

        /// <summary>
        /// 沙地高度阈值
        /// </summary>
        public float SandThreshold { get; set; } = 0.4f;

        /// <summary>
        /// 草地高度阈值
        /// </summary>
        public float GrassThreshold { get; set; } = 0.6f;

        /// <summary>
        /// 森林高度阈值
        /// </summary>
        public float ForestThreshold { get; set; } = 0.75f;

        /// <summary>
        /// 山地高度阈值
        /// </summary>
        public float MountainThreshold { get; set; } = 0.9f;
    }
}