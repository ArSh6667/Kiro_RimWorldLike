using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Systems;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务系统 - 管理游戏中的所有任务
    /// </summary>
    public class TaskSystem : IGameSystem
    {
        private readonly TaskManager _taskManager;
        private readonly TaskAssigner _taskAssigner;
        private readonly List<ITaskFactory> _taskFactories = new();

        /// <summary>
        /// 获取任务管理器（用于协作系统）
        /// </summary>
        internal TaskManager TaskManager => _taskManager;

        public int Priority => 90; // 在角色系统之后执行

        public TaskSystem()
        {
            _taskManager = new TaskManager();
            _taskAssigner = new TaskAssigner(_taskManager);
            
            // 注册默认任务工厂
            RegisterDefaultFactories();
        }

        public void Initialize()
        {
            Console.WriteLine("任务系统已初始化");
        }

        public void Update(float deltaTime)
        {
            // 更新所有任务
            _taskManager.UpdateTasks(deltaTime);
        }

        public void Shutdown()
        {
            Console.WriteLine("任务系统已关闭");
        }

        /// <summary>
        /// 注册任务工厂
        /// </summary>
        public void RegisterTaskFactory(ITaskFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _taskFactories.Add(factory);
            _taskManager.RegisterTaskFactory(factory);
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        public TaskId CreateTask(TaskDefinition definition)
        {
            return _taskManager.CreateTask(definition);
        }

        /// <summary>
        /// 创建简单任务
        /// </summary>
        public TaskId CreateSimpleTask(string name, TaskType type, TaskPriority priority = TaskPriority.Normal)
        {
            var definition = new TaskDefinition
            {
                Name = name,
                Type = type,
                Priority = priority,
                EstimatedDuration = 5.0f
            };

            return CreateTask(definition);
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ITask? GetTask(TaskId taskId)
        {
            return _taskManager.GetTask(taskId);
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public IEnumerable<ITask> GetAllTasks()
        {
            return _taskManager.GetAllTasks();
        }

        /// <summary>
        /// 获取可用任务
        /// </summary>
        public IEnumerable<ITask> GetAvailableTasks()
        {
            return _taskManager.GetAvailableTasks();
        }

        /// <summary>
        /// 为角色分配任务
        /// </summary>
        public TaskAssignmentResult AssignTaskToCharacter(CharacterEntity character)
        {
            return _taskAssigner.AssignBestTask(character);
        }

        /// <summary>
        /// 为多个角色分配任务
        /// </summary>
        public List<TaskAssignmentResult> AssignTasksToCharacters(IEnumerable<CharacterEntity> characters)
        {
            return _taskAssigner.AssignTasks(characters);
        }

        /// <summary>
        /// 手动分配任务
        /// </summary>
        public bool AssignTask(TaskId taskId, uint characterId)
        {
            return _taskManager.AssignTask(taskId, characterId);
        }

        /// <summary>
        /// 取消任务分配
        /// </summary>
        public bool UnassignTask(TaskId taskId, uint characterId)
        {
            return _taskManager.UnassignTask(taskId, characterId);
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public bool CompleteTask(TaskId taskId)
        {
            return _taskManager.CompleteTask(taskId);
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool CancelTask(TaskId taskId)
        {
            return _taskManager.CancelTask(taskId);
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        public bool RemoveTask(TaskId taskId)
        {
            return _taskManager.RemoveTask(taskId);
        }

        /// <summary>
        /// 获取角色的任务推荐
        /// </summary>
        public List<TaskRecommendation> GetTaskRecommendations(CharacterEntity character, int maxRecommendations = 5)
        {
            return _taskAssigner.GetTaskRecommendations(character, maxRecommendations);
        }

        /// <summary>
        /// 重新分配所有任务
        /// </summary>
        public TaskReassignmentResult ReassignAllTasks(IEnumerable<CharacterEntity> characters)
        {
            return _taskAssigner.ReassignAllTasks(characters);
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public TaskSystemStats GetStats()
        {
            var managerStats = _taskManager.GetStats();
            var tasks = _taskManager.GetAllTasks().ToList();

            return new TaskSystemStats
            {
                TotalTasks = managerStats.TotalTasks,
                PendingTasks = managerStats.PendingTasks,
                AvailableTasks = managerStats.AvailableTasks,
                AssignedTasks = managerStats.AssignedTasks,
                InProgressTasks = managerStats.InProgressTasks,
                CompletedTasks = managerStats.CompletedTasks,
                FailedTasks = managerStats.FailedTasks,
                CancelledTasks = managerStats.CancelledTasks,
                RegisteredFactories = _taskFactories.Count,
                AverageProgress = tasks.Any() ? tasks.Average(t => t.Progress) : 0f,
                TasksByType = tasks.GroupBy(t => t.Definition.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TasksByPriority = tasks.GroupBy(t => t.Definition.Priority)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        /// <summary>
        /// 创建建造任务
        /// </summary>
        public TaskId CreateConstructionTask(string name, RimWorldFramework.Core.Common.Vector3 position, int requiredSkillLevel = 3)
        {
            var definition = new TaskDefinition
            {
                Name = name,
                Type = TaskType.Construction,
                Priority = TaskPriority.Normal,
                TargetPosition = position,
                WorkRadius = 2.0f,
                EstimatedDuration = 10.0f,
                MaxAssignedCharacters = 2
            };

            definition.AddSkillRequirement(RimWorldFramework.Core.Characters.Components.SkillType.Construction, requiredSkillLevel);
            return CreateTask(definition);
        }

        /// <summary>
        /// 创建挖掘任务
        /// </summary>
        public TaskId CreateMiningTask(string name, RimWorldFramework.Core.Common.Vector3 position, int requiredSkillLevel = 2)
        {
            var definition = new TaskDefinition
            {
                Name = name,
                Type = TaskType.Mining,
                Priority = TaskPriority.Normal,
                TargetPosition = position,
                WorkRadius = 1.5f,
                EstimatedDuration = 8.0f,
                MaxAssignedCharacters = 1
            };

            definition.AddSkillRequirement(RimWorldFramework.Core.Characters.Components.SkillType.Mining, requiredSkillLevel);
            return CreateTask(definition);
        }

        /// <summary>
        /// 创建研究任务
        /// </summary>
        public TaskId CreateResearchTask(string name, int requiredSkillLevel = 5)
        {
            var definition = new TaskDefinition
            {
                Name = name,
                Type = TaskType.Research,
                Priority = TaskPriority.Low,
                EstimatedDuration = 20.0f,
                MaxAssignedCharacters = 1
            };

            definition.AddSkillRequirement(RimWorldFramework.Core.Characters.Components.SkillType.Research, requiredSkillLevel);
            return CreateTask(definition);
        }

        /// <summary>
        /// 注册默认工厂
        /// </summary>
        private void RegisterDefaultFactories()
        {
            RegisterTaskFactory(new DefaultTaskFactory());
        }

        /// <summary>
        /// 订阅任务事件
        /// </summary>
        public void SubscribeToTaskEvents(
            Action<ITask>? onTaskCreated = null,
            Action<ITask>? onTaskCompleted = null,
            Action<ITask>? onTaskFailed = null,
            Action<ITask>? onTaskCancelled = null)
        {
            if (onTaskCreated != null)
                _taskManager.TaskCreated += onTaskCreated;
            
            if (onTaskCompleted != null)
                _taskManager.TaskCompleted += onTaskCompleted;
            
            if (onTaskFailed != null)
                _taskManager.TaskFailed += onTaskFailed;
            
            if (onTaskCancelled != null)
                _taskManager.TaskCancelled += onTaskCancelled;
        }
    }

    /// <summary>
    /// 任务系统统计信息
    /// </summary>
    public class TaskSystemStats
    {
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int AvailableTasks { get; set; }
        public int AssignedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int RegisteredFactories { get; set; }
        public float AverageProgress { get; set; }
        public Dictionary<TaskType, int> TasksByType { get; set; } = new();
        public Dictionary<TaskPriority, int> TasksByPriority { get; set; } = new();

        public override string ToString()
        {
            return $"任务总数: {TotalTasks}, 可用: {AvailableTasks}, 进行中: {InProgressTasks}, " +
                   $"已完成: {CompletedTasks}, 平均进度: {AverageProgress:P}";
        }
    }
}