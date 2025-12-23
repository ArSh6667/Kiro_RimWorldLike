using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Resources
{
    /// <summary>
    /// 资源管理器实现
    /// 负责管理游戏中的各种资源，包括内存池、对象池和资源加载
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        private readonly ConcurrentDictionary<Type, object> _memoryPools;
        private readonly ConcurrentDictionary<Type, object> _objectPools;
        private readonly ConcurrentDictionary<string, object> _loadedResources;
        private readonly ConcurrentDictionary<string, ResourceStatus> _resourceStatus;
        private readonly ConcurrentDictionary<string, DateTime> _resourceLastAccess;
        private readonly Timer _cleanupTimer;
        private readonly object _lockObject = new object();

        private long _memoryLimitBytes;
        private long _resourceLoadCount;
        private long _resourceUnloadCount;
        private long _cacheHitCount;
        private long _cacheMissCount;

        public event EventHandler<ResourceStatusChangedEventArgs> ResourceStatusChanged;
        public event EventHandler<LowMemoryEventArgs> LowMemoryDetected;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryLimitMB">内存限制（MB）</param>
        /// <param name="cleanupIntervalMinutes">清理间隔（分钟）</param>
        public ResourceManager(long memoryLimitMB = 1024, int cleanupIntervalMinutes = 5)
        {
            _memoryPools = new ConcurrentDictionary<Type, object>();
            _objectPools = new ConcurrentDictionary<Type, object>();
            _loadedResources = new ConcurrentDictionary<string, object>();
            _resourceStatus = new ConcurrentDictionary<string, ResourceStatus>();
            _resourceLastAccess = new ConcurrentDictionary<string, DateTime>();

            _memoryLimitBytes = memoryLimitMB * 1024 * 1024;
            _resourceLoadCount = 0;
            _resourceUnloadCount = 0;
            _cacheHitCount = 0;
            _cacheMissCount = 0;

            // 设置定期清理定时器
            _cleanupTimer = new Timer(async _ => await CleanupUnusedResourcesAsync(), 
                null, TimeSpan.FromMinutes(cleanupIntervalMinutes), TimeSpan.FromMinutes(cleanupIntervalMinutes));
        }

        /// <summary>
        /// 获取内存池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>内存池实例</returns>
        public IMemoryPool<T> GetMemoryPool<T>() where T : class, new()
        {
            return (IMemoryPool<T>)_memoryPools.GetOrAdd(typeof(T), _ => new MemoryPool<T>());
        }

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象池实例</returns>
        public IObjectPool<T> GetObjectPool<T>() where T : class, new()
        {
            return (IObjectPool<T>)_objectPools.GetOrAdd(typeof(T), _ => new ObjectPool<T>());
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resourcePath">资源路径</param>
        /// <returns>加载的资源</returns>
        public async Task<T> LoadResourceAsync<T>(string resourcePath) where T : class
        {
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentException("Resource path cannot be null or empty", nameof(resourcePath));

            // 检查缓存
            if (_loadedResources.TryGetValue(resourcePath, out var cachedResource))
            {
                _resourceLastAccess[resourcePath] = DateTime.UtcNow;
                Interlocked.Increment(ref _cacheHitCount);
                OnResourceStatusChanged(resourcePath, ResourceStatus.Cached, ResourceStatus.Loaded, "Cache hit");
                return (T)cachedResource;
            }

            Interlocked.Increment(ref _cacheMissCount);
            OnResourceStatusChanged(resourcePath, ResourceStatus.NotLoaded, ResourceStatus.Loading, "Loading started");

            try
            {
                // 检查内存使用情况
                await CheckMemoryUsageAsync();

                // 模拟资源加载
                var resource = await LoadResourceFromDiskAsync<T>(resourcePath);
                
                // 缓存资源
                _loadedResources[resourcePath] = resource;
                _resourceStatus[resourcePath] = ResourceStatus.Loaded;
                _resourceLastAccess[resourcePath] = DateTime.UtcNow;
                
                Interlocked.Increment(ref _resourceLoadCount);
                OnResourceStatusChanged(resourcePath, ResourceStatus.Loading, ResourceStatus.Loaded, "Loading completed");

                return resource;
            }
            catch (Exception ex)
            {
                _resourceStatus[resourcePath] = ResourceStatus.Error;
                OnResourceStatusChanged(resourcePath, ResourceStatus.Loading, ResourceStatus.Error, $"Loading failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="resourcePath">资源路径</param>
        public async Task UnloadResourceAsync(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return;

            if (!_loadedResources.ContainsKey(resourcePath))
                return;

            OnResourceStatusChanged(resourcePath, ResourceStatus.Loaded, ResourceStatus.Unloading, "Unloading started");

            try
            {
                // 从缓存中移除
                _loadedResources.TryRemove(resourcePath, out var resource);
                _resourceStatus.TryRemove(resourcePath, out _);
                _resourceLastAccess.TryRemove(resourcePath, out _);

                // 如果资源实现了IDisposable，则释放它
                if (resource is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                Interlocked.Increment(ref _resourceUnloadCount);
                OnResourceStatusChanged(resourcePath, ResourceStatus.Unloading, ResourceStatus.NotLoaded, "Unloading completed");
            }
            catch (Exception ex)
            {
                OnResourceStatusChanged(resourcePath, ResourceStatus.Unloading, ResourceStatus.Error, $"Unloading failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="resourcePaths">资源路径列表</param>
        public async Task PreloadResourcesAsync(IEnumerable<string> resourcePaths)
        {
            var loadTasks = resourcePaths.Select(async path =>
            {
                try
                {
                    await LoadResourceAsync<object>(path);
                }
                catch (Exception ex)
                {
                    // 记录错误但不中断其他资源的加载
                    Console.WriteLine($"Failed to preload resource {path}: {ex.Message}");
                }
            });

            await Task.WhenAll(loadTasks);
        }

        /// <summary>
        /// 获取内存使用情况
        /// </summary>
        /// <returns>内存使用统计</returns>
        public MemoryUsageInfo GetMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            var gcMemory = GC.GetTotalMemory(false);

            return new MemoryUsageInfo
            {
                UsedMemoryBytes = process.WorkingSet64,
                TotalMemoryBytes = _memoryLimitBytes,
                AvailableMemoryBytes = Math.Max(0, _memoryLimitBytes - process.WorkingSet64),
                GCMemoryBytes = gcMemory
            };
        }

        /// <summary>
        /// 执行垃圾回收
        /// </summary>
        /// <param name="force">是否强制回收</param>
        public void CollectGarbage(bool force = false)
        {
            if (force)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            else
            {
                GC.Collect(0, GCCollectionMode.Optimized);
            }
        }

        /// <summary>
        /// 清理未使用的资源
        /// </summary>
        public async Task CleanupUnusedResourcesAsync()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30); // 30分钟未使用的资源
            var resourcesToRemove = new List<string>();

            foreach (var kvp in _resourceLastAccess)
            {
                if (kvp.Value < cutoffTime)
                {
                    resourcesToRemove.Add(kvp.Key);
                }
            }

            foreach (var resourcePath in resourcesToRemove)
            {
                try
                {
                    await UnloadResourceAsync(resourcePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to cleanup resource {resourcePath}: {ex.Message}");
                }
            }

            // 执行垃圾回收
            if (resourcesToRemove.Count > 0)
            {
                CollectGarbage();
            }
        }

        /// <summary>
        /// 设置内存限制
        /// </summary>
        /// <param name="maxMemoryMB">最大内存使用量（MB）</param>
        public void SetMemoryLimit(long maxMemoryMB)
        {
            _memoryLimitBytes = maxMemoryMB * 1024 * 1024;
        }

        /// <summary>
        /// 获取资源统计信息
        /// </summary>
        /// <returns>资源统计</returns>
        public ResourceStatistics GetResourceStatistics()
        {
            var stats = new ResourceStatistics
            {
                LoadedResourceCount = _loadedResources.Count,
                CachedResourceCount = _resourceStatus.Count(kvp => kvp.Value == ResourceStatus.Cached),
                ResourceLoadCount = _resourceLoadCount,
                ResourceUnloadCount = _resourceUnloadCount,
                CacheHitRate = _cacheHitCount + _cacheMissCount > 0 ? 
                    (double)_cacheHitCount / (_cacheHitCount + _cacheMissCount) * 100 : 0
            };

            // 收集内存池统计
            foreach (var kvp in _memoryPools)
            {
                var poolType = kvp.Key;
                var pool = kvp.Value;
                
                if (pool is MemoryPool<object> memoryPool)
                {
                    stats.MemoryPoolStats[poolType.Name] = new PoolStatistics
                    {
                        PoolName = poolType.Name,
                        AvailableCount = memoryPool.AvailableCount,
                        TotalCount = memoryPool.CurrentCount
                    };
                }
            }

            // 收集对象池统计
            foreach (var kvp in _objectPools)
            {
                var poolType = kvp.Key;
                var pool = kvp.Value;
                
                if (pool is ObjectPool<object> objectPool)
                {
                    stats.ObjectPoolStats[poolType.Name] = new PoolStatistics
                    {
                        PoolName = poolType.Name,
                        AvailableCount = objectPool.AvailableCount,
                        TotalCount = objectPool.TotalCount,
                        GetCount = objectPool.GetCount,
                        ReturnCount = objectPool.ReturnCount,
                        HitRate = objectPool.HitRate
                    };
                }
            }

            return stats;
        }

        /// <summary>
        /// 检查内存使用情况
        /// </summary>
        private async Task CheckMemoryUsageAsync()
        {
            var memoryInfo = GetMemoryUsage();
            
            if (memoryInfo.UsagePercentage > 80) // 内存使用超过80%
            {
                var lowMemoryArgs = new LowMemoryEventArgs
                {
                    CurrentMemoryMB = memoryInfo.UsedMemoryBytes / (1024 * 1024),
                    MemoryLimitMB = _memoryLimitBytes / (1024 * 1024),
                    MemoryUsagePercentage = memoryInfo.UsagePercentage,
                    SuggestedActions = new List<string>
                    {
                        "Clean up unused resources",
                        "Force garbage collection",
                        "Reduce resource cache size"
                    }
                };

                OnLowMemoryDetected(lowMemoryArgs);

                // 自动清理
                await CleanupUnusedResourcesAsync();
                CollectGarbage(true);
            }
        }

        /// <summary>
        /// 从磁盘加载资源（模拟实现）
        /// </summary>
        private async Task<T> LoadResourceFromDiskAsync<T>(string resourcePath) where T : class
        {
            // 模拟异步加载延迟
            await Task.Delay(10);

            // 简化的资源加载实现
            if (typeof(T) == typeof(string))
            {
                return File.Exists(resourcePath) ? File.ReadAllText(resourcePath) as T : 
                    $"Mock resource content for {resourcePath}" as T;
            }
            
            if (typeof(T) == typeof(byte[]))
            {
                return File.Exists(resourcePath) ? File.ReadAllBytes(resourcePath) as T :
                    System.Text.Encoding.UTF8.GetBytes($"Mock binary content for {resourcePath}") as T;
            }

            // 对于其他类型，创建一个模拟对象
            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// 触发资源状态变化事件
        /// </summary>
        private void OnResourceStatusChanged(string resourcePath, ResourceStatus oldStatus, ResourceStatus newStatus, string reason)
        {
            ResourceStatusChanged?.Invoke(this, new ResourceStatusChangedEventArgs
            {
                ResourcePath = resourcePath,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason
            });
        }

        /// <summary>
        /// 触发内存不足事件
        /// </summary>
        private void OnLowMemoryDetected(LowMemoryEventArgs args)
        {
            LowMemoryDetected?.Invoke(this, args);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            
            // 清理所有资源
            foreach (var resourcePath in _loadedResources.Keys.ToList())
            {
                try
                {
                    UnloadResourceAsync(resourcePath).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to dispose resource {resourcePath}: {ex.Message}");
                }
            }

            // 清理所有池
            foreach (var pool in _memoryPools.Values)
            {
                if (pool is MemoryPool<object> memoryPool)
                {
                    memoryPool.Clear();
                }
            }

            foreach (var pool in _objectPools.Values)
            {
                if (pool is ObjectPool<object> objectPool)
                {
                    objectPool.Clear();
                }
            }
        }
    }
}