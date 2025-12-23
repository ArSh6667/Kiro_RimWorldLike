using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务管理器 - 管理所有任务的生命周期和依赖关系
    /// </summary>
    public class TaskManager
    {
        private readonly Dictionary<TaskId, ITask> _tasks = new();
        private readonly Dictionary<TaskType, ITaskFactory> _taskFactories = new();
        private readonly ITaskValidator _validator;
        private readonly TaskDependencyResolver _dependencyResolver;
        private uint _nextTaskId = 1;

        // 事件
        public event Action<ITask>? TaskCreated;
        public event Action<ITask>? TaskCompleted;
        public event Action<ITask>? TaskFailed;
        public event Action<ITask>? TaskCancelled;

        public TaskManager(ITaskValidator? validator = null)
        {
            _validator = validator ?? new DefaultTaskValidator();
            _dependencyResolver = new TaskDependencyResolver();
        }

        /// <summary>
        /// 注册任务工厂
        /// </summary>
        public void RegisterTaskFactory(ITaskFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            foreach (var taskType in factory.SupportedTypes)
            {
                _taskFactories[taskType] = factory;
            }
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        public TaskId CreateTask(TaskDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            // 分配ID
            if (definition.Id.Value == 0)
            {
                definition.Id = new TaskId(_nextTaskId++);
            }

            // 验证任务定义
            var validationResult = _validator.ValidateDefinition(definition);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"任务定义无效: {string.Join(", ", validationResult.Errors)}");
            }

            // 验证依赖关系
            var dependencyResult = _validator.ValidateDependencies(definition, _tasks.Values);
            if (!dependencyResult.IsValid)
            {
                throw new ArgumentException($"任务依赖无效: {string.Join(", ", dependencyResult.Errors)}");
            }

            // 创建任务实例
            if (!_taskFactories.TryGetValue(definition.Type, out var factory))
            {
                throw new NotSupportedException($"不支持的任务类型: {definition.Type}");
            }

            var task = factory.CreateTask(definition);
            
            // 订阅事件
            task.StatusChanged += OnTaskStatusChanged;
            
            // 添加到管理器
            _tasks[task.Id] = task;
            
            // 更新依赖关系
            _dependencyResolver.AddTask(task);
            
            // 检查是否可以立即可用
            if (CanTaskBeAvailable(task))
            {
                SetTaskAvailable(task);
            }

            TaskCreated?.Invoke(task);
            return task.Id;
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public ITask? GetTask(TaskId taskId)
        {
            return _tasks.TryGetValue(taskId, out var task) ? task : null;
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public IEnumerable<ITask> GetAllTasks()
        {
            return _tasks.Values.ToList();
        }

        /// <summary>
        /// 获取可用任务
        /// </summary>
        public IEnumerable<ITask> GetAvailableTasks()
        {
            return _tasks.Values.Where(t => t.Status == TaskStatus.Available).ToList();
        }

        /// <summary>
        /// 获取指定状态的任务
        /// </summary>
        public IEnumerable<ITask> GetTasksByStatus(TaskStatus status)
        {
            return _tasks.Values.Where(t => t.Status == status).ToList();
        }

        /// <summary>
        /// 获取指定类型的任务
        /// </summary>
        public IEnumerable<ITask> GetTasksByType(TaskType type)
        {
            return _tasks.Values.Where(t => t.Definition.Type == type).ToList();
        }

        /// <summary>
        /// 获取指定优先级的任务
        /// </summary>
        public IEnumerable<ITask> GetTasksByPriority(TaskPriority priority)
        {
            return _tasks.Values.Where(t => t.Definition.Priority == priority).ToList();
        }

        /// <summary>
        /// 为角色分配任务
        /// </summary>
        public bool AssignTask(TaskId taskId, uint characterId)
        {
            var task = GetTask(taskId);
            if (task == null) return false;

            return task.AssignCharacter(characterId);
        }

        /// <summary>
        /// 取消角色任务分配
        /// </summary>
        public bool UnassignTask(TaskId taskId, uint characterId)
        {
            var task = GetTask(taskId);
            if (task == null) return false;

            return task.UnassignCharacter(characterId);
        }

        /// <summary>
        /// 完成任务
        /// </summary>
        public bool CompleteTask(TaskId taskId)
        {
            var task = GetTask(taskId);
            if (task == null) return false;

            var result = task.Complete();
            return result == TaskResult.Success;
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool CancelTask(TaskId taskId)
        {
            var task = GetTask(taskId);
            if (task == null) return false;

            task.Cancel();
            return true;
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        public bool RemoveTask(TaskId taskId)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
                return false;

            // 取消任务
            if (task.Status != TaskStatus.Completed && task.Status != TaskStatus.Failed)
            {
                task.Cancel();
            }

            // 取消事件订阅
            task.StatusChanged -= OnTaskStatusChanged;

            // 从依赖解析器中移除
            _dependencyResolver.RemoveTask(task);

            // 从管理器中移除
            _tasks.Remove(taskId);

            return true;
        }

        /// <summary>
        /// 更新所有任务
        /// </summary>
        public void UpdateTasks(float deltaTime)
        {
            var tasksToUpdate = _tasks.Values
                .Where(t => t.Status == TaskStatus.InProgress)
                .ToList();

            foreach (var task in tasksToUpdate)
            {
                try
                {
                    task.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新任务 {task.Definition.Name} 时出错: {ex.Message}");
                    task.Cancel();
                }
            }
        }

        /// <summary>
        /// 获取角色的最佳任务
        /// </summary>
        public ITask? GetBestTaskForCharacter(CharacterEntity character)
        {
            if (character == null) return null;

            var availableTasks = GetAvailableTasks()
                .Where(t => t.CanExecute(character))
                .ToList();

            if (!availableTasks.Any()) return null;

            // 按优先级和适合度排序
            return availableTasks
                .OrderBy(t => (int)t.Definition.Priority)
                .ThenByDescending(t => CalculateTaskSuitability(t, character))
                .First();
        }

        /// <summary>
        /// 计算任务适合度
        /// </summary>
        private float CalculateTaskSuitability(ITask task, CharacterEntity character)
        {
            if (character.Skills == null) return 0f;

            float suitability = 0f;
            float totalWeight = 0f;

            foreach (var requirement in task.Definition.SkillRequirements)
            {
                var skill = character.Skills.GetSkill(requirement.SkillType);
                var skillSuitability = Math.Min(1f, (float)skill.Level / Math.Max(1, requirement.MinLevel));
                
                suitability += skillSuitability * requirement.Weight;
                totalWeight += requirement.Weight;
            }

            // 考虑距离因素
            if (task.Definition.TargetPosition.HasValue && character.Position != null)
            {
                var distance = character.Position.DistanceTo(task.Definition.TargetPosition.Value);
                var distanceFactor = Math.Max(0.1f, 1f - (distance / 50f)); // 50单位内距离影响
                suitability *= distanceFactor;
            }

            return totalWeight > 0 ? suitability / totalWeight : 0f;
        }

        /// <summary>
        /// 检查任务是否可以变为可用状态
        /// </summary>
        private bool CanTaskBeAvailable(ITask task)
        {
            // 检查所有前置任务是否完成
            foreach (var prerequisiteId in task.Definition.Prerequisites)
            {
                var prerequisite = GetTask(prerequisiteId);
                if (prerequisite == null || prerequisite.Status != TaskStatus.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 设置任务为可用状态
        /// </summary>
        private void SetTaskAvailable(ITask task)
        {
            if (task is BaseTask baseTask)
            {
                baseTask.SetStatus(TaskStatus.Available);
            }
        }

        /// <summary>
        /// 任务状态改变事件处理
        /// </summary>
        private void OnTaskStatusChanged(ITask task, TaskStatus oldStatus, TaskStatus newStatus)
        {
            switch (newStatus)
            {
                case TaskStatus.Completed:
                    TaskCompleted?.Invoke(task);
                    // 检查依赖任务是否可以激活
                    CheckDependentTasks(task);
                    break;
                
                case TaskStatus.Failed:
                    TaskFailed?.Invoke(task);
                    break;
                
                case TaskStatus.Cancelled:
                    TaskCancelled?.Invoke(task);
                    break;
            }
        }

        /// <summary>
        /// 检查依赖任务
        /// </summary>
        private void CheckDependentTasks(ITask completedTask)
        {
            foreach (var dependentId in completedTask.Definition.Dependents)
            {
                var dependent = GetTask(dependentId);
                if (dependent != null && dependent.Status == TaskStatus.Pending)
                {
                    if (CanTaskBeAvailable(dependent))
                    {
                        SetTaskAvailable(dependent);
                    }
                }
            }
        }

        /// <summary>
        /// 获取管理器统计信息
        /// </summary>
        public TaskManagerStats GetStats()
        {
            var tasks = _tasks.Values.ToList();
            
            return new TaskManagerStats
            {
                TotalTasks = tasks.Count,
                PendingTasks = tasks.Count(t => t.Status == TaskStatus.Pending),
                AvailableTasks = tasks.Count(t => t.Status == TaskStatus.Available),
                AssignedTasks = tasks.Count(t => t.Status == TaskStatus.Assigned),
                InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
                CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
                FailedTasks = tasks.Count(t => t.Status == TaskStatus.Failed),
                CancelledTasks = tasks.Count(t => t.Status == TaskStatus.Cancelled),
                RegisteredFactories = _taskFactories.Count
            };
        }
    }

    /// <summary>
    /// 任务管理器统计信息
    /// </summary>
    public class TaskManagerStats
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

        public override string ToString()
        {
            return $"总任务: {TotalTasks}, 可用: {AvailableTasks}, 进行中: {InProgressTasks}, 已完成: {CompletedTasks}";
        }
    }
}