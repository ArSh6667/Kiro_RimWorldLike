using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 默认任务工厂
    /// </summary>
    public class DefaultTaskFactory : ITaskFactory
    {
        public IEnumerable<TaskType> SupportedTypes => new[]
        {
            TaskType.Construction,
            TaskType.Mining,
            TaskType.Growing,
            TaskType.Cooking,
            TaskType.Crafting,
            TaskType.Research,
            TaskType.Hauling,
            TaskType.Cleaning,
            TaskType.Hunting,
            TaskType.Social,
            TaskType.Medical,
            TaskType.Art,
            TaskType.Maintenance,
            TaskType.Defense
        };

        public ITask CreateTask(TaskDefinition definition)
        {
            return definition.Type switch
            {
                TaskType.Construction => new ConstructionTask(definition),
                TaskType.Mining => new MiningTask(definition),
                TaskType.Research => new ResearchTask(definition),
                _ => new ConcreteTask(definition)
            };
        }
    }

    /// <summary>
    /// 建造任务工厂
    /// </summary>
    public class ConstructionTaskFactory : ITaskFactory
    {
        public IEnumerable<TaskType> SupportedTypes => new[] { TaskType.Construction };

        public ITask CreateTask(TaskDefinition definition)
        {
            if (definition.Type != TaskType.Construction)
                throw new ArgumentException($"不支持的任务类型: {definition.Type}");

            return new ConstructionTask(definition);
        }
    }

    /// <summary>
    /// 挖掘任务工厂
    /// </summary>
    public class MiningTaskFactory : ITaskFactory
    {
        public IEnumerable<TaskType> SupportedTypes => new[] { TaskType.Mining };

        public ITask CreateTask(TaskDefinition definition)
        {
            if (definition.Type != TaskType.Mining)
                throw new ArgumentException($"不支持的任务类型: {definition.Type}");

            return new MiningTask(definition);
        }
    }

    /// <summary>
    /// 研究任务工厂
    /// </summary>
    public class ResearchTaskFactory : ITaskFactory
    {
        public IEnumerable<TaskType> SupportedTypes => new[] { TaskType.Research };

        public ITask CreateTask(TaskDefinition definition)
        {
            if (definition.Type != TaskType.Research)
                throw new ArgumentException($"不支持的任务类型: {definition.Type}");

            return new ResearchTask(definition);
        }
    }
}