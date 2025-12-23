using System;

namespace RimWorldFramework.Core.Events
{
    /// <summary>
    /// 游戏事件接口
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// 事件时间戳
        /// </summary>
        DateTime Timestamp { get; }
    }

    /// <summary>
    /// 事件处理器接口
    /// </summary>
    public interface IEventHandler<in T> where T : IGameEvent
    {
        void Handle(T gameEvent);
    }

    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        void Publish<T>(T gameEvent) where T : IGameEvent;

        /// <summary>
        /// 订阅事件
        /// </summary>
        void Subscribe<T>(IEventHandler<T> handler) where T : IGameEvent;

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        void Unsubscribe<T>(IEventHandler<T> handler) where T : IGameEvent;

        /// <summary>
        /// 订阅事件（使用委托）
        /// </summary>
        void Subscribe<T>(Action<T> handler) where T : IGameEvent;

        /// <summary>
        /// 取消订阅事件（使用委托）
        /// </summary>
        void Unsubscribe<T>(Action<T> handler) where T : IGameEvent;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 基础游戏事件
    /// </summary>
    public abstract class GameEvent : IGameEvent
    {
        public DateTime Timestamp { get; }

        protected GameEvent()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}