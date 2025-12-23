using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Events;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Tasks;

namespace RimWorldFramework.Core.Systems
{
    /// <summary>
    /// 游戏进度跟踪系统
    /// </summary>
    public class GameProgressSystem : GameSystem
    {
        private readonly IEntityManager _entityManager;
        private readonly IEventBus _eventBus;
        private readonly GameProgressTracker _progressTracker;

        public override int Priority => 50;
        public override string Name => "GameProgressSystem";

        public GameProgressSystem(IEntityManager entityManager, IEventBus eventBus, ILogger<GameProgressSystem>? logger = null)
            : base(logger)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _progressTracker = new GameProgressTracker();
        }

        protected override void OnInitialize()
        {
            // 订阅相关事件
            _eventBus.Subscribe<TaskCompletedEvent>(OnTaskCompleted);
            _eventBus.Subscribe<SkillLevelUpEvent>(OnSkillLevelUp);
            _eventBus.Subscribe<CharacterCreatedEvent>(OnCharacterCreated);
            _eventBus.Subscribe<CharacterRemovedEvent>(OnCharacterRemoved);
            _eventBus.Subscribe<ResearchCompletedEvent>(OnResearchCompleted);
            _eventBus.Subscribe<BuildingConstructedEvent>(OnBuildingConstructed);

            Logger.LogInformation("GameProgressSystem initialized");
        }

        protected override void OnUpdate(float deltaTime)
        {
            _progressTracker.Update(deltaTime);

            // 检查是否达成新的里程碑
            CheckMilestones();
        }

        protected override void OnShutdown()
        {
            // 取消订阅事件
            _eventBus.Unsubscribe<TaskCompletedEvent>(OnTaskCompleted);
            _eventBus.Unsubscribe<SkillLevelUpEvent>(OnSkillLevelUp);
            _eventBus.Unsubscribe<CharacterCreatedEvent>(OnCharacterCreated);
            _eventBus.Unsubscribe<CharacterRemovedEvent>(OnCharacterRemoved);
            _eventBus.Unsubscribe<ResearchCompletedEvent>(OnResearchCompleted);
            _eventBus.Unsubscribe<BuildingConstructedEvent>(OnBuildingConstructed);

            Logger.LogInformation("GameProgressSystem shutdown");
        }

        /// <summary>
        /// 处理任务完成事件
        /// </summary>
        private void OnTaskCompleted(TaskCompletedEvent evt)
        {
            _progressTracker.RecordTaskCompletion(evt.Task);
            Logger.LogDebug("Recorded task completion: {TaskType}", evt.Task.GetType().Name);
        }

        /// <summary>
        /// 处理技能升级事件
        /// </summary>
        private void OnSkillLevelUp(SkillLevelUpEvent evt)
        {
            _progressTracker.RecordSkillLevelUp(evt.CharacterId, evt.SkillType, evt.NewLevel);
            Logger.LogDebug("Recorded skill level up: Character {CharacterId}, Skill {Skill}, Level {Level}",
                evt.CharacterId, evt.SkillType, evt.NewLevel);
        }

        /// <summary>
        /// 处理角色创建事件
        /// </summary>
        private void OnCharacterCreated(CharacterCreatedEvent evt)
        {
            _progressTracker.RecordCharacterCreation(evt.CharacterId);
            Logger.LogDebug("Recorded character creation: {CharacterId}", evt.CharacterId);
        }

        /// <summary>
        /// 处理角色移除事件
        /// </summary>
        private void OnCharacterRemoved(CharacterRemovedEvent evt)
        {
            _progressTracker.RecordCharacterRemoval(evt.CharacterId);
            Logger.LogDebug("Recorded character removal: {CharacterId}", evt.CharacterId);
        }

        /// <summary>
        /// 处理研究完成事件
        /// </summary>
        private void OnResearchCompleted(ResearchCompletedEvent evt)
        {
            _progressTracker.RecordResearchCompletion(evt.ResearchId);
            Logger.LogDebug("Recorded research completion: {ResearchId}", evt.ResearchId);
        }

        /// <summary>
        /// 处理建筑建造事件
        /// </summary>
        private void OnBuildingConstructed(BuildingConstructedEvent evt)
        {
            _progressTracker.RecordBuildingConstruction(evt.BuildingType);
            Logger.LogDebug("Recorded building construction: {BuildingType}", evt.BuildingType);
        }

        /// <summary>
        /// 检查里程碑
        /// </summary>
        private void CheckMilestones()
        {
            var newMilestones = _progressTracker.CheckMilestones();
            foreach (var milestone in newMilestones)
            {
                _eventBus.Publish(new MilestoneAchievedEvent(milestone));
                Logger.LogInformation("Milestone achieved: {Milestone}", milestone.Name);
            }
        }

        /// <summary>
        /// 获取游戏进度
        /// </summary>
        public GameProgress GetProgress()
        {
            return _progressTracker.GetProgress();
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public GameStatistics GetStatistics()
        {
            return _progressTracker.GetStatistics();
        }
    }

    /// <summary>
    /// 游戏进度跟踪器
    /// </summary>
    public class GameProgressTracker
    {
        private readonly GameProgress _progress;
        private readonly GameStatistics _statistics;
        private readonly List<Milestone> _availableMilestones;
        private readonly HashSet<string> _achievedMilestones;

        public GameProgressTracker()
        {
            _progress = new GameProgress();
            _statistics = new GameStatistics();
            _availableMilestones = CreateMilestones();
            _achievedMilestones = new HashSet<string>();
        }

        public void Update(float deltaTime)
        {
            _progress.TotalPlayTime += TimeSpan.FromSeconds(deltaTime);
        }

        public void RecordTaskCompletion(ITask task)
        {
            _statistics.TasksCompleted++;
            _statistics.TaskCompletionsByType.TryGetValue(task.GetType().Name, out var count);
            _statistics.TaskCompletionsByType[task.GetType().Name] = count + 1;
        }

        public void RecordSkillLevelUp(uint characterId, SkillType skillType, int newLevel)
        {
            _statistics.SkillLevelUps++;
            
            if (!_statistics.HighestSkillLevels.ContainsKey(skillType) || 
                _statistics.HighestSkillLevels[skillType] < newLevel)
            {
                _statistics.HighestSkillLevels[skillType] = newLevel;
            }
        }

        public void RecordCharacterCreation(uint characterId)
        {
            _statistics.CharactersCreated++;
            _progress.CurrentCharacterCount++;
        }

        public void RecordCharacterRemoval(uint characterId)
        {
            _progress.CurrentCharacterCount = Math.Max(0, _progress.CurrentCharacterCount - 1);
        }

        public void RecordResearchCompletion(string researchId)
        {
            _statistics.ResearchCompleted++;
            _progress.CompletedResearch.Add(researchId);
        }

        public void RecordBuildingConstruction(string buildingType)
        {
            _statistics.BuildingsConstructed++;
            _statistics.BuildingsByType.TryGetValue(buildingType, out var count);
            _statistics.BuildingsByType[buildingType] = count + 1;
        }

        public List<Milestone> CheckMilestones()
        {
            var newMilestones = new List<Milestone>();

            foreach (var milestone in _availableMilestones)
            {
                if (!_achievedMilestones.Contains(milestone.Id) && milestone.IsAchieved(_statistics, _progress))
                {
                    _achievedMilestones.Add(milestone.Id);
                    _progress.AchievedMilestones.Add(milestone);
                    newMilestones.Add(milestone);
                }
            }

            return newMilestones;
        }

        public GameProgress GetProgress()
        {
            return _progress;
        }

        public GameStatistics GetStatistics()
        {
            return _statistics;
        }

        private List<Milestone> CreateMilestones()
        {
            return new List<Milestone>
            {
                new Milestone("first_character", "First Character", "Create your first character", 
                    (stats, progress) => stats.CharactersCreated >= 1),
                
                new Milestone("five_characters", "Small Colony", "Have 5 characters", 
                    (stats, progress) => progress.CurrentCharacterCount >= 5),
                
                new Milestone("first_skill_master", "Skill Master", "Reach level 10 in any skill", 
                    (stats, progress) => stats.HighestSkillLevels.Values.Any(level => level >= 10)),
                
                new Milestone("hundred_tasks", "Hard Worker", "Complete 100 tasks", 
                    (stats, progress) => stats.TasksCompleted >= 100),
                
                new Milestone("first_research", "Research Begins", "Complete your first research", 
                    (stats, progress) => stats.ResearchCompleted >= 1),
                
                new Milestone("ten_buildings", "Builder", "Construct 10 buildings", 
                    (stats, progress) => stats.BuildingsConstructed >= 10),
                
                new Milestone("one_hour_play", "Dedicated Player", "Play for 1 hour", 
                    (stats, progress) => progress.TotalPlayTime.TotalHours >= 1),
                
                new Milestone("skill_diversity", "Well Rounded", "Reach level 5 in 5 different skills", 
                    (stats, progress) => stats.HighestSkillLevels.Count(kvp => kvp.Value >= 5) >= 5)
            };
        }
    }

    /// <summary>
    /// 游戏进度
    /// </summary>
    public class GameProgress
    {
        public TimeSpan TotalPlayTime { get; set; }
        public int CurrentCharacterCount { get; set; }
        public HashSet<string> CompletedResearch { get; set; } = new HashSet<string>();
        public List<Milestone> AchievedMilestones { get; set; } = new List<Milestone>();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 游戏统计
    /// </summary>
    public class GameStatistics
    {
        public int TasksCompleted { get; set; }
        public int SkillLevelUps { get; set; }
        public int CharactersCreated { get; set; }
        public int ResearchCompleted { get; set; }
        public int BuildingsConstructed { get; set; }
        
        public Dictionary<string, int> TaskCompletionsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<SkillType, int> HighestSkillLevels { get; set; } = new Dictionary<SkillType, int>();
        public Dictionary<string, int> BuildingsByType { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// 里程碑
    /// </summary>
    public class Milestone
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public Func<GameStatistics, GameProgress, bool> Condition { get; }
        public DateTime AchievedTime { get; set; }

        public Milestone(string id, string name, string description, Func<GameStatistics, GameProgress, bool> condition)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public bool IsAchieved(GameStatistics statistics, GameProgress progress)
        {
            return Condition(statistics, progress);
        }
    }

    #region 进度相关事件

    /// <summary>
    /// 里程碑达成事件
    /// </summary>
    public class MilestoneAchievedEvent : GameEvent
    {
        public Milestone Milestone { get; }

        public MilestoneAchievedEvent(Milestone milestone)
        {
            Milestone = milestone ?? throw new ArgumentNullException(nameof(milestone));
            Milestone.AchievedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 研究完成事件
    /// </summary>
    public class ResearchCompletedEvent : GameEvent
    {
        public string ResearchId { get; }

        public ResearchCompletedEvent(string researchId)
        {
            ResearchId = researchId ?? throw new ArgumentNullException(nameof(researchId));
        }
    }

    /// <summary>
    /// 建筑建造事件
    /// </summary>
    public class BuildingConstructedEvent : GameEvent
    {
        public string BuildingType { get; }

        public BuildingConstructedEvent(string buildingType)
        {
            BuildingType = buildingType ?? throw new ArgumentNullException(nameof(buildingType));
        }
    }

    #endregion
}