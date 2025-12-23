using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RimWorldFramework.Core.Resources
{
    /// <summary>
    /// 内存池实现
    /// 提供高效的对象重用机制，减少GC压力
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class MemoryPool<T> : IMemoryPool<T> where T : class, new()
    {
        private readonly ConcurrentQueue<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T> _resetAction;
        private readonly int _maxCapacity;
        private int _currentCount;

        /// <summary>
        /// 获取池中可用对象数量
        /// </summary>
        public int AvailableCount => _pool.Count;

        /// <summary>
        /// 获取池的总容量
        /// </summary>
        public int Capacity => _maxCapacity;

        /// <summary>
        /// 获取当前已创建的对象数量
        /// </summary>
        public int CurrentCount => _currentCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="factory">对象工厂方法</param>
        /// <param name="resetAction">对象重置方法</param>
        /// <param name="maxCapacity">最大容量</param>
        public MemoryPool(Func<T> factory = null, Action<T> resetAction = null, int maxCapacity = 100)
        {
            _pool = new ConcurrentQueue<T>();
            _factory = factory ?? (() => new T());
            _resetAction = resetAction;
            _maxCapacity = maxCapacity;
            _currentCount = 0;
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            if (_pool.TryDequeue(out T item))
            {
                return item;
            }

            // 池中没有可用对象，创建新对象
            Interlocked.Increment(ref _currentCount);
            return _factory();
        }

        /// <summary>
        /// 将对象返回到池中
        /// </summary>
        /// <param name="item">要返回的对象</param>
        public void Return(T item)
        {
            if (item == null)
                return;

            // 如果池已满，直接丢弃对象
            if (_pool.Count >= _maxCapacity)
            {
                Interlocked.Decrement(ref _currentCount);
                return;
            }

            // 重置对象状态
            _resetAction?.Invoke(item);

            // 将对象放回池中
            _pool.Enqueue(item);
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (_pool.TryDequeue(out _))
            {
                // 清空队列
            }
            _currentCount = 0;
        }

        /// <summary>
        /// 预热池（预先创建指定数量的对象）
        /// </summary>
        /// <param name="count">预创建的对象数量</param>
        public void Warmup(int count)
        {
            count = Math.Min(count, _maxCapacity);
            
            for (int i = 0; i < count; i++)
            {
                if (_pool.Count >= _maxCapacity)
                    break;

                var item = _factory();
                _pool.Enqueue(item);
                Interlocked.Increment(ref _currentCount);
            }
        }
    }

    /// <summary>
    /// 对象池实现
    /// 提供对象的重用和生命周期管理
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> : IObjectPool<T> where T : class, new()
    {
        private readonly ConcurrentQueue<T> _pool;
        private readonly Func<T> _factory;
        private readonly Action<T> _resetAction;
        private readonly Action<T> _destroyAction;
        private readonly int _maxCapacity;
        private int _totalCount;
        private long _getCount;
        private long _returnCount;

        /// <summary>
        /// 获取池中可用对象数量
        /// </summary>
        public int AvailableCount => _pool.Count;

        /// <summary>
        /// 获取已创建的对象总数
        /// </summary>
        public int TotalCount => _totalCount;

        /// <summary>
        /// 获取获取次数
        /// </summary>
        public long GetCount => _getCount;

        /// <summary>
        /// 获取返回次数
        /// </summary>
        public long ReturnCount => _returnCount;

        /// <summary>
        /// 获取命中率
        /// </summary>
        public double HitRate => _getCount > 0 ? (double)_returnCount / _getCount * 100 : 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="factory">对象工厂方法</param>
        /// <param name="resetAction">对象重置方法</param>
        /// <param name="destroyAction">对象销毁方法</param>
        /// <param name="maxCapacity">最大容量</param>
        public ObjectPool(Func<T> factory = null, Action<T> resetAction = null, Action<T> destroyAction = null, int maxCapacity = 100)
        {
            _pool = new ConcurrentQueue<T>();
            _factory = factory ?? (() => new T());
            _resetAction = resetAction;
            _destroyAction = destroyAction;
            _maxCapacity = maxCapacity;
            _totalCount = 0;
            _getCount = 0;
            _returnCount = 0;
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            Interlocked.Increment(ref _getCount);

            if (_pool.TryDequeue(out T item))
            {
                return item;
            }

            // 池中没有可用对象，创建新对象
            item = _factory();
            Interlocked.Increment(ref _totalCount);
            return item;
        }

        /// <summary>
        /// 将对象返回到池中
        /// </summary>
        /// <param name="item">要返回的对象</param>
        public void Return(T item)
        {
            if (item == null)
                return;

            Interlocked.Increment(ref _returnCount);

            // 如果池已满，销毁对象
            if (_pool.Count >= _maxCapacity)
            {
                _destroyAction?.Invoke(item);
                Interlocked.Decrement(ref _totalCount);
                return;
            }

            // 重置对象状态
            _resetAction?.Invoke(item);

            // 将对象放回池中
            _pool.Enqueue(item);
        }

        /// <summary>
        /// 预热池（预先创建指定数量的对象）
        /// </summary>
        /// <param name="count">预创建的对象数量</param>
        public void Warmup(int count)
        {
            count = Math.Min(count, _maxCapacity);
            
            for (int i = 0; i < count; i++)
            {
                if (_pool.Count >= _maxCapacity)
                    break;

                var item = _factory();
                _pool.Enqueue(item);
                Interlocked.Increment(ref _totalCount);
            }
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (_pool.TryDequeue(out T item))
            {
                _destroyAction?.Invoke(item);
            }
            _totalCount = 0;
            _getCount = 0;
            _returnCount = 0;
        }
    }
}