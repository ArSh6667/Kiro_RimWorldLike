using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Resources
{
    /// <summary>
    /// 资源管理器接口
    /// 负责管理游戏中的各种资源，包括内存池、对象池和资源加载
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// 资源状态变化事件
        /// </summary>
        event EventHandler<ResourceStatusChangedEventArgs> ResourceStatusChanged;

        /// <summary>
        /// 内存不足事件
        /// </summary>
        event EventHandler<LowMemoryEventArgs> LowMemoryDetected;

        /// <summary>
        /// 获取内存池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>内存池实例</returns>
        IMemoryPool<T> GetMemoryPool<T>() where T : class, new();

        /// <summary>
        /// 获取对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>对象池实例</returns>
        IObjectPool<T> GetObjectPool<T>() where T : class, new();

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="resourcePath">资源路径</param>
        /// <returns>加载的资源</returns>
        Task<T> LoadResourceAsync<T>(string resourcePath) where T : class;

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="resourcePath">资源路径</param>
        Task UnloadResourceAsync(string resourcePath);

        /// <summary>
        /// 预加载资源
        /// </summary>
        /// <param name="resourcePaths">资源路径列表</param>
        Task PreloadResourcesAsync(IEnumerable<string> resourcePaths);

        /// <summary>
        /// 获取内存使用情况
        /// </summary>
        /// <returns>内存使用统计</returns>
        MemoryUsageInfo GetMemoryUsage();

        /// <summary>
        /// 执行垃圾回收
        /// </summary>
        /// <param name="force">是否强制回收</param>
        void CollectGarbage(bool force = false);

        /// <summary>
        /// 清理未使用的资源
        /// </summary>
        Task CleanupUnusedResourcesAsync();

        /// <summary>
        /// 设置内存限制
        /// </summary>
        /// <param name="maxMemoryMB">最大内存使用量（MB）</param>
        void SetMemoryLimit(long maxMemoryMB);

        /// <summary>
        /// 获取资源统计信息
        /// </summary>
        /// <returns>资源统计</returns>
        ResourceStatistics GetResourceStatistics();
    }

    /// <summary>
    /// 内存池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IMemoryPool<T> where T : class
    {
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        T Get();

        /// <summary>
        /// 将对象返回到池中
        /// </summary>
        /// <param name="item">要返回的对象</param>
        void Return(T item);

        /// <summary>
        /// 清空池
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取池中可用对象数量
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// 获取池的总容量
        /// </summary>
        int Capacity { get; }
    }

    /// <summary>
    /// 对象池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IObjectPool<T> where T : class
    {
        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        T Get();

        /// <summary>
        /// 将对象返回到池中
        /// </summary>
        /// <param name="item">要返回的对象</param>
        void Return(T item);

        /// <summary>
        /// 预热池（预先创建指定数量的对象）
        /// </summary>
        /// <param name="count">预创建的对象数量</param>
        void Warmup(int count);

        /// <summary>
        /// 清空池
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取池中可用对象数量
        /// </summary>
        int AvailableCount { get; }

        /// <summary>
        /// 获取已创建的对象总数
        /// </summary>
        int TotalCount { get; }
    }

    /// <summary>
    /// 资源状态变化事件参数
    /// </summary>
    public class ResourceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string ResourcePath { get; set; }

        /// <summary>
        /// 旧状态
        /// </summary>
        public ResourceStatus OldStatus { get; set; }

        /// <summary>
        /// 新状态
        /// </summary>
        public ResourceStatus NewStatus { get; set; }

        /// <summary>
        /// 状态变化原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 内存不足事件参数
    /// </summary>
    public class LowMemoryEventArgs : EventArgs
    {
        /// <summary>
        /// 当前内存使用量（MB）
        /// </summary>
        public long CurrentMemoryMB { get; set; }

        /// <summary>
        /// 内存限制（MB）
        /// </summary>
        public long MemoryLimitMB { get; set; }

        /// <summary>
        /// 内存使用百分比
        /// </summary>
        public double MemoryUsagePercentage { get; set; }

        /// <summary>
        /// 建议的清理操作
        /// </summary>
        public List<string> SuggestedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 内存使用信息
    /// </summary>
    public class MemoryUsageInfo
    {
        /// <summary>
        /// 已使用内存（字节）
        /// </summary>
        public long UsedMemoryBytes { get; set; }

        /// <summary>
        /// 总内存（字节）
        /// </summary>
        public long TotalMemoryBytes { get; set; }

        /// <summary>
        /// 可用内存（字节）
        /// </summary>
        public long AvailableMemoryBytes { get; set; }

        /// <summary>
        /// GC堆内存（字节）
        /// </summary>
        public long GCMemoryBytes { get; set; }

        /// <summary>
        /// 内存使用百分比
        /// </summary>
        public double UsagePercentage => TotalMemoryBytes > 0 ? (double)UsedMemoryBytes / TotalMemoryBytes * 100 : 0;
    }

    /// <summary>
    /// 资源统计信息
    /// </summary>
    public class ResourceStatistics
    {
        /// <summary>
        /// 已加载资源数量
        /// </summary>
        public int LoadedResourceCount { get; set; }

        /// <summary>
        /// 缓存资源数量
        /// </summary>
        public int CachedResourceCount { get; set; }

        /// <summary>
        /// 内存池统计
        /// </summary>
        public Dictionary<string, PoolStatistics> MemoryPoolStats { get; set; } = new Dictionary<string, PoolStatistics>();

        /// <summary>
        /// 对象池统计
        /// </summary>
        public Dictionary<string, PoolStatistics> ObjectPoolStats { get; set; } = new Dictionary<string, PoolStatistics>();

        /// <summary>
        /// 资源加载次数
        /// </summary>
        public long ResourceLoadCount { get; set; }

        /// <summary>
        /// 资源卸载次数
        /// </summary>
        public long ResourceUnloadCount { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRate { get; set; }
    }

    /// <summary>
    /// 池统计信息
    /// </summary>
    public class PoolStatistics
    {
        /// <summary>
        /// 池名称
        /// </summary>
        public string PoolName { get; set; }

        /// <summary>
        /// 可用对象数量
        /// </summary>
        public int AvailableCount { get; set; }

        /// <summary>
        /// 总对象数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 获取次数
        /// </summary>
        public long GetCount { get; set; }

        /// <summary>
        /// 返回次数
        /// </summary>
        public long ReturnCount { get; set; }

        /// <summary>
        /// 命中率
        /// </summary>
        public double HitRate => GetCount > 0 ? (double)ReturnCount / GetCount * 100 : 0;
    }

    /// <summary>
    /// 资源状态
    /// </summary>
    public enum ResourceStatus
    {
        /// <summary>
        /// 未加载
        /// </summary>
        NotLoaded,

        /// <summary>
        /// 加载中
        /// </summary>
        Loading,

        /// <summary>
        /// 已加载
        /// </summary>
        Loaded,

        /// <summary>
        /// 缓存中
        /// </summary>
        Cached,

        /// <summary>
        /// 卸载中
        /// </summary>
        Unloading,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }
}