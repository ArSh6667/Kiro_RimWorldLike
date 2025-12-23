using System;
using System.Linq;
using System.Reflection;

namespace RimWorldFramework.Tests.ECS
{
    /// <summary>
    /// 组件系统单元测试
    /// </summary>
    [TestFixture]
    public class ComponentSystemTests : TestBase
    {
        private ComponentSystem? _componentSystem;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _componentSystem = new ComponentSystem();
        }

        [TearDown]
        public override void TearDown()
        {
            _componentSystem = null;
            base.TearDown();
        }

        #region 组件类型注册测试

        [Test]
        public void RegisterComponentType_Generic_ShouldRegisterType()
        {
            // 行动
            _componentSystem!.RegisterComponentType<TestComponent>();

            // 断言
            Assert.That(_componentSystem.IsComponentTypeRegistered<TestComponent>(), Is.True);
            Assert.That(_componentSystem.GetRegisteredComponentTypes().Contains(typeof(TestComponent)), Is.True);
        }

        [Test]
        public void RegisterComponentType_ByType_ShouldRegisterType()
        {
            // 行动
            _componentSystem!.RegisterComponentType(typeof(TestComponent));

            // 断言
            Assert.That(_componentSystem.IsComponentTypeRegistered(typeof(TestComponent)), Is.True);
        }

        [Test]
        public void RegisterComponentType_Duplicate_ShouldNotThrow()
        {
            // 安排
            _componentSystem!.RegisterComponentType<TestComponent>();

            // 行动 & 断言
            AssertDoesNotThrow(() => _componentSystem.RegisterComponentType<TestComponent>());
            
            // 验证只注册了一次
            var registeredTypes = _componentSystem.GetRegisteredComponentTypes().ToList();
            var testComponentCount = registeredTypes.Count(t => t == typeof(TestComponent));
            Assert.That(testComponentCount, Is.EqualTo(1));
        }

        [Test]
        public void RegisterComponentType_WithNull_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentNullException>(() => 
                _componentSystem!.RegisterComponentType(null!));
        }

        [Test]
        public void RegisterComponentType_WithInvalidType_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentException>(() => 
                _componentSystem!.RegisterComponentType(typeof(string))); // string不是组件类型
        }

        [Test]
        public void RegisterComponentType_WithInterface_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentException>(() => 
                _componentSystem!.RegisterComponentType(typeof(IComponent)));
        }

        [Test]
        public void RegisterComponentType_WithAbstractClass_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentException>(() => 
                _componentSystem!.RegisterComponentType(typeof(AbstractTestComponent)));
        }

        #endregion

        #region 组件创建测试

        [Test]
        public void CreateComponent_Generic_ShouldCreateInstance()
        {
            // 行动
            var component = _componentSystem!.CreateComponent<TestComponent>();

            // 断言
            Assert.That(component, Is.Not.Null);
            Assert.That(component, Is.InstanceOf<TestComponent>());
            Assert.That(_componentSystem.IsComponentTypeRegistered<TestComponent>(), Is.True);
        }

        [Test]
        public void CreateComponent_ByType_ShouldCreateInstance()
        {
            // 行动
            var component = _componentSystem!.CreateComponent(typeof(TestComponent));

            // 断言
            Assert.That(component, Is.Not.Null);
            Assert.That(component, Is.InstanceOf<TestComponent>());
            Assert.That(_componentSystem.IsComponentTypeRegistered(typeof(TestComponent)), Is.True);
        }

        [Test]
        public void CreateComponent_WithNullType_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentNullException>(() => 
                _componentSystem!.CreateComponent(null!));
        }

        [Test]
        public void CreateComponent_WithInvalidType_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentException>(() => 
                _componentSystem!.CreateComponent(typeof(string)));
        }

        [Test]
        public void CreateComponent_WithNoParameterlessConstructor_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<InvalidOperationException>(() => 
                _componentSystem!.CreateComponent(typeof(ComponentWithoutDefaultConstructor)));
        }

        #endregion

        #region 组件类型验证测试

        [Test]
        public void ValidateComponentType_WithValidType_ShouldReturnTrue()
        {
            // 行动 & 断言
            Assert.That(_componentSystem!.ValidateComponentType(typeof(TestComponent)), Is.True);
            Assert.That(_componentSystem.ValidateComponentType(typeof(AnotherTestComponent)), Is.True);
        }

        [Test]
        public void ValidateComponentType_WithInvalidTypes_ShouldReturnFalse()
        {
            // 行动 & 断言
            Assert.That(_componentSystem!.ValidateComponentType(null), Is.False);
            Assert.That(_componentSystem.ValidateComponentType(typeof(string)), Is.False);
            Assert.That(_componentSystem.ValidateComponentType(typeof(IComponent)), Is.False);
            Assert.That(_componentSystem.ValidateComponentType(typeof(AbstractTestComponent)), Is.False);
        }

        [Test]
        public void ValidateComponentType_WithBaseComponentClass_ShouldReturnTrue()
        {
            // Component基类应该是有效的，即使它是抽象的
            Assert.That(_componentSystem!.ValidateComponentType(typeof(Component)), Is.True);
        }

        #endregion

        #region 组件类型信息测试

        [Test]
        public void GetComponentTypeInfo_ShouldReturnCorrectInfo()
        {
            // 安排
            _componentSystem!.RegisterComponentType<DescribedTestComponent>();

            // 行动
            var typeInfo = _componentSystem.GetComponentTypeInfo<DescribedTestComponent>();

            // 断言
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.ComponentType, Is.EqualTo(typeof(DescribedTestComponent)));
            Assert.That(typeInfo.Name, Is.EqualTo("DescribedTestComponent"));
            Assert.That(typeInfo.Description, Is.EqualTo("A test component with description"));
            Assert.That(typeInfo.IsAbstract, Is.False);
            Assert.That(typeInfo.IsSealed, Is.False);
            Assert.That(typeInfo.Properties.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GetComponentTypeInfo_WithUnregisteredType_ShouldAutoRegister()
        {
            // 行动
            var typeInfo = _componentSystem!.GetComponentTypeInfo<TestComponent>();

            // 断言
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(_componentSystem.IsComponentTypeRegistered<TestComponent>(), Is.True);
        }

        [Test]
        public void GetComponentTypeInfo_WithInvalidType_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentException>(() => 
                _componentSystem!.GetComponentTypeInfo(typeof(string)));
        }

        #endregion

        #region 自动注册测试

        [Test]
        public void AutoRegisterComponentTypes_ShouldRegisterValidTypes()
        {
            // 安排
            var assembly = Assembly.GetExecutingAssembly();
            var initialCount = _componentSystem!.GetRegisteredComponentTypes().Count();

            // 行动
            _componentSystem.AutoRegisterComponentTypes(assembly);

            // 断言
            var finalCount = _componentSystem.GetRegisteredComponentTypes().Count();
            Assert.That(finalCount, Is.GreaterThan(initialCount));
            
            // 验证特定类型已注册
            Assert.That(_componentSystem.IsComponentTypeRegistered<TestComponent>(), Is.True);
            Assert.That(_componentSystem.IsComponentTypeRegistered<AnotherTestComponent>(), Is.True);
        }

        [Test]
        public void AutoRegisterComponentTypes_WithNullAssembly_ShouldThrow()
        {
            // 行动 & 断言
            AssertThrows<ArgumentNullException>(() => 
                _componentSystem!.AutoRegisterComponentTypes(null!));
        }

        #endregion

        #region 统计信息测试

        [Test]
        public void GetStats_ShouldReturnCorrectStats()
        {
            // 安排
            _componentSystem!.RegisterComponentType<TestComponent>();
            _componentSystem.RegisterComponentType<AnotherTestComponent>();
            _componentSystem.RegisterComponentType<SealedTestComponent>();

            // 行动
            var stats = _componentSystem.GetStats();

            // 断言
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.RegisteredTypesCount, Is.GreaterThanOrEqualTo(4)); // 包括基础Component类
            Assert.That(stats.ConcreteTypesCount, Is.GreaterThan(0));
            Assert.That(stats.SealedTypesCount, Is.GreaterThanOrEqualTo(1)); // SealedTestComponent
        }

        #endregion

        #region 查找测试

        [Test]
        public void FindComponentTypeByName_ShouldReturnCorrectType()
        {
            // 安排
            _componentSystem!.RegisterComponentType<TestComponent>();

            // 行动
            var foundType = _componentSystem.FindComponentTypeByName("TestComponent");

            // 断言
            Assert.That(foundType, Is.EqualTo(typeof(TestComponent)));
        }

        [Test]
        public void FindComponentTypeByName_WithFullName_ShouldReturnCorrectType()
        {
            // 安排
            _componentSystem!.RegisterComponentType<TestComponent>();

            // 行动
            var foundType = _componentSystem.FindComponentTypeByName(typeof(TestComponent).FullName!);

            // 断言
            Assert.That(foundType, Is.EqualTo(typeof(TestComponent)));
        }

        [Test]
        public void FindComponentTypeByName_WithInvalidName_ShouldReturnNull()
        {
            // 行动
            var foundType = _componentSystem!.FindComponentTypeByName("NonExistentComponent");

            // 断言
            Assert.That(foundType, Is.Null);
        }

        [Test]
        public void FindComponentTypeByName_WithNullOrEmpty_ShouldReturnNull()
        {
            // 行动 & 断言
            Assert.That(_componentSystem!.FindComponentTypeByName(null), Is.Null);
            Assert.That(_componentSystem.FindComponentTypeByName(""), Is.Null);
            Assert.That(_componentSystem.FindComponentTypeByName("   "), Is.Null);
        }

        #endregion
    }

    /// <summary>
    /// 带描述的测试组件
    /// </summary>
    [ComponentDescription("A test component with description")]
    public class DescribedTestComponent : Component
    {
        public string Description { get; set; } = "Test";
        public int Priority { get; set; } = 1;
    }

    /// <summary>
    /// 抽象测试组件（应该无法注册）
    /// </summary>
    public abstract class AbstractTestComponent : Component
    {
        public abstract void DoSomething();
    }

    /// <summary>
    /// 密封测试组件
    /// </summary>
    public sealed class SealedTestComponent : Component
    {
        public bool IsSealed { get; set; } = true;
    }

    /// <summary>
    /// 没有无参构造函数的组件（应该无法创建）
    /// </summary>
    public class ComponentWithoutDefaultConstructor : Component
    {
        public ComponentWithoutDefaultConstructor(string requiredParameter)
        {
            // 需要参数的构造函数
        }
    }
}