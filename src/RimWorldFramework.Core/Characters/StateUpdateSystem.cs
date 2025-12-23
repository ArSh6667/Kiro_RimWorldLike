using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Events;
using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.Characters.Components;

namespace RimWorldFramework.Core.Characters
{
    /// <summary>
    /// 状态更新系统，负责管理角色状态和经验值更新
    /// </summary>
    public class StateUpdateSystem : GameSystem
    {
        private readonly IEntityManager _entityManager;
        private readonly IEventBus _eventBus;
        private readonly Dictionary<uint, CharacterStateTracker> _stateTrackers;

        public override int Priority => 200;
        public override string Name => "StateUpdateSystem";

        public StateUpdateSystem(IEntityManager entityManager, IEventBus eventBus, ILogger<StateUpdateSystem>? logger = null)
            : base(logger)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _stateTrackers = new Dictionary<uint, CharacterStateTracker>();
        }

        protected override void OnInitialize()
        {
            // 订阅相关事件
            _eventBus.Subscribe<TaskCompletedEvent>(OnTaskCompleted);
            _eventBus.Subscribe<SkillUsedEvent>(OnSkillUsed);
            _eventBus.Subscribe<CharacterCreatedEvent>(OnCharacterCreated);
            _eventBus.Subscribe<CharacterRemovedEvent>(OnCharacterRemoved);

            Logger.LogInformation("StateUpdateSystem initialized");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 更新所有角色的状态
            foreach (var tracker in _stateTrackers.Values)
            {
                UpdateCharacterState(tracker, deltaTime);
            }
        }

        protected override void OnShutdown()
        {
            // 取消订阅事件
            _eventBus.Unsubscribe<TaskCompletedEvent>(OnTaskCompleted);
            _eventBus.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
            _eventBus.Unsubscribe<CharacterCreatedEvent>(OnCharacterCreated);
            _eventBus.Unsubscribe<CharacterRemovedEvent>(OnCharacterRemoved);

            _stateTrackers.Clear();
            Logger.LogInformation("StateUpdateSystem shutdown");
        }

        /// <summary>
        /// 处理任务完成事件
        /// </summary>
        private void OnTaskCompleted(TaskCompletedEvent evt)
        {
            if (evt.AssignedCharacterId.HasValue && _stateTrackers.TryGetValue(evt.AssignedCharacterId.Value, out var tracker))
            {
                // 根据任务类型和难度计算经验值
                var experienceGain = CalculateExperienceGain(evt.Task);
                var skillType = DetermineSkillType(evt.Task);

                // 更新技能经验
                UpdateSkillExperience(tracker, skillType, experienceGain);

                // 发布状态更新事件
                _eventBus.Publish(new CharacterStateUpdatedEvent(evt.AssignedCharacterId.Value, skillType, experienceGain));

                Logger.LogDebug("Character {CharacterId} gained {Experience} experience in {Skill} from task completion",
                    evt.AssignedCharacterId.Value, experienceGain, skillType);
            }
        }

        /// <summary>
        /// 处理技能使用事件
        /// </summary>
        private void OnSkillUsed(SkillUsedEvent evt)
        {
            if (_stateTrackers.TryGetValue(evt.CharacterId, out var tracker))
            {
                // 使用技能获得少量经验
                var experienceGain = CalculateSkillUsageExperience(evt.SkillType, evt.SkillLevel);
                UpdateSkillExperience(tracker, evt.SkillType, experienceGain);

                Logger.LogDebug("Character {CharacterId} gained {Experience} experience from using {Skill}",
                    evt.CharacterId, experienceGain, evt.SkillType);
            }
        }

        /// <summary>
        /// 处理角色创建事件
        /// </summary>
        private void OnCharacterCreated(CharacterCreatedEvent evt)
        {
            var tracker = new CharacterStateTracker(evt.CharacterId);
            _stateTrackers[evt.CharacterId] = tracker;

            Logger.LogDebug("Started tracking state for character {CharacterId}", evt.CharacterId);
        }

        /// <summary>
        /// 处理角色移除事件
        /// </summary>
        private void OnCharacterRemoved(CharacterRemovedEvent evt)
        {
            _stateTrackers.Remove(evt.CharacterId);
            Logger.LogDebug("Stopped tracking state for character {CharacterId}", evt.CharacterId);
        }

        /// <summary>
        /// 更新角色状态
        /// </summary>
        private void UpdateCharacterState(CharacterStateTracker tracker, float deltaTime)
        {
            if (!_entityManager.EntityExists(tracker.CharacterId))
                return;

            var character = _entityManager.GetComponent<CharacterComponent>(tracker.CharacterId);
            if (character == null)
                return;

            // 更新需求值（饥饿、疲劳等）
            UpdateNeeds(character, deltaTime);

            // 检查技能等级提升
            CheckSkillLevelUps(tracker, character);

            // 更新角色整体状态
            UpdateOverallState(character);
        }

        /// <summary>
        /// 更新需求值
        /// </summary>
        private void UpdateNeeds(CharacterComponent character, float deltaTime)
        {
            // 饥饿值随时间增加
            var hungerNeed = character.Needs.GetNeed(NeedType.Hunger);
            hungerNeed.Value = Math.Max(0f, hungerNeed.Value - 0.01f * deltaTime);

            // 疲劳值随时间增加
            var restNeed = character.Needs.GetNeed(NeedType.Rest);
            restNeed.Value = Math.Max(0f, restNeed.Value - 0.008f * deltaTime);

            // 娱乐需求随时间增加
            var recreationNeed = character.Needs.GetNeed(NeedType.Recreation);
            recreationNeed.Value = Math.Max(0f, recreationNeed.Value - 0.005f * deltaTime);
        }

        /// <summary>
        /// 检查技能等级提升
        /// </summary>
        private void CheckSkillLevelUps(CharacterStateTracker tracker, CharacterComponent character)
        {
            foreach (var skill in character.Skills.GetAllSkills())
            {
                var requiredExp = CalculateRequiredExperience(skill.Level);
                if (skill.Experience >= requiredExp)
                {
                    // 技能升级
                    skill.Experience -= requiredExp;
                    skill.Level++;

                    // 发布技能升级事件
                    _eventBus.Publish(new SkillLevelUpEvent(tracker.CharacterId, skill.Type, skill.Level));

                    Logger.LogInformation("Character {CharacterId} skill {Skill} leveled up to {Level}",
                        tracker.CharacterId, skill.Type, skill.Level);
                }
            }
        }

        /// <summary>
        /// 更新角色整体状态
        /// </summary>
        private void UpdateOverallState(CharacterComponent character)
        {
            // 根据需求值计算心情
            var happiness = character.Needs.GetOverallHappiness();
            character.Mood = happiness;

            // 根据技能和心情计算整体效率
            var skillAverage = character.Skills.GetAllSkills().Average(s => s.Level) / 20.0f; // 假设最大技能等级为20
            character.Efficiency = (character.Mood * 0.6f + skillAverage * 0.4f);
        }

        /// <summary>
        /// 更新技能经验
        /// </summary>
        private void UpdateSkillExperience(CharacterStateTracker tracker, SkillType skillType, float experience)
        {
            if (!_entityManager.EntityExists(tracker.CharacterId))
                return;

            var character = _entityManager.GetComponent<CharacterComponent>(tracker.CharacterId);
            if (character == null)
                return;

            var skill = character.Skills.GetSkill(skillType);
            skill.Experience += experience;

            // 记录经验获得历史
            tracker.RecordExperienceGain(skillType, experience);
        }

        /// <summary>
        /// 计算任务完成的经验值
        /// </summary>
        private float CalculateExperienceGain(ITask task)
        {
            // 基础经验值
            float baseExp = 10.0f;

            // 根据任务优先级调整
            float priorityMultiplier = task.Priority switch
            {
                TaskPriority.Critical => 2.0f,
                TaskPriority.High => 1.5f,
                TaskPriority.Normal => 1.0f,
                TaskPriority.Low => 0.8f,
                _ => 1.0f
            };

            // 根据任务复杂度调整（这里简化处理）
            float complexityMultiplier = 1.0f;

            return baseExp * priorityMultiplier * complexityMultiplier;
        }

        /// <summary>
        /// 确定任务对应的技能类型
        /// </summary>
        private SkillType DetermineSkillType(ITask task)
        {
            // 根据任务类型确定技能类型（简化实现）
            return task.GetType().Name switch
            {
                "ConstructionTask" => SkillType.Construction,
                "CraftingTask" => SkillType.Crafting,
                "ResearchTask" => SkillType.Research,
                "CookingTask" => SkillType.Cooking,
                "MiningTask" => SkillType.Mining,
                _ => SkillType.Mining // 默认使用Mining作为通用技能
            };
        }

        /// <summary>
        /// 计算技能使用的经验值
        /// </summary>
        private float CalculateSkillUsageExperience(SkillType skillType, int currentLevel)
        {
            // 基础经验值，随技能等级递减
            return Math.Max(1.0f, 5.0f - currentLevel * 0.2f);
        }

        /// <summary>
        /// 计算升级所需经验值
        /// </summary>
        private float CalculateRequiredExperience(int currentLevel)
        {
            // 指数增长的经验需求
            return 100.0f * (float)Math.Pow(1.2, currentLevel);
        }

        /// <summary>
        /// 获取角色状态跟踪器
        /// </summary>
        public CharacterStateTracker? GetStateTracker(uint characterId)
        {
            return _stateTrackers.TryGetValue(characterId, out var tracker) ? tracker : null;
        }
    }

    /// <summary>
    /// 角色状态跟踪器
    /// </summary>
    public class CharacterStateTracker
    {
        public uint CharacterId { get; }
        public DateTime LastUpdate { get; private set; }
        public Dictionary<SkillType, List<ExperienceGainRecord>> ExperienceHistory { get; }

        public CharacterStateTracker(uint characterId)
        {
            CharacterId = characterId;
            LastUpdate = DateTime.UtcNow;
            ExperienceHistory = new Dictionary<SkillType, List<ExperienceGainRecord>>();
        }

        public void RecordExperienceGain(SkillType skillType, float experience)
        {
            if (!ExperienceHistory.ContainsKey(skillType))
            {
                ExperienceHistory[skillType] = new List<ExperienceGainRecord>();
            }

            ExperienceHistory[skillType].Add(new ExperienceGainRecord
            {
                Amount = experience,
                Timestamp = DateTime.UtcNow
            });

            LastUpdate = DateTime.UtcNow;

            // 保持历史记录在合理范围内
            if (ExperienceHistory[skillType].Count > 100)
            {
                ExperienceHistory[skillType].RemoveAt(0);
            }
        }

        public float GetTotalExperience(SkillType skillType)
        {
            return ExperienceHistory.TryGetValue(skillType, out var records) 
                ? records.Sum(r => r.Amount) 
                : 0.0f;
        }
    }

    /// <summary>
    /// 经验获得记录
    /// </summary>
    public class ExperienceGainRecord
    {
        public float Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #region 状态更新相关事件

    /// <summary>
    /// 角色状态更新事件
    /// </summary>
    public class CharacterStateUpdatedEvent : GameEvent
    {
        public uint CharacterId { get; }
        public SkillType SkillType { get; }
        public float ExperienceGained { get; }

        public CharacterStateUpdatedEvent(uint characterId, SkillType skillType, float experienceGained)
        {
            CharacterId = characterId;
            SkillType = skillType;
            ExperienceGained = experienceGained;
        }
    }

    /// <summary>
    /// 技能升级事件
    /// </summary>
    public class SkillLevelUpEvent : GameEvent
    {
        public uint CharacterId { get; }
        public SkillType SkillType { get; }
        public int NewLevel { get; }

        public SkillLevelUpEvent(uint characterId, SkillType skillType, int newLevel)
        {
            CharacterId = characterId;
            SkillType = skillType;
            NewLevel = newLevel;
        }
    }

    /// <summary>
    /// 技能使用事件
    /// </summary>
    public class SkillUsedEvent : GameEvent
    {
        public uint CharacterId { get; }
        public SkillType SkillType { get; }
        public int SkillLevel { get; }

        public SkillUsedEvent(uint characterId, SkillType skillType, int skillLevel)
        {
            CharacterId = characterId;
            SkillType = skillType;
            SkillLevel = skillLevel;
        }
    }

    /// <summary>
    /// 角色创建事件
    /// </summary>
    public class CharacterCreatedEvent : GameEvent
    {
        public uint CharacterId { get; }

        public CharacterCreatedEvent(uint characterId)
        {
            CharacterId = characterId;
        }
    }

    /// <summary>
    /// 角色移除事件
    /// </summary>
    public class CharacterRemovedEvent : GameEvent
    {
        public uint CharacterId { get; }

        public CharacterRemovedEvent(uint characterId)
        {
            CharacterId = characterId;
        }
    }

    #endregion
}