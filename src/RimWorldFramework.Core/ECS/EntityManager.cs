using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace RimWorldFramework.Core.ECS
{
    /// <summary>
    /// 实体管理器实现
    /// </summary>
    public class EntityManager : IEntityManager
    {
        private readonly Dictionary<EntityId, Entity> _entities = new();
        private readonly Dictionary<EntityId, Dictionary<Type, IComponent>> _components = new();
        private readonly Dictionary<Type, HashSet<EntityId>> _componentIndex = new();
        private readonly Queue<uint> _recycledIds = new();
        private uint _nextEntityId = 1;
        private readonly object _lock = new();

        /// <summary>
        /// 创建新实体
        /// </summary>
        public EntityId CreateEntity()
        {
            lock (_lock)
            {
                var entityId = GetNextEntityId();
                var entity = new BasicEntity(entityId);
                
                _entities[entityId] = entity;
                _components[entityId] = new Dictionary<Type, IComponent>();
                
                return entityId;
            }
        }

        /// <summary>
        /// 创建指定类型的实体
        /// </summary>
        public T CreateEntity<T>() where T : Entity, new()
        {
            lock (_lock)
            {
                var entityId = GetNextEntityId();
                var entity = new T();
                entity.Id = entityId;
                entity.IsActive = true;
                
                _entities[entityId] = entity;
                _components[entityId] = new Dictionary<Type, IComponent>();
                
                return entity;
            }
        }

        /// <summary>
        /// 销毁实体
        /// </summary>
        public void DestroyEntity(EntityId entityId)
        {
            lock (_lock)
            {
                if (!_entities.TryGetValue(entityId, out var entity))
                    return;

                // 移除所有组件
                if (_components.TryGetValue(entityId, out var entityComponents))
                {
                    foreach (var componentType in entityComponents.Keys.ToList())
                    {
                        RemoveComponentInternal(entityId, componentType);
                    }
                    _components.Remove(entityId);
                }

                // 标记实体为非活跃并移除
                entity.IsActive = false;
                _entities.Remove(entityId);
                
                // 回收实体ID
                _recycledIds.Enqueue(entityId.Value);
            }
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        public bool EntityExists(EntityId entityId)
        {
            lock (_lock)
            {
                return _entities.ContainsKey(entityId) && _entities[entityId].IsActive;
            }
        }

        /// <summary>
        /// 获取实体
        /// </summary>
        public Entity? GetEntity(EntityId entityId)
        {
            lock (_lock)
            {
                return _entities.TryGetValue(entityId, out var entity) && entity.IsActive ? entity : null;
            }
        }

        /// <summary>
        /// 获取指定类型的实体
        /// </summary>
        public T? GetEntity<T>(EntityId entityId) where T : Entity
        {
            return GetEntity(entityId) as T;
        }

        /// <summary>
        /// 获取所有活跃实体
        /// </summary>
        public IEnumerable<Entity> GetAllEntities()
        {
            lock (_lock)
            {
                return _entities.Values.Where(e => e.IsActive).ToList();
            }
        }

        /// <summary>
        /// 获取指定类型的所有实体
        /// </summary>
        public IEnumerable<T> GetEntitiesOfType<T>() where T : Entity
        {
            lock (_lock)
            {
                return _entities.Values
                    .Where(e => e.IsActive && e is T)
                    .Cast<T>()
                    .ToList();
            }
        }

        /// <summary>
        /// 为实体添加组件
        /// </summary>
        public void AddComponent<T>(EntityId entityId, T component) where T : IComponent
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            lock (_lock)
            {
                if (!_entities.ContainsKey(entityId))
                    throw new InvalidOperationException($"Entity {entityId} does not exist");

                var componentType = typeof(T);
                
                if (!_components.TryGetValue(entityId, out var entityComponents))
                {
                    entityComponents = new Dictionary<Type, IComponent>();
                    _components[entityId] = entityComponents;
                }

                // 设置组件的实体ID
                component.EntityId = entityId;
                
                // 添加组件
                entityComponents[componentType] = component;
                
                // 更新组件索引
                if (!_componentIndex.TryGetValue(componentType, out var entitySet))
                {
                    entitySet = new HashSet<EntityId>();
                    _componentIndex[componentType] = entitySet;
                }
                entitySet.Add(entityId);
            }
        }

        /// <summary>
        /// 移除实体的组件
        /// </summary>
        public void RemoveComponent<T>(EntityId entityId) where T : IComponent
        {
            lock (_lock)
            {
                RemoveComponentInternal(entityId, typeof(T));
            }
        }

        /// <summary>
        /// 获取实体的组件
        /// </summary>
        public T? GetComponent<T>(EntityId entityId) where T : IComponent
        {
            lock (_lock)
            {
                if (_components.TryGetValue(entityId, out var entityComponents) &&
                    entityComponents.TryGetValue(typeof(T), out var component))
                {
                    return (T)component;
                }
                return default;
            }
        }

        /// <summary>
        /// 检查实体是否有指定组件
        /// </summary>
        public bool HasComponent<T>(EntityId entityId) where T : IComponent
        {
            lock (_lock)
            {
                return _components.TryGetValue(entityId, out var entityComponents) &&
                       entityComponents.ContainsKey(typeof(T));
            }
        }

        /// <summary>
        /// 获取实体的所有组件
        /// </summary>
        public IEnumerable<IComponent> GetAllComponents(EntityId entityId)
        {
            lock (_lock)
            {
                if (_components.TryGetValue(entityId, out var entityComponents))
                {
                    return entityComponents.Values.ToList();
                }
                return Enumerable.Empty<IComponent>();
            }
        }

        /// <summary>
        /// 获取拥有指定组件类型的所有实体
        /// </summary>
        public IEnumerable<EntityId> GetEntitiesWithComponent<T>() where T : IComponent
        {
            lock (_lock)
            {
                if (_componentIndex.TryGetValue(typeof(T), out var entitySet))
                {
                    return entitySet.Where(id => _entities.ContainsKey(id) && _entities[id].IsActive).ToList();
                }
                return Enumerable.Empty<EntityId>();
            }
        }

        /// <summary>
        /// 获取拥有指定组件类型的所有实体和组件
        /// </summary>
        public IEnumerable<(EntityId entityId, T component)> GetEntitiesWithComponentData<T>() where T : IComponent
        {
            lock (_lock)
            {
                var results = new List<(EntityId, T)>();
                
                if (_componentIndex.TryGetValue(typeof(T), out var entitySet))
                {
                    foreach (var entityId in entitySet)
                    {
                        if (_entities.ContainsKey(entityId) && _entities[entityId].IsActive &&
                            _components.TryGetValue(entityId, out var entityComponents) &&
                            entityComponents.TryGetValue(typeof(T), out var component))
                        {
                            results.Add((entityId, (T)component));
                        }
                    }
                }
                
                return results;
            }
        }

        /// <summary>
        /// 获取下一个可用的实体ID
        /// </summary>
        private EntityId GetNextEntityId()
        {
            if (_recycledIds.Count > 0)
            {
                return new EntityId(_recycledIds.Dequeue());
            }
            
            return new EntityId(_nextEntityId++);
        }

        /// <summary>
        /// 内部移除组件方法
        /// </summary>
        private void RemoveComponentInternal(EntityId entityId, Type componentType)
        {
            if (_components.TryGetValue(entityId, out var entityComponents))
            {
                entityComponents.Remove(componentType);
                
                // 更新组件索引
                if (_componentIndex.TryGetValue(componentType, out var entitySet))
                {
                    entitySet.Remove(entityId);
                    if (entitySet.Count == 0)
                    {
                        _componentIndex.Remove(componentType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 基础实体实现
    /// </summary>
    internal class BasicEntity : Entity
    {
        public BasicEntity(EntityId id) : base(id)
        {
        }
    }
}