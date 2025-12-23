namespace RimWorldFramework.Core.Systems
{
    /// <summary>
    /// 游戏系统接口
    /// </summary>
    public interface IGameSystem
    {
        /// <summary>
        /// 系统优先级，数值越小优先级越高
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 系统名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 系统是否已初始化
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 初始化系统
        /// </summary>
        void Initialize();

        /// <summary>
        /// 更新系统
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 关闭系统
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// 基础游戏系统抽象类
    /// </summary>
    public abstract class GameSystem : IGameSystem
    {
        public abstract int Priority { get; }
        public abstract string Name { get; }
        public bool IsInitialized { get; private set; }

        public virtual void Initialize()
        {
            if (IsInitialized)
                return;

            OnInitialize();
            IsInitialized = true;
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsInitialized)
                return;

            OnUpdate(deltaTime);
        }

        public virtual void Shutdown()
        {
            if (!IsInitialized)
                return;

            OnShutdown();
            IsInitialized = false;
        }

        protected abstract void OnInitialize();
        protected abstract void OnUpdate(float deltaTime);
        protected abstract void OnShutdown();
    }
}