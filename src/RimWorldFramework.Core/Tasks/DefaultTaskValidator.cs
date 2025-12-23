using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 默认任务验证器
    /// </summary>
    public class DefaultTaskValidator : ITaskValidator
    {
        public TaskValidationResult ValidateDefinition(TaskDefinition definition)
        {
            var result = new TaskValidationResult { IsValid = true };

            if (definition == null)
            {
                result.AddError("任务定义不能为空");
                return result;
            }

            // 验证基本属性
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                result.AddError("任务名称不能为空");
            }

            if (definition.EstimatedDuration <= 0)
            {
                result.AddError("预计持续时间必须大于0");
            }

            if (definition.MaxDuration < definition.EstimatedDuration)
            {
                result.AddError("最大持续时间不能小于预计持续时间");
            }

            if (definition.MaxAssignedCharacters <= 0)
            {
                result.AddError("最大分配角色数必须大于0");
            }

            if (definition.WorkRadius < 0)
            {
                result.AddError("工作半径不能为负数");
            }

            // 验证技能需求
            foreach (var skillReq in definition.SkillRequirements)
            {
                if (skillReq.MinLevel < 0 || skillReq.MinLevel > 20)
                {
                    result.AddError($"技能 {skillReq.SkillType} 的最低等级必须在0-20之间");
                }

                if (skillReq.Weight <= 0)
                {
                    result.AddError($"技能 {skillReq.SkillType} 的权重必须大于0");
                }
            }

            // 验证截止时间
            if (definition.Deadline.HasValue && definition.Deadline.Value <= DateTime.Now)
            {
                result.AddWarning("任务截止时间已过期");
            }

            // 验证资源需求
            foreach (var item in definition.RequiredItems)
            {
                if (item.Value <= 0)
                {
                    result.AddError($"所需物品 {item.Key} 的数量必须大于0");
                }
            }

            foreach (var item in definition.ProducedItems)
            {
                if (item.Value <= 0)
                {
                    result.AddError($"产出物品 {item.Key} 的数量必须大于0");
                }
            }

            return result;
        }

        public TaskValidationResult ValidateAssignment(ITask task, CharacterEntity character)
        {
            var result = new TaskValidationResult { IsValid = true };

            if (task == null)
            {
                result.AddError("任务不能为空");
                return result;
            }

            if (character == null)
            {
                result.AddError("角色不能为空");
                return result;
            }

            // 检查任务状态
            if (task.Status != TaskStatus.Available && task.Status != TaskStatus.Assigned)
            {
                result.AddError($"任务状态 {task.Status} 不允许分配角色");
            }

            // 检查分配数量限制
            if (task.AssignedCharacters.Count >= task.Definition.MaxAssignedCharacters)
            {
                result.AddError("任务已达到最大分配角色数");
            }

            // 检查角色是否已分配
            if (task.AssignedCharacters.Contains(character.Id))
            {
                result.AddError("角色已分配到此任务");
            }

            // 检查技能需求
            if (character.Skills != null)
            {
                foreach (var requirement in task.Definition.SkillRequirements)
                {
                    var skill = character.Skills.GetSkill(requirement.SkillType);
                    if (skill.Level < requirement.MinLevel)
                    {
                        result.AddError($"角色 {requirement.SkillType} 技能等级 {skill.Level} 低于要求的 {requirement.MinLevel}");
                    }
                    else if (skill.Level == requirement.MinLevel)
                    {
                        result.AddWarning($"角色 {requirement.SkillType} 技能等级刚好满足最低要求");
                    }
                }
            }
            else
            {
                if (task.Definition.SkillRequirements.Any())
                {
                    result.AddError("角色没有技能组件，无法验证技能需求");
                }
            }

            // 检查距离
            if (task.Definition.TargetPosition.HasValue && character.Position != null)
            {
                var distance = character.Position.DistanceTo(task.Definition.TargetPosition.Value);
                if (distance > task.Definition.WorkRadius * 3) // 允许一定的距离容差
                {
                    result.AddWarning($"角色距离任务位置较远 ({distance:F1} 单位)");
                }
            }

            // 检查角色状态
            if (character.Needs != null)
            {
                var criticalNeeds = character.Needs.GetCriticalNeeds().ToList();
                if (criticalNeeds.Any())
                {
                    var needNames = string.Join(", ", criticalNeeds.Select(n => n.GetName()));
                    result.AddWarning($"角色有关键需求未满足: {needNames}");
                }
            }

            return result;
        }

        public TaskValidationResult ValidateDependencies(TaskDefinition definition, IEnumerable<ITask> existingTasks)
        {
            var result = new TaskValidationResult { IsValid = true };

            if (definition == null)
            {
                result.AddError("任务定义不能为空");
                return result;
            }

            var existingTaskIds = existingTasks?.Select(t => t.Id).ToHashSet() ?? new HashSet<TaskId>();

            // 验证前置任务是否存在
            foreach (var prerequisiteId in definition.Prerequisites)
            {
                if (!existingTaskIds.Contains(prerequisiteId))
                {
                    result.AddError($"前置任务 {prerequisiteId} 不存在");
                }
            }

            // 验证依赖任务是否存在
            foreach (var dependentId in definition.Dependents)
            {
                if (!existingTaskIds.Contains(dependentId))
                {
                    result.AddWarning($"依赖任务 {dependentId} 不存在，将在创建时建立关系");
                }
            }

            // 检查自依赖
            if (definition.Prerequisites.Contains(definition.Id))
            {
                result.AddError("任务不能依赖自己");
            }

            if (definition.Dependents.Contains(definition.Id))
            {
                result.AddError("任务不能被自己依赖");
            }

            // 检查重复依赖
            var duplicatePrereqs = definition.Prerequisites
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicatePrereqs)
            {
                result.AddWarning($"前置任务 {duplicate} 重复定义");
            }

            var duplicateDeps = definition.Dependents
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicateDeps)
            {
                result.AddWarning($"依赖任务 {duplicate} 重复定义");
            }

            // 简单的循环依赖检查（只检查直接循环）
            var commonTasks = definition.Prerequisites.Intersect(definition.Dependents);
            foreach (var commonTask in commonTasks)
            {
                result.AddError($"任务 {commonTask} 同时是前置任务和依赖任务，可能存在循环依赖");
            }

            return result;
        }
    }
}