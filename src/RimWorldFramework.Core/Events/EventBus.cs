using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace RimWorldFramework.Core.Events
{
    /// <summary>
    /// 事件总线实现
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<IEventHandler>> _handlers = new();
        private readonly ConcurrentDictionary<Type, ConcurrentBag<Delegate>> _delegateHandlers = new();
        private readonly object _lock = new();

        /// <summary>
        /// 发布事件
        /// </summary>
        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (gameEvent == null)
                return;

            var eventType = typeof(T);
            
            // 调用接口处理器
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        if (handler is EventHandlerAdapter<T> adapter)
                        {
                            adapter.Handle(gameEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不中断其他处理器
                        OnEventHandlingError(eventType, handler, ex);
                    }
                }
            }

            // 调用委托处理器
            if (_delegateHandlers.TryGetValue(eventType, out var delegateHandlers))
            {
                foreach (var handler in delegateHandlers)
                {
                    try
                    {
                        if (handler is Action<T> action)
                        {
                            action(gameEvent);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不中断其他处理器
                        OnEventHandlingError(eventType, handler, ex);
                    }
                }
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(IEventHandler<T> handler) where T : IGameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);
            var adapter = new EventHandlerAdapter<T>(handler);
            
            _handlers.AddOrUpdate(eventType, 
                new ConcurrentBag<IEventHandler> { adapter },
                (key, existing) => 
                {
                    existing.Add(adapter);
                    return existing;
                });
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(IEventHandler<T> handler) where T : IGameEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                // ConcurrentBag不支持直接移除，需要重建
                lock (_lock)
                {
                    var newHandlers = new ConcurrentBag<IEventHandler>();
                    
                    foreach (var h in handlers)
                    {
                        if (h is EventHandlerAdapter<T> adapter && 
                            !ReferenceEquals(adapter._handler, handler))
                        {
                            newHandlers.Add(h);
                        }
                        else if (!(h is EventHandlerAdapter<T>))
                        {
                            newHandlers.Add(h);
                        }
                    }
                    
                    if (newHandlers.IsEmpty)
                    {
                        _handlers.TryRemove(eventType, out _);
                    }
                    else
                    {
                        _handlers[eventType] = newHandlers;
                    }
                }
            }
        }

        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);
            _delegateHandlers.AddOrUpdate(eventType,
                new ConcurrentBag<Delegate> { handler },
                (key, existing) =>
                {
                    existing.Add(handler);
                    return existing;
                });
        }

        /// <summary>
        /// 取消订阅事件（使用委托）
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);
            if (_delegateHandlers.TryGetValue(eventType, out var handlers))
            {
                // ConcurrentBag不支持直接移除，需要重建
                lock (_lock)
                {
                    var newHandlers = new ConcurrentBag<Delegate>(
                        handlers.Where(h => !ReferenceEquals(h, handler)));
                    
                    if (newHandlers.IsEmpty)
                    {
                        _delegateHandlers.TryRemove(eventType, out _);
                    }
                    else
                    {
                        _delegateHandlers[eventType] = newHandlers;
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
            _delegateHandlers.Clear();
        }

        /// <summary>
        /// 获取事件类型的订阅者数量
        /// </summary>
        public int GetSubscriberCount<T>() where T : IGameEvent
        {
            var eventType = typeof(T);
            var count = 0;

            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                count += handlers.Count;
            }

            if (_delegateHandlers.TryGetValue(eventType, out var delegateHandlers))
            {
                count += delegateHandlers.Count;
            }

            return count;
        }

        /// <summary>
        /// 获取所有已订阅的事件类型
        /// </summary>
        public IEnumerable<Type> GetSubscribedEventTypes()
        {
            var types = new HashSet<Type>();
            
            foreach (var key in _handlers.Keys)
            {
                types.Add(key);
            }
            
            foreach (var key in _delegateHandlers.Keys)
            {
                types.Add(key);
            }
            
            return types;
        }

        /// <summary>
        /// 事件处理错误回调
        /// </summary>
        public event Action<Type, object, Exception>? EventHandlingError;

        /// <summary>
        /// 触发事件处理错误
        /// </summary>
        private void OnEventHandlingError(Type eventType, object handler, Exception exception)
        {
            EventHandlingError?.Invoke(eventType, handler, exception);
        }
    }

    /// <summary>
    /// 内部事件处理器接口（用于类型擦除）
    /// </summary>
    internal interface IEventHandler
    {
    }

    /// <summary>
    /// 内部事件处理器适配器
    /// </summary>
    internal class EventHandlerAdapter<T> : IEventHandler where T : IGameEvent
    {
        public readonly IEventHandler<T> _handler;

        public EventHandlerAdapter(IEventHandler<T> handler)
        {
            _handler = handler;
        }

        public void Handle(T gameEvent)
        {
            _handler.Handle(gameEvent);
        }
    }
}