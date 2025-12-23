using System;
using System.Linq;
using NUnit.Framework;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Pathfinding;
using RimWorldFramework.Core.Common;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Tests.AI
{
    /// <summary>
    /// AI系统综合集成测试
    /// 验证角色系统、任务系统、路径寻找系统、协作系统的协同工作
    /// </summary>
    [TestFixture]
    public class AISystemIntegrationTests
    {
        private IEntityManager _entityManager = null!;
        private CharacterSystem _characterSystem = null!;
        private TaskSystem _taskSystem = null!;
        private PathfindingSystem _pathfindingSystem = null!;
        private CollaborationSystem _collaborationSystem = null!;
        private PathfindingGrid _pathfindingGrid = null!;

        [SetUp]
        public void Setup()
        {
            _entityManager = new EntityManager();
            _characterSystem = new CharacterSystem(_entityManager);
            _taskSystem = new TaskSystem();
            _pathfindingGrid = new PathfindingGrid(20, 20);
            _pathfindingSystem = new PathfindingSystem(_entityManager, _pathfindingGrid);
            _collaborationSystem = new CollaborationSystem(_taskSystem, _characterSystem);

            // 初始化所有系统
            _characterSystem.Initialize();
            _taskSystem.Initialize();
            _pathfindingSystem.Initialize();
            _collaborationSystem.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _collaborationSystem.Shutdown();
            _pathfindingSystem.Shutdown();
            _taskSystem.Shutdown();
            _characterSystem.Shutdown();
        }

        [Test]
        public void AISystemsIntegration_CompleteWorkflow_WorksCorrectly()
        {
            // Arrange - 创建测试场景
            var characters = CreateTestCharacters(3);
            var tasks = CreateTestTasks(2);

            // Act & Assert - 验证完整的AI工作流程

            // 1. 角色系统应该正确管理角色
            Assert.That(_characterSystem.GetAllCharacters().Count(), Is.EqualTo(3));
            
            foreach (var character in characters)
            {
                var retrievedCharacter = _characterSystem.GetCharacter(character.Id);
                Assert.That(retrievedCharacter, Is.Not.Null);
                Assert.That(retrievedCharacter.Name, Is.EqualTo(character.Name));
            }

            // 2. 任务系统应该正确管理任务
            Assert.That(_taskSystem.GetAllTasks().Count(), Is.EqualTo(2));
            Assert.That(_taskSystem.GetAvailableTasks().Count(), Is.EqualTo(2));

            // 3. 协作系统应该能够创建协作任务
            var collaborativeTaskResult = _collaborationSystem.AutoAssignCollaborativeTasks();
            Assert.That(collaborativeTaskResult, Is.Not.Null);

            // 4. 路径寻找系统应该能够为角色规划路径
            var character1 = characters[0];
            var pathResult = _pathfindingSystem.RequestPath(
                character1.Id, 
                new Vector3(0, 0, 0), 
                new Vector3(5, 5, 0)
            );
            Assert.That(pathResult, Is.True);

            // 5. 更新所有系统，验证协同工作
            for (int i = 0; i < 5; i++)
            {
                _pathfindingSystem.Update(0.1f);
                _characterSystem.Update(0.1f);
                _taskSystem.Update(0.1f);
                _collaborationSystem.Update(0.1f);
            }

            // 验证系统状态
            Assert.That(_pathfindingSystem.IsPathfinding(character1.Id), Is.True);
        }

        [Test]
        public void TaskAssignmentWorkflow_WithPathfinding_WorksCorrectly()
        {
            // Arrange
            var character = CreateTestCharacter("Worker", new Vector3(0, 0, 0));
            var taskId = _taskSystem.CreateConstructionTask("Build Wall", new Vector3(10, 10, 0), 3);

            // Act - 分配任务并验证路径寻找
            var assignmentResult = _taskSystem.AssignTaskToCharacter(character);
            
            if (assignmentResult.IsSuccess)
            {
                var task = assignmentResult.AssignedTask;
                Assert.That(task, Is.Not.Null);

                // 角色应该开始寻路到任务位置
                if (task!.Definition.TargetPosition.HasValue)
                {
                    var pathRequested = _pathfindingSystem.RequestPath(
                        character.Id,
                        character.Position!.Position,
                        task.Definition.TargetPosition.Value
                    );
                    Assert.That(pathRequested, Is.True);
                }
            }

            // 更新系统
            _pathfindingSystem.Update(0.1f);
            _taskSystem.Update(0.1f);
            _characterSystem.Update(0.1f);

            // Assert - 验证任务分配和路径寻找协同工作
            Assert.That(assignmentResult.IsSuccess || assignmentResult.Message.Contains("没有适合的任务"), Is.True);
        }

        [Test]
        public void CollaborativeTaskWorkflow_WithMultipleCharacters_WorksCorrectly()
        {
            // Arrange
            var characters = CreateTestCharacters(3);
            
            // 设置不同的技能水平
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].Skills != null)
                {
                    characters[i].Skills.SetSkillLevel(SkillType.Construction, 5 + i * 2);
                }
            }

            // 创建协作任务
            var taskDefinition = new TaskDefinition
            {
                Name = "Build Complex Structure",
                Type = TaskType.Construction,
                Priority = TaskPriority.High,
                MaxAssignedCharacters = 3,
                EstimatedDuration = 20.0f,
                TargetPosition = new Vector3(15, 15, 0),
                WorkRadius = 3.0f
            };

            var collaborationResult = _collaborationSystem.CreateCollaborativeTask(taskDefinition, CollaborationType.Construction);

            // Act - 自动分配协作任务
            var coordinationResult = _collaborationSystem.AutoAssignCollaborativeTasks();

            // Assert - 验证协作任务分配
            Assert.That(collaborationResult.IsSuccess, Is.True);
            Assert.That(coordinationResult, Is.Not.Null);
            
            if (coordinationResult.Assignments.Any())
            {
                // 验证角色被分配到协作任务
                var assignedCharacters = coordinationResult.Assignments.Select(a => a.CharacterId).Distinct();
                Assert.That(assignedCharacters.Count(), Is.GreaterThan(0));
                
                // 验证有领导者角色
                var hasLeader = coordinationResult.Assignments.Any(a => a.Role == CollaborationRole.Leader);
                Assert.That(hasLeader, Is.True);
            }
        }

        [Test]
        public void PathfindingWithObstacles_AndTaskReassignment_WorksCorrectly()
        {
            // Arrange
            var character = CreateTestCharacter("Navigator", new Vector3(0, 0, 0));
            var destination = new Vector3(10, 0, 0);

            // 创建障碍物
            _pathfindingSystem.SetTerrainType(new Vector3(5, 0, 0), TerrainType.Blocked);
            _pathfindingSystem.SetTerrainType(new Vector3(6, 0, 0), TerrainType.Blocked);

            // Act - 请求路径
            var pathResult = _pathfindingSystem.RequestPath(character.Id, character.Position!.Position, destination);
            Assert.That(pathResult, Is.True);

            // 更新路径寻找系统
            _pathfindingSystem.Update(0.1f);

            // 添加动态障碍物，触发路径重规划
            _pathfindingSystem.SetDynamicObstacle(new Vector3(7, 1, 0), true);
            _pathfindingSystem.Update(0.1f);

            // Assert - 验证路径重规划
            var currentPath = _pathfindingSystem.GetCurrentPath(character.Id);
            if (currentPath != null)
            {
                // 路径应该避开障碍物
                foreach (var point in currentPath)
                {
                    var gridPos = _pathfindingGrid.WorldToGrid(point);
                    var node = _pathfindingGrid.GetNode(gridPos.x, gridPos.y);
                    Assert.That(node?.IsWalkable(), Is.True);
                }
            }
        }

        [Test]
        public void ResourceConflictResolution_WithMultipleCharacters_WorksCorrectly()
        {
            // Arrange
            var characters = CreateTestCharacters(2);
            var workPosition = new Vector3(8, 8, 0);

            // Act - 两个角色尝试预订同一工作区域
            var reservation1 = _collaborationSystem.ReserveWorkArea(workPosition, characters[0].Id, 60f);
            var reservation2 = _collaborationSystem.ReserveWorkArea(workPosition, characters[1].Id, 60f);

            // Assert - 验证资源冲突解决
            Assert.That(reservation1.IsSuccess, Is.True);
            Assert.That(reservation2.IsSuccess, Is.False);

            // 验证冲突检测
            var conflictResult = _collaborationSystem.CheckPositionConflict(workPosition, characters[1].Id, 2.0f);
            Assert.That(conflictResult.HasConflict, Is.True);
            Assert.That(conflictResult.ConflictingReservations.Count, Is.EqualTo(1));
        }

        [Test]
        public void BehaviorTreeExecution_WithTaskAssignment_WorksCorrectly()
        {
            // Arrange
            var character = CreateTestCharacter("AI Character", new Vector3(2, 2, 0));
            
            // 确保角色有行为树
            var behaviorStatus = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(behaviorStatus, Is.Not.Null);

            // 创建任务
            var taskId = _taskSystem.CreateSimpleTask("Test Task", TaskType.Construction, TaskPriority.Normal);
            var task = _taskSystem.GetTask(taskId);
            Assert.That(task, Is.Not.Null);

            // Act - 更新角色系统，让行为树执行
            for (int i = 0; i < 10; i++)
            {
                _characterSystem.Update(0.1f);
                _taskSystem.Update(0.1f);
            }

            // Assert - 验证行为树正常执行
            var updatedStatus = _characterSystem.GetCharacterBehaviorStatus(character.Id);
            Assert.That(updatedStatus, Is.Not.Null);
        }

        [Test]
        public void SystemPerformance_WithMultipleEntities_MaintainsStability()
        {
            // Arrange - 创建大量实体测试性能
            var characters = CreateTestCharacters(10);
            var tasks = CreateTestTasks(5);

            // Act - 执行多次更新循环
            var startTime = DateTime.Now;
            
            for (int i = 0; i < 100; i++)
            {
                _characterSystem.Update(0.016f); // 60 FPS
                _taskSystem.Update(0.016f);
                _pathfindingSystem.Update(0.016f);
                _collaborationSystem.Update(0.016f);
            }

            var endTime = DateTime.Now;
            var totalTime = endTime - startTime;

            // Assert - 验证性能稳定性
            Assert.That(totalTime.TotalSeconds, Is.LessThan(5.0), "系统更新应该在合理时间内完成");
            
            // 验证所有系统仍然正常工作
            Assert.That(_characterSystem.GetAllCharacters().Count(), Is.EqualTo(10));
            Assert.That(_taskSystem.GetAllTasks().Count(), Is.EqualTo(5));
        }

        [Test]
        public void ErrorHandling_WithInvalidOperations_HandlesGracefully()
        {
            // Arrange & Act & Assert - 测试各种错误情况的处理

            // 1. 无效角色ID的路径请求
            var invalidPathResult = _pathfindingSystem.RequestPath(999, Vector3.Zero, Vector3.One);
            Assert.That(invalidPathResult, Is.False);

            // 2. 无效任务ID的协作加入
            var invalidCollabResult = _collaborationSystem.JoinCollaboration(new TaskId(999), 1, CollaborationRole.Worker);
            Assert.That(invalidCollabResult.IsSuccess, Is.False);

            // 3. 空角色列表的任务分配
            var emptyAssignmentResult = _taskSystem.AssignTasksToCharacters(new CharacterEntity[0]);
            Assert.That(emptyAssignmentResult, Is.Not.Null);
            Assert.That(emptyAssignmentResult.Count, Is.EqualTo(0));

            // 4. 系统在错误后仍能正常工作
            var character = CreateTestCharacter("Test", Vector3.Zero);
            var validResult = _characterSystem.GetCharacter(character.Id);
            Assert.That(validResult, Is.Not.Null);
        }

        #region Helper Methods

        private CharacterEntity CreateTestCharacter(string name, Vector3 position)
        {
            var character = new CharacterEntity(_entityManager.CreateEntity())
            {
                Name = name
            };

            var positionComponent = new PositionComponent(position);
            var skillComponent = new SkillComponent();
            var needComponent = new NeedComponent();
            var inventoryComponent = new InventoryComponent();

            _entityManager.AddComponent(character.Id, positionComponent);
            _entityManager.AddComponent(character.Id, skillComponent);
            _entityManager.AddComponent(character.Id, needComponent);
            _entityManager.AddComponent(character.Id, inventoryComponent);

            character.SetComponentReferences(_entityManager);

            // 设置基本技能
            if (character.Skills != null)
            {
                character.Skills.SetSkillLevel(SkillType.Construction, 5);
                character.Skills.SetSkillLevel(SkillType.Mining, 3);
                character.Skills.SetSkillLevel(SkillType.Research, 2);
            }

            _characterSystem.RegisterCharacter(character);
            return character;
        }

        private System.Collections.Generic.List<CharacterEntity> CreateTestCharacters(int count)
        {
            var characters = new System.Collections.Generic.List<CharacterEntity>();
            
            for (int i = 0; i < count; i++)
            {
                var position = new Vector3(i * 2, i * 2, 0);
                var character = CreateTestCharacter($"Character_{i}", position);
                characters.Add(character);
            }

            return characters;
        }

        private System.Collections.Generic.List<TaskId> CreateTestTasks(int count)
        {
            var taskIds = new System.Collections.Generic.List<TaskId>();

            for (int i = 0; i < count; i++)
            {
                var taskId = _taskSystem.CreateSimpleTask(
                    $"Task_{i}", 
                    TaskType.Construction, 
                    TaskPriority.Normal
                );
                taskIds.Add(taskId);
            }

            return taskIds;
        }

        #endregion
    }
}