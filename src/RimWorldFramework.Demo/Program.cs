using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core;
using RimWorldFramework.Core.Characters;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Configuration;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Events;
using RimWorldFramework.Core.MapGeneration;
using RimWorldFramework.Core.Systems;
using RimWorldFramework.Core.Tasks;

namespace RimWorldFramework.Demo
{
    /// <summary>
    /// RimWorld游戏框架演示程序
    /// </summary>
    class Program
    {
        private static GameFramework? _gameFramework;
        private static bool _isRunning = true;
        private static readonly Random _random = new Random();

        static void Main(string[] args)
        {
            // Set console encoding to UTF-8 to handle Unicode characters
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // Fallback to English if UTF-8 is not supported
            }

            Console.WriteLine("=== RimWorld Game Framework Demo ===");
            Console.WriteLine("Press 'q' to quit the game");
            Console.WriteLine();

            try
            {
                // 初始化游戏框架
                InitializeGame();

                // 创建初始游戏内容
                SetupInitialGameContent();

                // 开始游戏循环
                StartGameLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Game error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                _gameFramework?.Dispose();
            }
        }

        /// <summary>
        /// Initialize game framework
        /// </summary>
        static void InitializeGame()
        {
            Console.WriteLine("Initializing game framework...");

            // Create logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            });
            var logger = loggerFactory.CreateLogger<GameFramework>();

            // Create game framework
            _gameFramework = new GameFramework(logger);

            // Create configuration
            var config = new GameConfig
            {
                Graphics = new GraphicsConfig
                {
                    Width = 1920,
                    Height = 1080,
                    Fullscreen = false,
                    VSync = true
                },
                Audio = new AudioConfig
                {
                    MasterVolume = 0.8f,
                    MusicVolume = 0.6f,
                    SfxVolume = 0.7f
                },
                Gameplay = new GameplayConfig
                {
                    Difficulty = DifficultyLevel.Normal,
                    AutoSave = true,
                    AutoSaveInterval = TimeSpan.FromMinutes(5)
                }
            };

            // Initialize framework
            _gameFramework.Initialize(config);

            Console.WriteLine("Game framework initialized successfully!");
            Console.WriteLine();
        }

        /// <summary>
        /// Setup initial game content
        /// </summary>
        static void SetupInitialGameContent()
        {
            Console.WriteLine("Creating initial game content...");

            var entityManager = _gameFramework!.GetEntityManager();
            var eventBus = _gameFramework.GetEventBus();

            // Generate map
            GenerateMap();

            // Create initial characters
            CreateInitialCharacters(entityManager, eventBus);

            // Create initial tasks
            CreateInitialTasks();

            Console.WriteLine("Initial game content created successfully!");
            Console.WriteLine();
        }

        /// <summary>
        /// Generate map
        /// </summary>
        static void GenerateMap()
        {
            var mapGenSystem = _gameFramework!.GetSystem<MapGenerationSystem>();
            if (mapGenSystem != null)
            {
                var mapConfig = new MapGenerationConfig
                {
                    Width = 100,
                    Height = 100,
                    Seed = _random.Next(),
                    TerrainVariety = 0.7f,
                    ResourceDensity = 0.5f
                };

                Console.WriteLine($"Generating {mapConfig.Width}x{mapConfig.Height} map (seed: {mapConfig.Seed})...");
                var map = mapGenSystem.GenerateMap(mapConfig);
                Console.WriteLine($"Map generation complete! Terrain types: {map?.TerrainData?.Count ?? 0}");
            }
        }

        /// <summary>
        /// Create initial characters
        /// </summary>
        static void CreateInitialCharacters(IEntityManager entityManager, IEventBus eventBus)
        {
            var characterNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Edward" };

            for (int i = 0; i < 3; i++) // Create 3 initial characters
            {
                var characterId = entityManager.CreateEntity();
                var character = new CharacterComponent
                {
                    Name = characterNames[i],
                    Skills = new SkillComponent(),
                    Needs = new NeedComponent(),
                    Mood = 0.7f + _random.NextSingle() * 0.2f,
                    Efficiency = 0.6f + _random.NextSingle() * 0.3f
                };

                // 设置需求值
                character.Needs.SetNeedValue(NeedType.Hunger, _random.NextSingle() * 0.3f);
                character.Needs.SetNeedValue(NeedType.Rest, _random.NextSingle() * 0.3f);
                character.Needs.SetNeedValue(NeedType.Recreation, _random.NextSingle() * 0.3f);

                // Randomly set some skill levels
                var skills = character.Skills.GetAllSkills();
                foreach (var skill in skills)
                {
                    skill.Level = _random.Next(1, 6);
                    skill.Experience = _random.NextSingle() * 50f;
                }

                entityManager.AddComponent(characterId, character);
                eventBus.Publish(new CharacterCreatedEvent(characterId));

                Console.WriteLine($"Created character: {character.Name} (ID: {characterId})");
            }
        }

        /// <summary>
        /// Create initial tasks
        /// </summary>
        static void CreateInitialTasks()
        {
            var taskManager = _gameFramework!.GetSystem<TaskManager>();
            if (taskManager != null)
            {
                var taskTypes = new[] { "Construction", "Gathering", "Research", "Cooking", "Cleaning" };
                var priorities = new[] { TaskPriority.High, TaskPriority.Normal, TaskPriority.Low };

                for (int i = 0; i < 5; i++)
                {
                    var task = new DemoTask
                    {
                        Id = $"task_{i}",
                        Name = $"{taskTypes[i]} Task",
                        Description = $"Perform {taskTypes[i]} related work",
                        Priority = priorities[_random.Next(priorities.Length)],
                        Status = TaskStatus.Pending,
                        EstimatedDuration = TimeSpan.FromMinutes(_random.Next(5, 30))
                    };

                    taskManager.AddTask(task);
                    Console.WriteLine($"Created task: {task.Name} (Priority: {task.Priority})");
                }
            }
        }

        /// <summary>
        /// Start game loop
        /// </summary>
        static void StartGameLoop()
        {
            Console.WriteLine("Game started!");
            Console.WriteLine("Real-time status updating... (Press 'q' to quit)");
            Console.WriteLine();

            var lastUpdate = DateTime.UtcNow;
            var frameCount = 0;
            var lastStatsDisplay = DateTime.UtcNow;

            // Start input listening thread
            var inputThread = new Thread(HandleInput) { IsBackground = true };
            inputThread.Start();

            while (_isRunning && _gameFramework!.IsRunning)
            {
                var currentTime = DateTime.UtcNow;
                var deltaTime = (float)(currentTime - lastUpdate).TotalSeconds;
                lastUpdate = currentTime;

                // Update game framework
                _gameFramework.Update(deltaTime);

                frameCount++;

                // Display statistics every 5 seconds
                if ((currentTime - lastStatsDisplay).TotalSeconds >= 5)
                {
                    DisplayGameStats();
                    lastStatsDisplay = currentTime;
                }

                // Control frame rate (about 10 FPS for easy observation)
                Thread.Sleep(100);
            }

            Console.WriteLine("\nGame exited.");
        }

        /// <summary>
        /// 处理用户输入
        /// </summary>
        static void HandleInput()
        {
            while (_isRunning)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    _isRunning = false;
                    break;
                }
            }
        }

        /// <summary>
        /// Display game statistics
        /// </summary>
        static void DisplayGameStats()
        {
            try
            {
                var progressSystem = _gameFramework!.GetSystem<GameProgressSystem>();
                var entityManager = _gameFramework.GetEntityManager();

                if (progressSystem != null)
                {
                    var progress = progressSystem.GetProgress();
                    var stats = progressSystem.GetStatistics();

                    Console.Clear();
                    Console.WriteLine("=== RimWorld Game Framework Demo - Live Status ===");
                    Console.WriteLine($"Game Time: {progress.TotalPlayTime:hh\\:mm\\:ss}");
                    Console.WriteLine($"Current Characters: {progress.CurrentCharacterCount}");
                    Console.WriteLine();

                    Console.WriteLine("=== Game Statistics ===");
                    Console.WriteLine($"Tasks Completed: {stats.TasksCompleted}");
                    Console.WriteLine($"Skill Level Ups: {stats.SkillLevelUps}");
                    Console.WriteLine($"Characters Created: {stats.CharactersCreated}");
                    Console.WriteLine($"Research Completed: {stats.ResearchCompleted}");
                    Console.WriteLine($"Buildings Constructed: {stats.BuildingsConstructed}");
                    Console.WriteLine();

                    Console.WriteLine("=== Milestones Achieved ===");
                    if (progress.AchievedMilestones.Count > 0)
                    {
                        foreach (var milestone in progress.AchievedMilestones)
                        {
                            Console.WriteLine($"* {milestone.Name}: {milestone.Description}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No milestones achieved yet");
                    }
                    Console.WriteLine();

                    // Display character status
                    DisplayCharacterStatus(entityManager);

                    Console.WriteLine("Press 'q' to quit the game");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Display character status
        /// </summary>
        static void DisplayCharacterStatus(IEntityManager entityManager)
        {
            Console.WriteLine("=== Character Status ===");

            var stateUpdateSystem = _gameFramework!.GetSystem<StateUpdateSystem>();
            var entities = entityManager.GetEntitiesWithComponent<CharacterComponent>();

            foreach (var entityId in entities)
            {
                var character = entityManager.GetComponent<CharacterComponent>(entityId);
                if (character != null)
                {
                    Console.WriteLine($"{character.Name} (ID: {entityId}):");
                    Console.WriteLine($"  Mood: {character.Mood:P1} | Efficiency: {character.Efficiency:P1}");
                    Console.WriteLine($"  Hunger: {character.Needs.GetNeed(NeedType.Hunger).Value:P1} | Fatigue: {character.Needs.GetNeed(NeedType.Rest).Value:P1} | Recreation: {character.Needs.GetNeed(NeedType.Recreation).Value:P1}");

                    // Display skill levels
                    var topSkills = character.Skills.GetAllSkills()
                        .Where(s => s.Level > 0)
                        .OrderByDescending(s => s.Level)
                        .Take(3);

                    if (topSkills.Any())
                    {
                        var skillsText = string.Join(", ", topSkills.Select(s => $"{s.Type}:{s.Level}"));
                        Console.WriteLine($"  Top Skills: {skillsText}");
                    }

                    Console.WriteLine();
                }
            }
        }
    }

    /// <summary>
    /// 演示任务类
    /// </summary>
    public class DemoTask : ITask
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public float Progress { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public uint? AssignedCharacterId { get; set; }

        public bool CanExecute(uint characterId) => Status == TaskStatus.Pending || Status == TaskStatus.InProgress;

        public TaskResult Execute(uint characterId, float deltaTime)
        {
            if (Status == TaskStatus.Pending)
            {
                Status = TaskStatus.InProgress;
                StartedAt = DateTime.UtcNow;
                AssignedCharacterId = characterId;
            }

            // 模拟任务进度
            var progressRate = 0.1f; // 每秒10%的进度
            Progress += progressRate * deltaTime;

            if (Progress >= 1.0f)
            {
                return Complete();
            }

            return TaskResult.InProgress;
        }

        public TaskResult Complete()
        {
            Status = TaskStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            Progress = 1.0f;
            return TaskResult.Success;
        }

        public void Cancel()
        {
            Status = TaskStatus.Cancelled;
        }

        public ITask Clone()
        {
            return new DemoTask
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Priority = Priority,
                Status = Status,
                Progress = Progress,
                EstimatedDuration = EstimatedDuration,
                CreatedAt = CreatedAt,
                StartedAt = StartedAt,
                CompletedAt = CompletedAt,
                AssignedCharacterId = AssignedCharacterId
            };
        }
    }
}