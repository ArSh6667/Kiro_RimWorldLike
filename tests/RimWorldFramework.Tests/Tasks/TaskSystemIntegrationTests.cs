using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Tasks
{
    /// <summary>
    /// 任务系统集成测试
    /// </summary>
    [TestFixture]
    public class TaskSystemIntegrationTests : TestBase
    {
        private TaskSystem _taskSystem;
        private EntityManager _entityManager;

        [SetUp]
        public void SetUp()
        {
            _entityManager = new EntityManager();
            _taskSystem = new TaskSystem();
            _taskSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _taskSystem.Shutdown();
        }

        [Test]
        public void CreateTask_ShouldCreateValidTask()
        {
            // Arrange
            var definition = new TaskDefinition
            {
                Name = "测试任务",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 5.0f
            };

            // Act
            var taskId = _taskSystem.CreateTask(definition);
            var task = _taskSystem.GetTask(taskId);

            // Assert
            Assert.That(task, Is.Not.Null);
            Assert.That(task.Definition.Name, Is.EqualTo("测试任务"));
            Assert.That(task.Status, Is.EqualTo(TaskStatus.Available));
        }

        [Test]
        public void CreateTaskWithDependencies_ShouldRespectDependencies()
        {
            // Arrange
            var prerequisiteTask = new TaskDefinition
            {
                Name = "前置任务",
                Type = TaskType.Mining,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 3.0f
            };

            var mainTask = new TaskDefinition
            {
                Name = "主任务",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 5.0f
            };

            // Act
            var prerequisiteId = _taskSystem.CreateTask(prerequisiteTask);
            mainTask.AddPrerequisite(prerequisiteId);
            var mainTaskId = _taskSystem.CreateTask(mainTask);

            var prerequisite = _taskSystem.GetTask(prerequisiteId);
            var main = _taskSystem.GetTask(mainTaskId);

            // Assert
            Assert.That(prerequisite?.Status, Is.EqualTo(TaskStatus.Available));
            Assert.That(main?.Status, Is.EqualTo(TaskStatus.Pending));
        }

        [Test]
        public void CompletePrerequisiteTask_ShouldActivateDependentTask()
        {
            // Arrange
            var prerequisiteTask = new TaskDefinition
            {
                Name = "前置任务",
                Type = TaskType.Mining,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 3.0f
            };

            var mainTask = new TaskDefinition
            {
                Name = "主任务",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 5.0f
            };

            var prerequisiteId = _taskSystem.CreateTask(prerequisiteTask);
            mainTask.AddPrerequisite(prerequisiteId);
            var mainTaskId = _taskSystem.CreateTask(mainTask);

            var prerequisite = _taskSystem.GetTask(prerequisiteId);
            var main = _taskSystem.GetTask(mainTaskId);

            // Act
            prerequisite?.AssignCharacter(1);
            prerequisite?.Start();
            prerequisite?.Complete();

            // Assert
            Assert.That(prerequisite?.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(main?.Status, Is.EqualTo(TaskStatus.Available));
        }

        [Test]
        public void AssignTaskToCharacter_ShouldWorkWithSuitableCharacter()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _entityManager.AddComponent(character.Id, new SkillComponent());
            character.SetComponentReferences(_entityManager);
            
            // 设置建造技能
            character.Skills?.SetSkillLevel(SkillType.Construction, 5);

            var taskId = _taskSystem.CreateConstructionTask("建造墙壁", new Vector3(10, 10, 0), 3);

            // Act
            var result = _taskSystem.AssignTaskToCharacter(character);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.AssignedTask, Is.Not.Null);
            Assert.That(result.AssignedTask?.AssignedCharacters, Contains.Item(character.Id));
        }

        [Test]
        public void AssignTaskToCharacter_ShouldFailWithUnsuitableCharacter()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _entityManager.AddComponent(character.Id, new SkillComponent());
            character.SetComponentReferences(_entityManager);
            
            // 设置低建造技能
            character.Skills?.SetSkillLevel(SkillType.Construction, 1);

            var taskId = _taskSystem.CreateConstructionTask("建造墙壁", new Vector3(10, 10, 0), 10); // 需要高技能

            // Act
            var result = _taskSystem.AssignTaskToCharacter(character);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void TaskExecution_ShouldUpdateProgress()
        {
            // Arrange
            var definition = new TaskDefinition
            {
                Name = "测试任务",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 1.0f // 短时间便于测试
            };

            var taskId = _taskSystem.CreateTask(definition);
            var task = _taskSystem.GetTask(taskId);

            // Act
            task?.AssignCharacter(1);
            task?.Start();
            
            // 模拟多次更新
            for (int i = 0; i < 10; i++)
            {
                task?.Update(0.2f);
            }

            // Assert
            Assert.That(task?.Status, Is.EqualTo(TaskStatus.Completed));
            Assert.That(task?.Progress, Is.EqualTo(1.0f));
        }

        [Test]
        public void GetTaskRecommendations_ShouldReturnSuitableTasks()
        {
            // Arrange
            var character = new CharacterEntity("测试角色");
            _entityManager.AddComponent(character.Id, new SkillComponent());
            character.SetComponentReferences(_entityManager);
            
            character.Skills?.SetSkillLevel(SkillType.Construction, 8);
            character.Skills?.SetSkillLevel(SkillType.Mining, 3);

            // 创建多个任务
            _taskSystem.CreateConstructionTask("建造任务1", new Vector3(5, 5, 0), 5);
            _taskSystem.CreateConstructionTask("建造任务2", new Vector3(15, 15, 0), 10);
            _taskSystem.CreateMiningTask("挖掘任务", new Vector3(20, 20, 0), 2);

            // Act
            var recommendations = _taskSystem.GetTaskRecommendations(character, 5);

            // Assert
            Assert.That(recommendations.Count, Is.GreaterThan(0));
            Assert.That(recommendations.All(r => r.Score > 0), Is.True);
            
            // 建造任务应该有更高的分数（因为角色建造技能更高）
            var constructionRecs = recommendations.Where(r => r.Task.Definition.Type == TaskType.Construction).ToList();
            var miningRecs = recommendations.Where(r => r.Task.Definition.Type == TaskType.Mining).ToList();
            
            if (constructionRecs.Any() && miningRecs.Any())
            {
                Assert.That(constructionRecs.Max(r => r.Score), Is.GreaterThan(miningRecs.Max(r => r.Score)));
            }
        }

        [Test]
        public void TaskSystem_ShouldProvideAccurateStats()
        {
            // Arrange
            _taskSystem.CreateSimpleTask("任务1", TaskType.Construction, TaskPriority.High);
            _taskSystem.CreateSimpleTask("任务2", TaskType.Mining, TaskPriority.Normal);
            _taskSystem.CreateSimpleTask("任务3", TaskType.Research, TaskPriority.Low);

            // Act
            var stats = _taskSystem.GetStats();

            // Assert
            Assert.That(stats.TotalTasks, Is.EqualTo(3));
            Assert.That(stats.AvailableTasks, Is.EqualTo(3));
            Assert.That(stats.TasksByType.Count, Is.GreaterThan(0));
            Assert.That(stats.TasksByPriority.Count, Is.GreaterThan(0));
        }

        [Test]
        public void CancelTask_ShouldUpdateTaskStatus()
        {
            // Arrange
            var taskId = _taskSystem.CreateSimpleTask("测试任务", TaskType.Construction);
            var task = _taskSystem.GetTask(taskId);

            // Act
            var cancelled = _taskSystem.CancelTask(taskId);

            // Assert
            Assert.That(cancelled, Is.True);
            Assert.That(task?.Status, Is.EqualTo(TaskStatus.Cancelled));
        }

        [Test]
        public void RemoveTask_ShouldRemoveTaskFromSystem()
        {
            // Arrange
            var taskId = _taskSystem.CreateSimpleTask("测试任务", TaskType.Construction);

            // Act
            var removed = _taskSystem.RemoveTask(taskId);
            var task = _taskSystem.GetTask(taskId);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(task, Is.Null);
        }

        [Test]
        public void TaskValidation_ShouldRejectInvalidTasks()
        {
            // Arrange
            var invalidDefinition = new TaskDefinition
            {
                Name = "", // 空名称
                Type = TaskType.Construction,
                EstimatedDuration = -1.0f // 负持续时间
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _taskSystem.CreateTask(invalidDefinition));
        }

        [Test]
        public void MultipleCharacterAssignment_ShouldRespectMaxLimit()
        {
            // Arrange
            var definition = new TaskDefinition
            {
                Name = "多人任务",
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                EstimatedDuration = 5.0f,
                MaxAssignedCharacters = 2
            };

            var taskId = _taskSystem.CreateTask(definition);
            var task = _taskSystem.GetTask(taskId);

            // Act
            var assigned1 = task?.AssignCharacter(1);
            var assigned2 = task?.AssignCharacter(2);
            var assigned3 = task?.AssignCharacter(3); // 应该失败

            // Assert
            Assert.That(assigned1, Is.True);
            Assert.That(assigned2, Is.True);
            Assert.That(assigned3, Is.False);
            Assert.That(task?.AssignedCharacters.Count, Is.EqualTo(2));
        }
    }
}