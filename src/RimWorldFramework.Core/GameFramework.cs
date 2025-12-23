using System;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core.Configuration;
using RimWorldFramework.Core.Events;
using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Serialization;
using RimWorldFramework.Core.Mods;
using RimWorldFramework.Core.Performance;

namespace RimWorldFramework.Core
{
    /// <summary>
    /// 游戏框架主类实现
    /// </summary>
    public class GameFramework : IGameFramework, IDisposable
    {
        private readonly ISystemManager _systemManager;
        private readonly IEventBus _eventBus;
        private readonly IEntityManager _entityManager;
        private readonly ILogger<GameFramework> _logger;
        
        private GameConfig? _config;
        private bool _isInitialized;
        private bool _isRunning;
        private bool _disposed;

        public bool IsInitialized => _isInitialized;
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameFramework(ILogger<GameFramework>? logger = null)
        {
            _logger = logger ?? CreateDefaultLogger();
            _systemManager = new SystemManager(_logger as ILogger<SystemManager>);
            _eventBus = new EventBus();
            _entityManager = new EntityManager();

            // 订阅事件总线的错误事件
            _eventBus.EventHandlingError += OnEventHandlingError;

            _logger.LogDebug("GameFramework created");
        }

        /// <summary>
        /// 初始化框架
        /// </summary>
        public void Initialize(GameConfig config)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            if (_isInitialized)
            {
                _logger.LogWarning("GameFramework is already initialized");
                return;
            }

            try
            {
                _config = config ?? throw new ArgumentNullException(nameof(config));
                
                _logger.LogInformation("Initializing GameFramework");

                // 发布框架初始化开始事件
                _eventBus.Publish(new FrameworkInitializationStartedEvent());

                // 注册核心系统
                RegisterCoreSystems();

                // 初始化所有系统
                _systemManager.InitializeAllSystems();

                _isInitialized = true;
                _isRunning = true;

                // 发布框架初始化完成事件
                _eventBus.Publish(new FrameworkInitializedEvent(_config));

                _logger.LogInformation("GameFramework initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize GameFramework");
                
                // 发布初始化失败事件
                _eventBus.Publish(new FrameworkInitializationFailedEvent(ex));
                
                // 清理已初始化的资源
                try
                {
                    _systemManager.ShutdownAllSystems();
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error during cleanup after initialization failure");
                }

                throw;
            }
        }

        /// <summary>
        /// 更新框架
        /// </summary>
        public void Update(float deltaTime)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            if (!_isInitialized || !_isRunning)
                return;

            try
            {
                // 发布帧开始事件
                _eventBus.Publish(new FrameStartEvent(deltaTime));

                // 更新所有系统
                _systemManager.UpdateAllSystems(deltaTime);

                // 发布帧结束事件
                _eventBus.Publish(new FrameEndEvent(deltaTime));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during framework update");
                
                // 发布更新错误事件
                _eventBus.Publish(new FrameworkUpdateErrorEvent(ex, deltaTime));
                
                // 框架继续运行，不因单次更新错误而停止
            }
        }

        /// <summary>
        /// 关闭框架
        /// </summary>
        public void Shutdown()
        {
            if (_disposed || !_isInitialized)
                return;

            try
            {
                _logger.LogInformation("Shutting down GameFramework");

                _isRunning = false;

                // 发布框架关闭开始事件
                _eventBus.Publish(new FrameworkShutdownStartedEvent());

                // 关闭所有系统
                _systemManager.ShutdownAllSystems();

                // 发布框架关闭完成事件
                _eventBus.Publish(new FrameworkShutdownCompletedEvent());

                _isInitialized = false;

                _logger.LogInformation("GameFramework shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during framework shutdown");
                
                // 发布关闭错误事件
                _eventBus.Publish(new FrameworkShutdownErrorEvent(ex));
            }
        }

        /// <summary>
        /// 获取系统
        /// </summary>
        public T? GetSystem<T>() where T : class, IGameSystem
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            return _systemManager.GetSystem<T>();
        }

        /// <summary>
        /// 注册系统
        /// </summary>
        public void RegisterSystem<T>(T system) where T : class, IGameSystem
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            if (system == null)
                throw new ArgumentNullException(nameof(system));

            _systemManager.RegisterSystem(system);
            _logger.LogDebug("Registered system {SystemType}", typeof(T).Name);
        }

        /// <summary>
        /// 移除系统
        /// </summary>
        public void UnregisterSystem<T>() where T : class, IGameSystem
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            _systemManager.UnregisterSystem<T>();
            _logger.LogDebug("Unregistered system {SystemType}", typeof(T).Name);
        }

        /// <summary>
        /// 检查系统是否已注册
        /// </summary>
        public bool HasSystem<T>() where T : class, IGameSystem
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            return _systemManager.HasSystem<T>();
        }

        /// <summary>
        /// 获取事件总线
        /// </summary>
        public IEventBus GetEventBus()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            return _eventBus;
        }

        /// <summary>
        /// 获取实体管理器
        /// </summary>
        public IEntityManager GetEntityManager()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameFramework));

            return _entityManager;
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public GameConfig? GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// 注册核心系统
        /// </summary>
        private void RegisterCoreSystems()
        {
            // 注册所有核心系统
            try
            {
                // ECS核心系统
                RegisterSystem(new ComponentSystem());
                
                // 角色系统
                RegisterSystem(new CharacterSystem(_entityManager, _eventBus, _logger as ILogger<CharacterSystem>));
                RegisterSystem(new StateUpdateSystem(_entityManager, _eventBus, _logger as ILogger<StateUpdateSystem>));
                
                // 任务系统
                RegisterSystem(new TaskManager(_entityManager, _eventBus, _logger as ILogger<TaskManager>));
                RegisterSystem(new CollaborationManager(_entityManager, _eventBus, _logger as ILogger<CollaborationManager>));
                
                // 路径寻找系统
                RegisterSystem(new PathfindingSystem(_entityManager, _eventBus, _logger as ILogger<PathfindingSystem>));
                
                // 地图生成系统
                RegisterSystem(new MapGenerationSystem(_eventBus, _logger as ILogger<MapGenerationSystem>));
                
                // 序列化系统
                RegisterSystem(new SerializationSystem(_entityManager, _eventBus, _logger as ILogger<SerializationSystem>));
                
                // 模组系统
                RegisterSystem(new ModManager(_eventBus, _logger as ILogger<ModManager>));
                
                // 性能管理系统
                RegisterSystem(new ResourceManager(_logger as ILogger<ResourceManager>));
                RegisterSystem(new PerformanceMonitor(_eventBus, _logger as ILogger<PerformanceMonitor>));
                
                // 游戏进度系统
                RegisterSystem(new GameProgressSystem(_entityManager, _eventBus, _logger as ILogger<GameProgressSystem>));
                
                _logger.LogInformation("All core systems registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register core systems");
                throw;
            }
        }

        /// <summary>
        /// 事件处理错误回调
        /// </summary>
        private void OnEventHandlingError(Type eventType, object handler, Exception exception)
        {
            _logger.LogError(exception, 
                "Error handling event {EventType} with handler {HandlerType}", 
                eventType.Name, handler.GetType().Name);
        }

        /// <summary>
        /// 创建默认日志记录器
        /// </summary>
        private static ILogger<GameFramework> CreateDefaultLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            });
            return loggerFactory.CreateLogger<GameFramework>();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                Shutdown();
                _eventBus.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GameFramework disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #region 框架事件定义

    /// <summary>
    /// 框架初始化开始事件
    /// </summary>
    public class FrameworkInitializationStartedEvent : GameEvent
    {
    }

    /// <summary>
    /// 框架初始化完成事件
    /// </summary>
    public class FrameworkInitializedEvent : GameEvent
    {
        public GameConfig Config { get; }

        public FrameworkInitializedEvent(GameConfig config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }
    }

    /// <summary>
    /// 框架初始化失败事件
    /// </summary>
    public class FrameworkInitializationFailedEvent : GameEvent
    {
        public Exception Exception { get; }

        public FrameworkInitializationFailedEvent(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    /// <summary>
    /// 帧开始事件
    /// </summary>
    public class FrameStartEvent : GameEvent
    {
        public float DeltaTime { get; }

        public FrameStartEvent(float deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }

    /// <summary>
    /// 帧结束事件
    /// </summary>
    public class FrameEndEvent : GameEvent
    {
        public float DeltaTime { get; }

        public FrameEndEvent(float deltaTime)
        {
            DeltaTime = deltaTime;
        }
    }

    /// <summary>
    /// 框架更新错误事件
    /// </summary>
    public class FrameworkUpdateErrorEvent : GameEvent
    {
        public Exception Exception { get; }
        public float DeltaTime { get; }

        public FrameworkUpdateErrorEvent(Exception exception, float deltaTime)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            DeltaTime = deltaTime;
        }
    }

    /// <summary>
    /// 框架关闭开始事件
    /// </summary>
    public class FrameworkShutdownStartedEvent : GameEvent
    {
    }

    /// <summary>
    /// 框架关闭完成事件
    /// </summary>
    public class FrameworkShutdownCompletedEvent : GameEvent
    {
    }

    /// <summary>
    /// 框架关闭错误事件
    /// </summary>
    public class FrameworkShutdownErrorEvent : GameEvent
    {
        public Exception Exception { get; }

        public FrameworkShutdownErrorEvent(Exception exception)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }
    }

    #endregion
}