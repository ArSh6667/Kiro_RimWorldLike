using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.Characters;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 任务基类 - 提供任务的基本实现
    /// </summary>
    public abstract class BaseTask : ITask
    {
        private readonly List<uint> _assignedCharacters = new();
        private TaskStatus _status = TaskStatus.Pending;
        private float _progress = 0f;

        public TaskId Id => Definition.Id;
        public TaskDefinition Definition { get; }
        public TaskStatus Status => _status;
        public IReadOnlyList<uint> AssignedCharacters => _assignedCharacters.AsReadOnly();
        public DateTime? StartTime { get; private set; }
        public DateTime? CompletionTime { get; private set; }
        public float Progress => _progress;

        // 事件
        public event Action<ITask, TaskStatus, TaskStatus>? StatusChanged;
        public event Action<ITask, float>? ProgressUpdated;

        protected BaseTask(TaskDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public virtual bool CanExecute(CharacterEntity character)
        {
            if (character == null) return false;
            if (_status != TaskStatus.Available && _status != TaskStatus.Assigned) return false;
            if (_assignedCharacters.Count >= Definition.MaxAssignedCharacters) return false;

            // 检查技能需求
            if (character.Skills != null)
            {
                foreach (var requirement in Definition.SkillRequirements)
                {
                    var skill = character.Skills.GetSkill(requirement.SkillType);
                    if (skill.Level < requirement.MinLevel)
                        return false;
                }
            }

            // 检查距离（如果有位置要求）
            if (Definition.TargetPosition.HasValue && character.Position != null)
            {
                var distance = character.Position.DistanceTo(Definition.TargetPosition.Value);
                if (distance > Definition.WorkRadius * 2) // 允许一定的距离容差
                    return false;
            }

            return true;
        }

        public virtual bool AssignCharacter(uint characterId)
        {
            if (_assignedCharacters.Contains(characterId))
                return false;

            if (_assignedCharacters.Count >= Definition.MaxAssignedCharacters)
                return false;

            _assignedCharacters.Add(characterId);
            
            if (_status == TaskStatus.Available)
            {
                SetStatus(TaskStatus.Assigned);
            }

            return true;
        }

        public virtual bool UnassignCharacter(uint characterId)
        {
            if (!_assignedCharacters.Remove(characterId))
                return false;

            if (_assignedCharacters.Count == 0 && _status == TaskStatus.Assigned)
            {
                SetStatus(TaskStatus.Available);
            }

            return true;
        }

        public virtual TaskResult Start()
        {
            if (_status != TaskStatus.Assigned)
                return TaskResult.Failure;

            if (_assignedCharacters.Count == 0)
                return TaskResult.Failure;

            StartTime = DateTime.Now;
            SetStatus(TaskStatus.InProgress);
            
            return OnStart();
        }

        public virtual TaskResult Update(float deltaTime)
        {
            if (_status != TaskStatus.InProgress)
                return TaskResult.Failure;

            // 检查超时
            if (StartTime.HasValue && Definition.MaxDuration < float.MaxValue)
            {
                var elapsed = (DateTime.Now - StartTime.Value).TotalSeconds;
                if (elapsed > Definition.MaxDuration)
                {
                    SetStatus(TaskStatus.Failed);
                    return TaskResult.Failure;
                }
            }

            // 检查截止时间
            if (Definition.IsExpired())
            {
                SetStatus(TaskStatus.Failed);
                return TaskResult.Failure;
            }

            var result = OnUpdate(deltaTime);
            
            if (result == TaskResult.Success)
            {
                return Complete();
            }
            else if (result == TaskResult.Failure)
            {
                SetStatus(TaskStatus.Failed);
            }

            return result;
        }

        public virtual TaskResult Complete()
        {
            if (_status != TaskStatus.InProgress)
                return TaskResult.Failure;

            CompletionTime = DateTime.Now;
            SetProgress(1.0f);
            SetStatus(TaskStatus.Completed);
            
            OnComplete();
            return TaskResult.Success;
        }

        public virtual void Cancel()
        {
            if (_status == TaskStatus.Completed || _status == TaskStatus.Failed)
                return;

            SetStatus(TaskStatus.Cancelled);
            OnCancel();
        }

        public virtual void Pause()
        {
            if (_status == TaskStatus.InProgress)
            {
                OnPause();
            }
        }

        public virtual void Resume()
        {
            if (_status == TaskStatus.InProgress)
            {
                OnResume();
            }
        }

        public virtual void Reset()
        {
            _assignedCharacters.Clear();
            StartTime = null;
            CompletionTime = null;
            SetProgress(0f);
            SetStatus(TaskStatus.Pending);
            OnReset();
        }

        public virtual string GetDetailedInfo()
        {
            var info = $"任务: {Definition.Name}\n";
            info += $"类型: {Definition.Type}\n";
            info += $"优先级: {Definition.Priority}\n";
            info += $"状态: {Status}\n";
            info += $"进度: {Progress:P}\n";
            info += $"分配角色数: {_assignedCharacters.Count}/{Definition.MaxAssignedCharacters}\n";
            
            if (StartTime.HasValue)
            {
                info += $"开始时间: {StartTime.Value:yyyy-MM-dd HH:mm:ss}\n";
            }
            
            if (CompletionTime.HasValue)
            {
                info += $"完成时间: {CompletionTime.Value:yyyy-MM-dd HH:mm:ss}\n";
            }

            if (Definition.SkillRequirements.Any())
            {
                info += "技能需求:\n";
                foreach (var req in Definition.SkillRequirements)
                {
                    info += $"  - {req.SkillType}: 等级 {req.MinLevel}\n";
                }
            }

            return info;
        }

        /// <summary>
        /// 设置任务状态
        /// </summary>
        protected void SetStatus(TaskStatus newStatus)
        {
            if (_status == newStatus) return;

            var oldStatus = _status;
            _status = newStatus;
            StatusChanged?.Invoke(this, oldStatus, newStatus);
        }

        /// <summary>
        /// 设置任务进度
        /// </summary>
        protected void SetProgress(float newProgress)
        {
            newProgress = Math.Max(0f, Math.Min(1f, newProgress));
            if (Math.Abs(_progress - newProgress) < 0.001f) return;

            _progress = newProgress;
            ProgressUpdated?.Invoke(this, _progress);
        }

        /// <summary>
        /// 任务开始时调用
        /// </summary>
        protected virtual TaskResult OnStart()
        {
            return TaskResult.InProgress;
        }

        /// <summary>
        /// 任务更新时调用
        /// </summary>
        protected abstract TaskResult OnUpdate(float deltaTime);

        /// <summary>
        /// 任务完成时调用
        /// </summary>
        protected virtual void OnComplete() { }

        /// <summary>
        /// 任务取消时调用
        /// </summary>
        protected virtual void OnCancel() { }

        /// <summary>
        /// 任务暂停时调用
        /// </summary>
        protected virtual void OnPause() { }

        /// <summary>
        /// 任务恢复时调用
        /// </summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// 任务重置时调用
        /// </summary>
        protected virtual void OnReset() { }

        public override string ToString()
        {
            return $"{Definition.Name} ({Status}, {Progress:P})";
        }
    }
}