using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.Configuration;

namespace RimWorldFramework.Core
{
    /// <summary>
    /// 游戏框架主接口
    /// </summary>
    public interface IGameFramework
    {
        /// <summary>
        /// 框架是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 框架是否正在运行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 初始化框架
        /// </summary>
        void Initialize(GameConfig config);

        /// <summary>
        /// 更新框架
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 关闭框架
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 获取系统
        /// </summary>
        T? GetSystem<T>() where T : class, IGameSystem;

        /// <summary>
        /// 注册系统
        /// </summary>
        void RegisterSystem<T>(T system) where T : class, IGameSystem;

        /// <summary>
        /// 移除系统
        /// </summary>
        void UnregisterSystem<T>() where T : class, IGameSystem;

        /// <summary>
        /// 检查系统是否已注册
        /// </summary>
        bool HasSystem<T>() where T : class, IGameSystem;
    }
}