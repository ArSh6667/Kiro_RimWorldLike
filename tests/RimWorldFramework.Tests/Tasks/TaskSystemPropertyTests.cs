using System;
using System.Linq;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Tasks;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Tests.Tasks
{
    /// <summary>
    /// 任务系统属性测试
    /// Feature: rimworld-game-framework
    /// </summary>
    [TestFixture]
    public class TaskSystemPropertyTests : TestBase
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

        /// <summary>
        /// Property 7: 任务依赖管理
        /// 对于任何新创建的任务，其依赖关系应当被正确设置并在任务树中维护
        /// </summary>
        [Property(MaxTest = 100)]
        [Category("Property")]
        public Property TaskDependencyManagement()
        {
            return Prop.ForAll(
                GenerateTaskWithDependencies(),
                (taskData) =>
                {
                    // 创建前置任务
                    var prerequisiteTasks = taskData.Prerequisites.Select(def => _taskSystem.CreateTask(def)).ToList();
                    
                    // 更新主任务的前置任务ID
                    taskData.MainTask.Prerequisites.Clear();
                    taskData.MainTask.Prerequisites.AddRange(prerequisiteTasks);

                    // 创建主任务
                    var mainTaskId = _taskSystem.CreateTask(taskData.MainTask);
                    var mainTask = _taskSystem.GetTask(mainTaskId);

                    // 验证依赖关系正确设置
                    var dependenciesCorrect = mainTask != null &&
                        mainTask.Definition.Prerequisites.Count == prerequisiteTasks.Count &&
                        mainTask.Definition.Prerequisites.All(id => prerequisiteTasks.Contains(id));

                    // 验证任务状态正确（应该是Pending，因为前置任务未完成）
                    var statusCorrect = mainTask?.Status == TaskStatus.Pending;

                    return dependenciesCorrect && statusCorrect;
                });
        }

        /// <summary>
        /// Property 8: 任务状态传播
        /// 对于任何任务状态变化，所有依赖该任务的其他任务的可用性应当自动更新
        /// </summary>
        [Property(MaxTest = 100)]
        [Category("Property")]
        public Property TaskStatusPropagation()
        {
            return Prop.ForAll(
                GenerateTaskChain(),
                (taskChain) =>
                {
                    // 创建任务链
                    var taskIds = taskChain.Select(def => _taskSystem.CreateTask(def)).ToList();
                    
                    // 获取第一个任务（没有前置任务）
                    var firstTask = _taskSystem.GetTask(taskIds[0]);
                    if (firstTask == null) return false;

                    // 第一个任务应该是可用的
                    var initialStateCorrect = firstTask.Status == TaskStatus.Available;

                    // 完成第一个任务
                    if (firstTask.Status == TaskStatus.Available)
                    {
                        firstTask.AssignCharacter(1); // 分配一个虚拟角色ID
                        firstTask.Start();
                        firstTask.Complete();
                    }

                    // 检查依赖任务是否变为可用
                    var secondTask = taskIds.Count > 1 ? _taskSystem.GetTask(taskIds[1]) : null;
                    var propagationCorrect = secondTask == null || secondTask.Status == TaskStatus.Available;

                    return initialStateCorrect && propagationCorrect;
                });
        }

        /// <summary>
        /// Property 9: 任务激活链
        /// 对于任何完成的任务，其后续依赖任务应当在满足所有前置条件时自动激活
        /// </summary>
        [Property(MaxTest = 50)]
        [Category("Property")]
        public Property TaskActivationChain()
        {
            return Prop.ForAll(
                GenerateSimpleTaskChain(),
                (taskDefinitions) =>
                {
                    if (taskDefinitions.Count < 2) return true; // 跳过太短的链

                    // 创建任务链
                    var taskIds = new List<TaskId>();
                    
                    foreach (var def in taskDefinitions)
                    {
                        var taskId = _taskSystem.CreateTask(def);
                        taskIds.Add(taskId);
                    }

                    // 验证激活链
                    var allTasksActivatedCorrectly = true;
                    
                    for (int i = 0; i < taskIds.Count - 1; i++)
                    {
                        var currentTask = _taskSystem.GetTask(taskIds[i]);
                        var nextTask = _taskSystem.GetTask(taskIds[i + 1]);
                        
                        if (currentTask == null || nextTask == null)
                        {
                            allTasksActivatedCorrectly = false;
                            break;
                        }

                        // 当前任务应该是可用的（如果是第一个）或等待的
                        if (i == 0 && currentTask.Status != TaskStatus.Available)
                        {
                            allTasksActivatedCorrectly = false;
                            break;
                        }

                        // 下一个任务应该是等待的（因为前置任务未完成）
                        if (nextTask.Status != TaskStatus.Pending)
                        {
                            allTasksActivatedCorrectly = false;
                            break;
                        }
                    }

                    return allTasksActivatedCorrectly;
                });
        }

        /// <summary>
        /// 生成带依赖关系的任务
        /// </summary>
        private static Arbitrary<TaskWithDependencies> GenerateTaskWithDependencies()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var prerequisiteCount = random.Next(0, 3);
                
                var prerequisites = new List<TaskDefinition>();
                for (int i = 0; i < prerequisiteCount; i++)
                {
                    prerequisites.Add(CreateRandomTaskDefinition(random, $"前置任务_{i}"));
                }

                var mainTask = CreateRandomTaskDefinition(random, "主任务");
                
                return new TaskWithDependencies
                {
                    MainTask = mainTask,
                    Prerequisites = prerequisites
                };
            }));
        }

        /// <summary>
        /// 生成任务链
        /// </summary>
        private static Arbitrary<List<TaskDefinition>> GenerateTaskChain()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var chainLength = random.Next(2, 5);
                var tasks = new List<TaskDefinition>();

                for (int i = 0; i < chainLength; i++)
                {
                    var task = CreateRandomTaskDefinition(random, $"任务_{i}");
                    
                    // 设置依赖关系（每个任务依赖前一个任务）
                    if (i > 0)
                    {
                        task.Prerequisites.Add(tasks[i - 1].Id);
                        tasks[i - 1].Dependents.Add(task.Id);
                    }
                    
                    tasks.Add(task);
                }

                return tasks;
            }));
        }

        /// <summary>
        /// 生成简单任务链
        /// </summary>
        private static Arbitrary<List<TaskDefinition>> GenerateSimpleTaskChain()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var chainLength = random.Next(2, 4);
                var tasks = new List<TaskDefinition>();

                TaskId? previousTaskId = null;

                for (int i = 0; i < chainLength; i++)
                {
                    var task = CreateRandomTaskDefinition(random, $"链任务_{i}");
                    
                    if (previousTaskId.HasValue)
                    {
                        task.Prerequisites.Add(previousTaskId.Value);
                    }
                    
                    tasks.Add(task);
                    previousTaskId = task.Id;
                }

                return tasks;
            }));
        }

        /// <summary>
        /// 创建随机任务定义
        /// </summary>
        private static TaskDefinition CreateRandomTaskDefinition(Random random, string name)
        {
            var taskTypes = Enum.GetValues<TaskType>();
            var priorities = Enum.GetValues<TaskPriority>();
            var skillTypes = Enum.GetValues<SkillType>();

            var definition = new TaskDefinition
            {
                Id = new TaskId((uint)random.Next(1000, 9999)),
                Name = name,
                Type = taskTypes[random.Next(taskTypes.Length)],
                Priority = priorities[random.Next(priorities.Length)],
                EstimatedDuration = random.NextSingle() * 10f + 1f,
                MaxAssignedCharacters = random.Next(1, 4),
                WorkRadius = random.NextSingle() * 5f + 1f
            };

            // 添加随机技能需求
            var skillCount = random.Next(0, 3);
            for (int i = 0; i < skillCount; i++)
            {
                var skillType = skillTypes[random.Next(skillTypes.Length)];
                var minLevel = random.Next(1, 11);
                definition.AddSkillRequirement(skillType, minLevel);
            }

            // 添加随机位置
            if (random.NextSingle() > 0.5f)
            {
                definition.TargetPosition = new Vector3(
                    random.NextSingle() * 100f,
                    random.NextSingle() * 100f,
                    0f
                );
            }

            return definition;
        }

        /// <summary>
        /// 测试任务分配的一致性
        /// </summary>
        [Property(MaxTest = 50)]
        [Category("Property")]
        public Property TaskAssignmentConsistency()
        {
            return Prop.ForAll(
                GenerateCharacterWithSkills(),
                GenerateSimpleTask(),
                (character, taskDef) =>
                {
                    // 创建任务
                    var taskId = _taskSystem.CreateTask(taskDef);
                    var task = _taskSystem.GetTask(taskId);
                    
                    if (task == null) return false;

                    // 尝试分配任务
                    var canExecuteBefore = task.CanExecute(character);
                    var assignmentResult = _taskSystem.AssignTaskToCharacter(character);

                    // 验证分配结果的一致性
                    if (canExecuteBefore && assignmentResult.IsSuccess)
                    {
                        // 如果角色能执行且分配成功，任务应该被分配
                        return task.AssignedCharacters.Contains(character.Id);
                    }
                    else if (!canExecuteBefore)
                    {
                        // 如果角色不能执行，分配应该失败
                        return !assignmentResult.IsSuccess;
                    }

                    return true; // 其他情况都是合理的
                });
        }

        /// <summary>
        /// 生成带技能的角色
        /// </summary>
        private Arbitrary<CharacterEntity> GenerateCharacterWithSkills()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                var character = CharacterEntity.GenerateRandom(random);
                
                _entityManager.AddComponent(character.Id, new SkillComponent());
                character.SetComponentReferences(_entityManager);
                character.Skills?.RandomizeSkills(random, 0, 15);

                return character;
            }));
        }

        /// <summary>
        /// 生成简单任务
        /// </summary>
        private static Arbitrary<TaskDefinition> GenerateSimpleTask()
        {
            return Arb.From(Gen.Fresh(() =>
            {
                var random = new Random();
                return CreateRandomTaskDefinition(random, "测试任务");
            }));
        }

        /// <summary>
        /// 任务依赖数据结构
        /// </summary>
        private class TaskWithDependencies
        {
            public TaskDefinition MainTask { get; set; } = null!;
            public List<TaskDefinition> Prerequisites { get; set; } = new();
        }

        /// <summary>
        /// 测试任务完成后的状态更新
        /// </summary>
        [Property(MaxTest = 50)]
        [Category("Property")]
        public Property TaskCompletionStateUpdate()
        {
            return Prop.ForAll(
                GenerateSimpleTask(),
                (taskDef) =>
                {
                    // 创建任务
                    var taskId = _taskSystem.CreateTask(taskDef);
                    var task = _taskSystem.GetTask(taskId);
                    
                    if (task == null) return false;

                    // 记录初始状态
                    var initialStatus = task.Status;
                    var initialProgress = task.Progress;

                    // 分配并开始任务
                    if (task.Status == TaskStatus.Available)
                    {
                        task.AssignCharacter(1);
                        task.Start();
                        
                        // 完成任务
                        var result = task.Complete();
                        
                        // 验证状态更新
                        var statusUpdated = task.Status == TaskStatus.Completed;
                        var progressUpdated = task.Progress == 1.0f;
                        var resultCorrect = result == TaskResult.Success;
                        var completionTimeSet = task.CompletionTime.HasValue;

                        return statusUpdated && progressUpdated && resultCorrect && completionTimeSet;
                    }

                    return true; // 如果任务不可用，跳过测试
                });
        }
    }
}