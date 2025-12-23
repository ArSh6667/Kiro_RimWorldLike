using System;
using System.Collections.Generic;
using RimWorldFramework.Core.Characters;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务接口 - 定义任务的基本行为
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        TaskId Id { get; }

        /// <summary>
        /// 任务定义
        /// </summary>
        TaskDefinition Definition { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        TaskStatus Status { get; }

        /// <summary>
        /// 分配的角色列表
        /// </summary>
        IReadOnlyList<uint> AssignedCharacters { get; }

        /// <summary>
        /// 开始时间
        /// </summary>
        DateTime? StartTime { get; }

        /// <summary>
        /// 完成时间
        /// </summary>
        DateTime? CompletionTime { get; }

        /// <summary>
        /// 进度百分比 (0.0 - 1.0)
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 检查角色是否可以执行此任务
        /// </summary>
        bool CanExecute(CharacterEntity character);

        /// <summary>
        /// 分配角色到任务
        /// </summary>
        bool AssignCharacter(uint characterId);

        /// <summary>
        /// 取消分配角色
        /// </summary>
        bool UnassignCharacter(uint characterId);

        /// <summary>
        /// 开始执行任务
        /// </summary>
        TaskResult Start();

        /// <summary>
        /// 更新任务执行
        /// </summary>
        TaskResult Update(float deltaTime);

        /// <summary>
        /// 完成任务
        /// </summary>
        TaskResult Complete();

        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();

        /// <summary>
        /// 暂停任务
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复任务
        /// </summary>
        void Resume();

        /// <summary>
        /// 重置任务状态
        /// </summary>
        void Reset();

        /// <summary>
        /// 获取任务详细信息
        /// </summary>
        string GetDetailedInfo();

        /// <summary>
        /// 任务状态改变事件
        /// </summary>
        event Action<ITask, TaskStatus, TaskStatus>? StatusChanged;

        /// <summary>
        /// 任务进度更新事件
        /// </summary>
        event Action<ITask, float>? ProgressUpdated;
    }

    /// <summary>
    /// 任务工厂接口
    /// </summary>
    public interface ITaskFactory
    {
        /// <summary>
        /// 创建任务
        /// </summary>
        ITask CreateTask(TaskDefinition definition);

        /// <summary>
        /// 支持的任务类型
        /// </summary>
        IEnumerable<TaskType> SupportedTypes { get; }
    }

    /// <summary>
    /// 任务验证器接口
    /// </summary>
    public interface ITaskValidator
    {
        /// <summary>
        /// 验证任务定义
        /// </summary>
        TaskValidationResult ValidateDefinition(TaskDefinition definition);

        /// <summary>
        /// 验证任务分配
        /// </summary>
        TaskValidationResult ValidateAssignment(ITask task, CharacterEntity character);

        /// <summary>
        /// 验证任务依赖
        /// </summary>
        TaskValidationResult ValidateDependencies(TaskDefinition definition, IEnumerable<ITask> existingTasks);
    }

    /// <summary>
    /// 任务验证结果
    /// </summary>
    public class TaskValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static TaskValidationResult Success() => new() { IsValid = true };
        
        public static TaskValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}