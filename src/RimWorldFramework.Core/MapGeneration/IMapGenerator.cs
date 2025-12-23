namespace RimWorldFramework.Core.MapGeneration
{
    /// <summary>
    /// 地图生成器接口
    /// </summary>
    public interface IMapGenerator
    {
        /// <summary>
        /// 生成游戏地图
        /// </summary>
        /// <param name="config">地图生成配置</param>
        /// <returns>生成的游戏地图</returns>
        GameMap GenerateMap(MapGenerationConfig config);

        /// <summary>
        /// 设置随机种子
        /// </summary>
        /// <param name="seed">随机种子</param>
        void SetSeed(int seed);

        /// <summary>
        /// 验证地图的有效性
        /// </summary>
        /// <param name="map">要验证的地图</param>
        /// <returns>地图是否有效</returns>
        bool ValidateMap(GameMap map);
    }

    /// <summary>
    /// 地图生成配置
    /// </summary>
    public class MapGenerationConfig
    {
        /// <summary>
        /// 地图宽度
        /// </summary>
        public int Width { get; set; } = 100;

        /// <summary>
        /// 地图高度
        /// </summary>
        public int Height { get; set; } = 100;

        /// <summary>
        /// 随机种子
        /// </summary>
        public int Seed { get; set; } = 0;

        /// <summary>
        /// 噪声配置
        /// </summary>
        public NoiseConfig NoiseConfig { get; set; } = new NoiseConfig();

        /// <summary>
        /// 地形配置
        /// </summary>
        public TerrainConfig TerrainConfig { get; set; } = new TerrainConfig();

        /// <summary>
        /// 资源配置
        /// </summary>
        public ResourceConfig ResourceConfig { get; set; } = new ResourceConfig();

        /// <summary>
        /// 是否启用连通性验证
        /// </summary>
        public bool EnableConnectivityValidation { get; set; } = true;

        /// <summary>
        /// 最小可行走区域比例
        /// </summary>
        public float MinWalkableAreaRatio { get; set; } = 0.4f;
    }
}