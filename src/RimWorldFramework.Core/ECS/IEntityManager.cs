using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.ECS
{
    /// <summary>
    /// 实体管理器接口
    /// </summary>
    public interface IEntityManager
    {
        /// <summary>
        /// 创建新实体
        /// </summary>
        EntityId CreateEntity();

        /// <summary>
        /// 创建指定类型的实体
        /// </summary>
        T CreateEntity<T>() where T : Entity, new();

        /// <summary>
        /// 销毁实体
        /// </summary>
        void DestroyEntity(EntityId entityId);

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        bool EntityExists(EntityId entityId);

        /// <summary>
        /// 获取实体
        /// </summary>
        Entity? GetEntity(EntityId entityId);

        /// <summary>
        /// 获取指定类型的实体
        /// </summary>
        T? GetEntity<T>(EntityId entityId) where T : Entity;

        /// <summary>
        /// 获取所有活跃实体
        /// </summary>
        IEnumerable<Entity> GetAllEntities();

        /// <summary>
        /// 获取指定类型的所有实体
        /// </summary>
        IEnumerable<T> GetEntitiesOfType<T>() where T : Entity;

        /// <summary>
        /// 为实体添加组件
        /// </summary>
        void AddComponent<T>(EntityId entityId, T component) where T : IComponent;

        /// <summary>
        /// 移除实体的组件
        /// </summary>
        void RemoveComponent<T>(EntityId entityId) where T : IComponent;

        /// <summary>
        /// 获取实体的组件
        /// </summary>
        T? GetComponent<T>(EntityId entityId) where T : IComponent;

        /// <summary>
        /// 检查实体是否有指定组件
        /// </summary>
        bool HasComponent<T>(EntityId entityId) where T : IComponent;

        /// <summary>
        /// 获取实体的所有组件
        /// </summary>
        IEnumerable<IComponent> GetAllComponents(EntityId entityId);
    }
}