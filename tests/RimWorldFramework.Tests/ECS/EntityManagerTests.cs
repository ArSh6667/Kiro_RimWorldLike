using System;
using System.Linq;

namespace RimWorldFramework.Tests.ECS
{
    /// <summary>
    /// 实体管理器单元测试
    /// </summary>
    [TestFixture]
    public class EntityManagerTests : TestBase
    {
        private EntityManager? _entityManager;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _entityManager = new EntityManager();
        }

        [TearDown]
        public override void TearDown()
        {
            _entityManager = null;
            base.TearDown();
        }

        #region 实体创建和销毁测试

        [Test]
        public void CreateEntity_ShouldReturnValidEntityId()
        {
            // 行动
            var entityId = _entityManager!.CreateEntity();

            // 断言
            Assert.That(entityId, Is.Not.EqualTo(EntityId.Invalid));
            Assert.That(_entityManager.EntityExists(entityId), Is.True);
        }

        [Test]
        public void CreateEntity_ShouldReturnUniqueIds()
        {
            // 行动
            var entity1 = _entityManager!.CreateEntity();
            var entity2 = _entityManager.CreateEntity();
            var entity3 = _entityManager.CreateEntity();

            // 断言
            Assert.That(entity1, Is.Not.EqualTo(entity2));
            Assert.That(entity2, Is.Not.EqualTo(entity3));
            Assert.That(entity1, Is.Not.EqualTo(entity3));
        }

        [Test]
        public void CreateEntityGeneric_ShouldReturnCorrectType()
        {
            // 行动
            var entity = _entityManager!.CreateEntity<TestEntity>();

            // 断言
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity, Is.InstanceOf<TestEntity>());
            Assert.That(entity.Id, Is.Not.EqualTo(EntityId.Invalid));
            Assert.That(entity.IsActive, Is.True);
        }

        [Test]
        public void DestroyEntity_ShouldRemoveEntity()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            Assert.That(_entityManager.EntityExists(entityId), Is.True);

            // 行动
            _entityManager.DestroyEntity(entityId);

            // 断言
            Assert.That(_entityManager.EntityExists(entityId), Is.False);
            Assert.That(_entityManager.GetEntity(entityId), Is.Null);
        }

        [Test]
        public void DestroyEntity_WithInvalidId_ShouldNotThrow()
        {
            // 行动 & 断言
            AssertDoesNotThrow(() => _entityManager!.DestroyEntity(EntityId.Invalid));
            AssertDoesNotThrow(() => _entityManager.DestroyEntity(new EntityId(999)));
        }

        [Test]
        public void EntityIdRecycling_ShouldReuseDestroyedIds()
        {
            // 安排
            var entity1 = _entityManager!.CreateEntity();
            var entity2 = _entityManager.CreateEntity();
            
            // 行动：销毁第一个实体
            _entityManager.DestroyEntity(entity1);
            
            // 创建新实体应该重用ID
            var entity3 = _entityManager.CreateEntity();
            
            // 断言：新实体应该重用被销毁的ID
            Assert.That(entity3, Is.EqualTo(entity1));
            Assert.That(_entityManager.EntityExists(entity3), Is.True);
            Assert.That(_entityManager.EntityExists(entity2), Is.True);
        }

        #endregion

        #region 实体查询测试

        [Test]
        public void GetEntity_WithValidId_ShouldReturnEntity()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();

            // 行动
            var entity = _entityManager.GetEntity(entityId);

            // 断言
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity!.Id, Is.EqualTo(entityId));
            Assert.That(entity.IsActive, Is.True);
        }

        [Test]
        public void GetEntity_WithInvalidId_ShouldReturnNull()
        {
            // 行动
            var entity = _entityManager!.GetEntity(EntityId.Invalid);

            // 断言
            Assert.That(entity, Is.Null);
        }

        [Test]
        public void GetEntityGeneric_ShouldReturnCorrectType()
        {
            // 安排
            var testEntity = _entityManager!.CreateEntity<TestEntity>();

            // 行动
            var retrievedEntity = _entityManager.GetEntity<TestEntity>(testEntity.Id);

            // 断言
            Assert.That(retrievedEntity, Is.Not.Null);
            Assert.That(retrievedEntity, Is.InstanceOf<TestEntity>());
            Assert.That(retrievedEntity!.Id, Is.EqualTo(testEntity.Id));
        }

        [Test]
        public void GetAllEntities_ShouldReturnAllActiveEntities()
        {
            // 安排
            var entity1 = _entityManager!.CreateEntity();
            var entity2 = _entityManager.CreateEntity();
            var entity3 = _entityManager.CreateEntity();
            
            // 销毁一个实体
            _entityManager.DestroyEntity(entity2);

            // 行动
            var allEntities = _entityManager.GetAllEntities().ToList();

            // 断言
            Assert.That(allEntities.Count, Is.EqualTo(2));
            Assert.That(allEntities.Any(e => e.Id == entity1), Is.True);
            Assert.That(allEntities.Any(e => e.Id == entity3), Is.True);
            Assert.That(allEntities.Any(e => e.Id == entity2), Is.False);
        }

        [Test]
        public void GetEntitiesOfType_ShouldReturnCorrectEntities()
        {
            // 安排
            var testEntity1 = _entityManager!.CreateEntity<TestEntity>();
            var testEntity2 = _entityManager.CreateEntity<TestEntity>();
            var normalEntity = _entityManager.CreateEntity();

            // 行动
            var testEntities = _entityManager.GetEntitiesOfType<TestEntity>().ToList();

            // 断言
            Assert.That(testEntities.Count, Is.EqualTo(2));
            Assert.That(testEntities.Any(e => e.Id == testEntity1.Id), Is.True);
            Assert.That(testEntities.Any(e => e.Id == testEntity2.Id), Is.True);
        }

        #endregion

        #region 组件管理测试

        [Test]
        public void AddComponent_ShouldAddComponentToEntity()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            var component = new TestComponent { Value = 42 };

            // 行动
            _entityManager.AddComponent(entityId, component);

            // 断言
            Assert.That(_entityManager.HasComponent<TestComponent>(entityId), Is.True);
            Assert.That(component.EntityId, Is.EqualTo(entityId));
        }

        [Test]
        public void AddComponent_WithNullComponent_ShouldThrow()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();

            // 行动 & 断言
            AssertThrows<ArgumentNullException>(() => 
                _entityManager.AddComponent<TestComponent>(entityId, null!));
        }

        [Test]
        public void AddComponent_WithInvalidEntity_ShouldThrow()
        {
            // 安排
            var component = new TestComponent { Value = 42 };

            // 行动 & 断言
            AssertThrows<InvalidOperationException>(() => 
                _entityManager!.AddComponent(EntityId.Invalid, component));
        }

        [Test]
        public void GetComponent_ShouldReturnCorrectComponent()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            var component = new TestComponent { Value = 42 };
            _entityManager.AddComponent(entityId, component);

            // 行动
            var retrievedComponent = _entityManager.GetComponent<TestComponent>(entityId);

            // 断言
            Assert.That(retrievedComponent, Is.Not.Null);
            Assert.That(retrievedComponent!.Value, Is.EqualTo(42));
            Assert.That(retrievedComponent.EntityId, Is.EqualTo(entityId));
        }

        [Test]
        public void GetComponent_WithoutComponent_ShouldReturnNull()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();

            // 行动
            var component = _entityManager.GetComponent<TestComponent>(entityId);

            // 断言
            Assert.That(component, Is.Null);
        }

        [Test]
        public void RemoveComponent_ShouldRemoveComponent()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            var component = new TestComponent { Value = 42 };
            _entityManager.AddComponent(entityId, component);
            
            Assert.That(_entityManager.HasComponent<TestComponent>(entityId), Is.True);

            // 行动
            _entityManager.RemoveComponent<TestComponent>(entityId);

            // 断言
            Assert.That(_entityManager.HasComponent<TestComponent>(entityId), Is.False);
            Assert.That(_entityManager.GetComponent<TestComponent>(entityId), Is.Null);
        }

        [Test]
        public void GetAllComponents_ShouldReturnAllEntityComponents()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            var component1 = new TestComponent { Value = 42 };
            var component2 = new AnotherTestComponent { Name = "Test" };
            
            _entityManager.AddComponent(entityId, component1);
            _entityManager.AddComponent(entityId, component2);

            // 行动
            var allComponents = _entityManager.GetAllComponents(entityId).ToList();

            // 断言
            Assert.That(allComponents.Count, Is.EqualTo(2));
            Assert.That(allComponents.Any(c => c is TestComponent), Is.True);
            Assert.That(allComponents.Any(c => c is AnotherTestComponent), Is.True);
        }

        [Test]
        public void DestroyEntity_ShouldRemoveAllComponents()
        {
            // 安排
            var entityId = _entityManager!.CreateEntity();
            var component1 = new TestComponent { Value = 42 };
            var component2 = new AnotherTestComponent { Name = "Test" };
            
            _entityManager.AddComponent(entityId, component1);
            _entityManager.AddComponent(entityId, component2);

            // 行动
            _entityManager.DestroyEntity(entityId);

            // 断言
            Assert.That(_entityManager.EntityExists(entityId), Is.False);
            Assert.That(_entityManager.GetAllComponents(entityId).Count(), Is.EqualTo(0));
        }

        #endregion

        #region 边界情况测试

        [Test]
        public void MultipleOperations_ShouldMaintainConsistency()
        {
            // 创建多个实体和组件
            var entities = new EntityId[10];
            for (int i = 0; i < 10; i++)
            {
                entities[i] = _entityManager!.CreateEntity();
                _entityManager.AddComponent(entities[i], new TestComponent { Value = i });
            }

            // 销毁一些实体
            for (int i = 0; i < 5; i++)
            {
                _entityManager!.DestroyEntity(entities[i]);
            }

            // 验证剩余实体
            var remainingEntities = _entityManager!.GetAllEntities().ToList();
            Assert.That(remainingEntities.Count, Is.EqualTo(5));

            // 验证组件一致性
            foreach (var entity in remainingEntities)
            {
                Assert.That(_entityManager.HasComponent<TestComponent>(entity.Id), Is.True);
                var component = _entityManager.GetComponent<TestComponent>(entity.Id);
                Assert.That(component, Is.Not.Null);
                Assert.That(component!.EntityId, Is.EqualTo(entity.Id));
            }
        }

        #endregion
    }

    /// <summary>
    /// 测试用实体类
    /// </summary>
    public class TestEntity : Entity
    {
        public string Name { get; set; } = "TestEntity";
    }

    /// <summary>
    /// 测试用组件
    /// </summary>
    public class TestComponent : Component
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// 另一个测试用组件
    /// </summary>
    public class AnotherTestComponent : Component
    {
        public string Name { get; set; } = string.Empty;
    }
}