using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务依赖关系解析器
    /// </summary>
    public class TaskDependencyResolver
    {
        private readonly Dictionary<TaskId, HashSet<TaskId>> _dependencies = new();
        private readonly Dictionary<TaskId, HashSet<TaskId>> _dependents = new();
        private readonly Dictionary<TaskId, ITask> _tasks = new();

        /// <summary>
        /// 添加任务
        /// </summary>
        public void AddTask(ITask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            _tasks[task.Id] = task;
            
            // 初始化依赖关系
            _dependencies[task.Id] = new HashSet<TaskId>(task.Definition.Prerequisites);
            _dependents[task.Id] = new HashSet<TaskId>(task.Definition.Dependents);

            // 更新反向依赖关系
            foreach (var prerequisiteId in task.Definition.Prerequisites)
            {
                if (!_dependents.ContainsKey(prerequisiteId))
                    _dependents[prerequisiteId] = new HashSet<TaskId>();
                
                _dependents[prerequisiteId].Add(task.Id);
            }

            foreach (var dependentId in task.Definition.Dependents)
            {
                if (!_dependencies.ContainsKey(dependentId))
                    _dependencies[dependentId] = new HashSet<TaskId>();
                
                _dependencies[dependentId].Add(task.Id);
            }
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public void RemoveTask(ITask task)
        {
            if (task == null) return;

            var taskId = task.Id;
            
            // 移除依赖关系
            if (_dependencies.TryGetValue(taskId, out var dependencies))
            {
                foreach (var depId in dependencies)
                {
                    _dependents.TryGetValue(depId, out var deps);
                    deps?.Remove(taskId);
                }
            }

            if (_dependents.TryGetValue(taskId, out var dependents))
            {
                foreach (var depId in dependents)
                {
                    _dependencies.TryGetValue(depId, out var deps);
                    deps?.Remove(taskId);
                }
            }

            // 清理
            _dependencies.Remove(taskId);
            _dependents.Remove(taskId);
            _tasks.Remove(taskId);
        }

        /// <summary>
        /// 添加依赖关系
        /// </summary>
        public bool AddDependency(TaskId dependentTask, TaskId prerequisiteTask)
        {
            // 检查循环依赖
            if (HasCircularDependency(dependentTask, prerequisiteTask))
                return false;

            if (!_dependencies.ContainsKey(dependentTask))
                _dependencies[dependentTask] = new HashSet<TaskId>();
            
            if (!_dependents.ContainsKey(prerequisiteTask))
                _dependents[prerequisiteTask] = new HashSet<TaskId>();

            _dependencies[dependentTask].Add(prerequisiteTask);
            _dependents[prerequisiteTask].Add(dependentTask);

            return true;
        }

        /// <summary>
        /// 移除依赖关系
        /// </summary>
        public bool RemoveDependency(TaskId dependentTask, TaskId prerequisiteTask)
        {
            var removed = false;

            if (_dependencies.TryGetValue(dependentTask, out var deps))
            {
                removed = deps.Remove(prerequisiteTask);
            }

            if (_dependents.TryGetValue(prerequisiteTask, out var dependents))
            {
                dependents.Remove(dependentTask);
            }

            return removed;
        }

        /// <summary>
        /// 获取任务的直接前置任务
        /// </summary>
        public IEnumerable<TaskId> GetPrerequisites(TaskId taskId)
        {
            return _dependencies.TryGetValue(taskId, out var deps) ? deps.ToList() : Enumerable.Empty<TaskId>();
        }

        /// <summary>
        /// 获取任务的直接依赖任务
        /// </summary>
        public IEnumerable<TaskId> GetDependents(TaskId taskId)
        {
            return _dependents.TryGetValue(taskId, out var deps) ? deps.ToList() : Enumerable.Empty<TaskId>();
        }

        /// <summary>
        /// 获取任务的所有前置任务（递归）
        /// </summary>
        public IEnumerable<TaskId> GetAllPrerequisites(TaskId taskId)
        {
            var visited = new HashSet<TaskId>();
            var result = new List<TaskId>();
            
            GetAllPrerequisitesRecursive(taskId, visited, result);
            
            return result;
        }

        /// <summary>
        /// 获取任务的所有依赖任务（递归）
        /// </summary>
        public IEnumerable<TaskId> GetAllDependents(TaskId taskId)
        {
            var visited = new HashSet<TaskId>();
            var result = new List<TaskId>();
            
            GetAllDependentsRecursive(taskId, visited, result);
            
            return result;
        }

        /// <summary>
        /// 检查任务是否可以执行（所有前置任务已完成）
        /// </summary>
        public bool CanExecute(TaskId taskId)
        {
            if (!_dependencies.TryGetValue(taskId, out var prerequisites))
                return true;

            foreach (var prereqId in prerequisites)
            {
                if (!_tasks.TryGetValue(prereqId, out var prereqTask))
                    return false;

                if (prereqTask.Status != TaskStatus.Completed)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取拓扑排序的任务列表
        /// </summary>
        public List<TaskId> GetTopologicalOrder()
        {
            var result = new List<TaskId>();
            var visited = new HashSet<TaskId>();
            var visiting = new HashSet<TaskId>();

            foreach (var taskId in _tasks.Keys)
            {
                if (!visited.Contains(taskId))
                {
                    if (!TopologicalSortVisit(taskId, visited, visiting, result))
                    {
                        throw new InvalidOperationException("检测到循环依赖");
                    }
                }
            }

            result.Reverse();
            return result;
        }

        /// <summary>
        /// 检查是否存在循环依赖
        /// </summary>
        public bool HasCircularDependency(TaskId from, TaskId to)
        {
            if (from == to) return true;

            var visited = new HashSet<TaskId>();
            return HasCircularDependencyRecursive(to, from, visited);
        }

        /// <summary>
        /// 获取可以立即执行的任务
        /// </summary>
        public IEnumerable<TaskId> GetExecutableTasks()
        {
            return _tasks.Keys.Where(CanExecute).ToList();
        }

        /// <summary>
        /// 获取依赖关系图的统计信息
        /// </summary>
        public DependencyStats GetStats()
        {
            var totalDependencies = _dependencies.Values.Sum(deps => deps.Count);
            var tasksWithDependencies = _dependencies.Count(kvp => kvp.Value.Count > 0);
            var maxDependencies = _dependencies.Values.Any() ? _dependencies.Values.Max(deps => deps.Count) : 0;

            return new DependencyStats
            {
                TotalTasks = _tasks.Count,
                TotalDependencies = totalDependencies,
                TasksWithDependencies = tasksWithDependencies,
                MaxDependenciesPerTask = maxDependencies,
                ExecutableTasks = GetExecutableTasks().Count()
            };
        }

        private void GetAllPrerequisitesRecursive(TaskId taskId, HashSet<TaskId> visited, List<TaskId> result)
        {
            if (visited.Contains(taskId)) return;
            visited.Add(taskId);

            if (_dependencies.TryGetValue(taskId, out var prerequisites))
            {
                foreach (var prereqId in prerequisites)
                {
                    result.Add(prereqId);
                    GetAllPrerequisitesRecursive(prereqId, visited, result);
                }
            }
        }

        private void GetAllDependentsRecursive(TaskId taskId, HashSet<TaskId> visited, List<TaskId> result)
        {
            if (visited.Contains(taskId)) return;
            visited.Add(taskId);

            if (_dependents.TryGetValue(taskId, out var dependents))
            {
                foreach (var depId in dependents)
                {
                    result.Add(depId);
                    GetAllDependentsRecursive(depId, visited, result);
                }
            }
        }

        private bool TopologicalSortVisit(TaskId taskId, HashSet<TaskId> visited, HashSet<TaskId> visiting, List<TaskId> result)
        {
            if (visiting.Contains(taskId)) return false; // 循环依赖
            if (visited.Contains(taskId)) return true;

            visiting.Add(taskId);

            if (_dependencies.TryGetValue(taskId, out var prerequisites))
            {
                foreach (var prereqId in prerequisites)
                {
                    if (!TopologicalSortVisit(prereqId, visited, visiting, result))
                        return false;
                }
            }

            visiting.Remove(taskId);
            visited.Add(taskId);
            result.Add(taskId);

            return true;
        }

        private bool HasCircularDependencyRecursive(TaskId current, TaskId target, HashSet<TaskId> visited)
        {
            if (current == target) return true;
            if (visited.Contains(current)) return false;

            visited.Add(current);

            if (_dependencies.TryGetValue(current, out var prerequisites))
            {
                foreach (var prereqId in prerequisites)
                {
                    if (HasCircularDependencyRecursive(prereqId, target, visited))
                        return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// 依赖关系统计信息
    /// </summary>
    public class DependencyStats
    {
        public int TotalTasks { get; set; }
        public int TotalDependencies { get; set; }
        public int TasksWithDependencies { get; set; }
        public int MaxDependenciesPerTask { get; set; }
        public int ExecutableTasks { get; set; }

        public override string ToString()
        {
            return $"任务: {TotalTasks}, 依赖: {TotalDependencies}, 可执行: {ExecutableTasks}";
        }
    }
}