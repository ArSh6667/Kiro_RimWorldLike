using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RimWorldFramework.Core.Systems
{
    /// <summary>
    /// 系统管理器接口
    /// </summary>
    public interface ISystemManager
    {
        /// <summary>
        /// 注册系统
        /// </summary>
        void RegisterSystem<T>(T system) where T : class, IGameSystem;

        /// <summary>
        /// 移除系统
        /// </summary>
        void UnregisterSystem<T>() where T : class, IGameSystem;

        /// <summary>
        /// 获取系统
        /// </summary>
        T? GetSystem<T>() where T : class, IGameSystem;

        /// <summary>
        /// 检查系统是否已注册
        /// </summary>
        bool HasSystem<T>() where T : class, IGameSystem;

        /// <summary>
        /// 获取所有系统
        /// </summary>
        IEnumerable<IGameSystem> GetAllSystems();

        /// <summary>
        /// 初始化所有系统
        /// </summary>
        void InitializeAllSystems();

        /// <summary>
        /// 更新所有系统
        /// </summary>
        void UpdateAllSystems(float deltaTime);

        /// <summary>
        /// 关闭所有系统
        /// </summary>
        void ShutdownAllSystems();

        /// <summary>
        /// 系统数量
        /// </summary>
        int SystemCount { get; }
    }

    /// <summary>
    /// 系统管理器实现
    /// </summary>
    public class SystemManager : ISystemManager
    {
        private readonly Dictionary<Type, IGameSystem> _systems = new();
        private readonly List<IGameSystem> _sortedSystems = new();
        private readonly ILogger<SystemManager>? _logger;
        private readonly object _lock = new();
        private bool _isInitialized = false;

        public int SystemCount => _systems.Count;

        public SystemManager(ILogger<SystemManager>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// 注册系统
        /// </summary>
        public void RegisterSystem<T>(T system) where T : class, IGameSystem
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            lock (_lock)
            {
                var systemType = typeof(T);
                
                if (_systems.ContainsKey(systemType))
                {
                    _logger?.LogWarning("System {SystemType} is already registered, replacing it", systemType.Name);
                }

                _systems[systemType] = system;
                RebuildSortedSystemsList();
                
                _logger?.LogDebug("Registered system {SystemType} with priority {Priority}", 
                    systemType.Name, system.Priority);

                // 如果管理器已经初始化，立即初始化新系统
                if (_isInitialized && !system.IsInitialized)
                {
                    try
                    {
                        system.Initialize();
                        _logger?.LogDebug("Initialized system {SystemType}", systemType.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to initialize system {SystemType}", systemType.Name);
                    }
                }
            }
        }

        /// <summary>
        /// 移除系统
        /// </summary>
        public void UnregisterSystem<T>() where T : class, IGameSystem
        {
            lock (_lock)
            {
                var systemType = typeof(T);
                
                if (_systems.TryGetValue(systemType, out var system))
                {
                    try
                    {
                        if (system.IsInitialized)
                        {
                            system.Shutdown();
                            _logger?.LogDebug("Shutdown system {SystemType}", systemType.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error shutting down system {SystemType}", systemType.Name);
                    }

                    _systems.Remove(systemType);
                    RebuildSortedSystemsList();
                    
                    _logger?.LogDebug("Unregistered system {SystemType}", systemType.Name);
                }
            }
        }

        /// <summary>
        /// 获取系统
        /// </summary>
        public T? GetSystem<T>() where T : class, IGameSystem
        {
            lock (_lock)
            {
                return _systems.TryGetValue(typeof(T), out var system) ? system as T : null;
            }
        }

        /// <summary>
        /// 检查系统是否已注册
        /// </summary>
        public bool HasSystem<T>() where T : class, IGameSystem
        {
            lock (_lock)
            {
                return _systems.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// 获取所有系统
        /// </summary>
        public IEnumerable<IGameSystem> GetAllSystems()
        {
            lock (_lock)
            {
                return _sortedSystems.ToList(); // 返回副本以避免并发修改
            }
        }

        /// <summary>
        /// 初始化所有系统
        /// </summary>
        public void InitializeAllSystems()
        {
            lock (_lock)
            {
                _logger?.LogInformation("Initializing {SystemCount} systems", _systems.Count);

                foreach (var system in _sortedSystems)
                {
                    try
                    {
                        if (!system.IsInitialized)
                        {
                            system.Initialize();
                            _logger?.LogDebug("Initialized system {SystemName}", system.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to initialize system {SystemName}", system.Name);
                        // 继续初始化其他系统
                    }
                }

                _isInitialized = true;
                _logger?.LogInformation("System initialization completed");
            }
        }

        /// <summary>
        /// 更新所有系统
        /// </summary>
        public void UpdateAllSystems(float deltaTime)
        {
            // 不需要锁定，因为_sortedSystems在更新期间不会改变
            // 如果需要修改系统列表，会在下一帧生效
            var systems = _sortedSystems;

            foreach (var system in systems)
            {
                try
                {
                    if (system.IsInitialized)
                    {
                        system.Update(deltaTime);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error updating system {SystemName}", system.Name);
                    // 继续更新其他系统
                }
            }
        }

        /// <summary>
        /// 关闭所有系统
        /// </summary>
        public void ShutdownAllSystems()
        {
            lock (_lock)
            {
                _logger?.LogInformation("Shutting down {SystemCount} systems", _systems.Count);

                // 按相反顺序关闭系统
                var systemsToShutdown = _sortedSystems.ToList();
                systemsToShutdown.Reverse();

                foreach (var system in systemsToShutdown)
                {
                    try
                    {
                        if (system.IsInitialized)
                        {
                            system.Shutdown();
                            _logger?.LogDebug("Shutdown system {SystemName}", system.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error shutting down system {SystemName}", system.Name);
                        // 继续关闭其他系统
                    }
                }

                _isInitialized = false;
                _logger?.LogInformation("System shutdown completed");
            }
        }

        /// <summary>
        /// 重建排序的系统列表
        /// </summary>
        private void RebuildSortedSystemsList()
        {
            _sortedSystems.Clear();
            _sortedSystems.AddRange(_systems.Values.OrderBy(s => s.Priority));
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public SystemManagerStats GetStats()
        {
            lock (_lock)
            {
                return new SystemManagerStats
                {
                    TotalSystems = _systems.Count,
                    InitializedSystems = _systems.Values.Count(s => s.IsInitialized),
                    UninitializedSystems = _systems.Values.Count(s => !s.IsInitialized),
                    IsManagerInitialized = _isInitialized
                };
            }
        }
    }

    /// <summary>
    /// 系统管理器统计信息
    /// </summary>
    public class SystemManagerStats
    {
        public int TotalSystems { get; set; }
        public int InitializedSystems { get; set; }
        public int UninitializedSystems { get; set; }
        public bool IsManagerInitialized { get; set; }
    }
}