using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RimWorldFramework.Core.ECS
{
    /// <summary>
    /// 组件系统接口
    /// </summary>
    public interface IComponentSystem
    {
        /// <summary>
        /// 注册组件类型
        /// </summary>
        void RegisterComponentType<T>() where T : IComponent;

        /// <summary>
        /// 注册组件类型
        /// </summary>
        void RegisterComponentType(Type componentType);

        /// <summary>
        /// 检查组件类型是否已注册
        /// </summary>
        bool IsComponentTypeRegistered<T>() where T : IComponent;

        /// <summary>
        /// 检查组件类型是否已注册
        /// </summary>
        bool IsComponentTypeRegistered(Type componentType);

        /// <summary>
        /// 获取所有已注册的组件类型
        /// </summary>
        IEnumerable<Type> GetRegisteredComponentTypes();

        /// <summary>
        /// 创建组件实例
        /// </summary>
        T CreateComponent<T>() where T : IComponent, new();

        /// <summary>
        /// 创建组件实例
        /// </summary>
        IComponent CreateComponent(Type componentType);

        /// <summary>
        /// 验证组件类型
        /// </summary>
        bool ValidateComponentType(Type type);

        /// <summary>
        /// 获取组件类型信息
        /// </summary>
        ComponentTypeInfo GetComponentTypeInfo<T>() where T : IComponent;

        /// <summary>
        /// 获取组件类型信息
        /// </summary>
        ComponentTypeInfo GetComponentTypeInfo(Type componentType);
    }

    /// <summary>
    /// 组件类型信息
    /// </summary>
    public class ComponentTypeInfo
    {
        public Type ComponentType { get; }
        public string Name { get; }
        public string Description { get; }
        public bool IsAbstract { get; }
        public bool IsSealed { get; }
        public PropertyInfo[] Properties { get; }
        public FieldInfo[] Fields { get; }

        public ComponentTypeInfo(Type componentType)
        {
            ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
            Name = componentType.Name;
            Description = GetComponentDescription(componentType);
            IsAbstract = componentType.IsAbstract;
            IsSealed = componentType.IsSealed;
            Properties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        }

        private static string GetComponentDescription(Type componentType)
        {
            // 尝试从特性获取描述
            var descriptionAttr = componentType.GetCustomAttribute<ComponentDescriptionAttribute>();
            if (descriptionAttr != null)
                return descriptionAttr.Description;

            // 使用类型名称作为默认描述
            return $"Component of type {componentType.Name}";
        }
    }

    /// <summary>
    /// 组件描述特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public ComponentDescriptionAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }

    /// <summary>
    /// 组件系统实现
    /// </summary>
    public class ComponentSystem : IComponentSystem
    {
        private readonly Dictionary<Type, ComponentTypeInfo> _registeredTypes = new();
        private readonly Dictionary<string, Type> _typeNameLookup = new();
        private readonly object _lock = new();

        /// <summary>
        /// 构造函数，自动注册基础组件类型
        /// </summary>
        public ComponentSystem()
        {
            // 自动注册基础组件类型
            RegisterComponentType<Component>();
        }

        /// <summary>
        /// 注册组件类型
        /// </summary>
        public void RegisterComponentType<T>() where T : IComponent
        {
            RegisterComponentType(typeof(T));
        }

        /// <summary>
        /// 注册组件类型
        /// </summary>
        public void RegisterComponentType(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));

            if (!ValidateComponentType(componentType))
                throw new ArgumentException($"Type {componentType.Name} is not a valid component type", nameof(componentType));

            lock (_lock)
            {
                if (_registeredTypes.ContainsKey(componentType))
                    return; // 已经注册

                var typeInfo = new ComponentTypeInfo(componentType);
                _registeredTypes[componentType] = typeInfo;
                _typeNameLookup[componentType.Name] = componentType;
                _typeNameLookup[componentType.FullName ?? componentType.Name] = componentType;
            }
        }

        /// <summary>
        /// 检查组件类型是否已注册
        /// </summary>
        public bool IsComponentTypeRegistered<T>() where T : IComponent
        {
            return IsComponentTypeRegistered(typeof(T));
        }

        /// <summary>
        /// 检查组件类型是否已注册
        /// </summary>
        public bool IsComponentTypeRegistered(Type componentType)
        {
            lock (_lock)
            {
                return _registeredTypes.ContainsKey(componentType);
            }
        }

        /// <summary>
        /// 获取所有已注册的组件类型
        /// </summary>
        public IEnumerable<Type> GetRegisteredComponentTypes()
        {
            lock (_lock)
            {
                return _registeredTypes.Keys.ToList();
            }
        }

        /// <summary>
        /// 创建组件实例
        /// </summary>
        public T CreateComponent<T>() where T : IComponent, new()
        {
            var componentType = typeof(T);
            
            if (!IsComponentTypeRegistered(componentType))
            {
                RegisterComponentType(componentType);
            }

            return new T();
        }

        /// <summary>
        /// 创建组件实例
        /// </summary>
        public IComponent CreateComponent(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));

            if (!ValidateComponentType(componentType))
                throw new ArgumentException($"Type {componentType.Name} is not a valid component type", nameof(componentType));

            if (!IsComponentTypeRegistered(componentType))
            {
                RegisterComponentType(componentType);
            }

            try
            {
                return (IComponent)Activator.CreateInstance(componentType)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create instance of component type {componentType.Name}", ex);
            }
        }

        /// <summary>
        /// 验证组件类型
        /// </summary>
        public bool ValidateComponentType(Type type)
        {
            if (type == null)
                return false;

            // 必须实现IComponent接口
            if (!typeof(IComponent).IsAssignableFrom(type))
                return false;

            // 不能是接口或抽象类（除非是基础Component类）
            if (type.IsInterface)
                return false;

            if (type.IsAbstract && type != typeof(Component))
                return false;

            // 必须有无参构造函数
            var constructors = type.GetConstructors();
            if (!constructors.Any(c => c.GetParameters().Length == 0))
                return false;

            return true;
        }

        /// <summary>
        /// 获取组件类型信息
        /// </summary>
        public ComponentTypeInfo GetComponentTypeInfo<T>() where T : IComponent
        {
            return GetComponentTypeInfo(typeof(T));
        }

        /// <summary>
        /// 获取组件类型信息
        /// </summary>
        public ComponentTypeInfo GetComponentTypeInfo(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));

            lock (_lock)
            {
                if (_registeredTypes.TryGetValue(componentType, out var typeInfo))
                {
                    return typeInfo;
                }

                // 如果类型未注册但是有效，则自动注册
                if (ValidateComponentType(componentType))
                {
                    RegisterComponentType(componentType);
                    return _registeredTypes[componentType];
                }

                throw new ArgumentException($"Component type {componentType.Name} is not registered and is not valid", nameof(componentType));
            }
        }

        /// <summary>
        /// 根据名称查找组件类型
        /// </summary>
        public Type? FindComponentTypeByName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            lock (_lock)
            {
                return _typeNameLookup.TryGetValue(typeName, out var type) ? type : null;
            }
        }

        /// <summary>
        /// 自动发现并注册程序集中的所有组件类型
        /// </summary>
        public void AutoRegisterComponentTypes(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var componentTypes = assembly.GetTypes()
                .Where(type => ValidateComponentType(type))
                .ToList();

            foreach (var componentType in componentTypes)
            {
                try
                {
                    RegisterComponentType(componentType);
                }
                catch (Exception)
                {
                    // 忽略注册失败的类型
                }
            }
        }

        /// <summary>
        /// 获取组件类型的统计信息
        /// </summary>
        public ComponentSystemStats GetStats()
        {
            lock (_lock)
            {
                return new ComponentSystemStats
                {
                    RegisteredTypesCount = _registeredTypes.Count,
                    ConcreteTypesCount = _registeredTypes.Values.Count(t => !t.IsAbstract),
                    AbstractTypesCount = _registeredTypes.Values.Count(t => t.IsAbstract),
                    SealedTypesCount = _registeredTypes.Values.Count(t => t.IsSealed)
                };
            }
        }
    }

    /// <summary>
    /// 组件系统统计信息
    /// </summary>
    public class ComponentSystemStats
    {
        public int RegisteredTypesCount { get; set; }
        public int ConcreteTypesCount { get; set; }
        public int AbstractTypesCount { get; set; }
        public int SealedTypesCount { get; set; }
    }
}