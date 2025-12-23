using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RimWorldFramework.StandaloneDemo
{
    /// <summary>
    /// 独立的RimWorld游戏框架演示程序
    /// 展示核心概念而不依赖复杂的框架
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== RimWorld Game Framework Standalone Demo ===");
            Console.WriteLine("This demo shows the core game concepts in action");
            Console.WriteLine();

            try
            {
                // Run all demos quickly
                Console.WriteLine("Running quick demos...");
                
                DemoECS();
                DemoCharacterSystem();
                DemoSkillsAndNeeds();
                DemoTaskSystem();
                DemoQuickGameLoop();

                Console.WriteLine("=== Demo Completed Successfully! ===");
                Console.WriteLine("This demonstrates the core concepts of the RimWorld framework.");
                Console.WriteLine("Demo finished in under 5 seconds!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Demo error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // Auto-exit after 3 seconds instead of waiting for key press
            Console.WriteLine("\nDemo will exit in 3 seconds...");
            Thread.Sleep(3000);
        }

        /// <summary>
        /// Demo Entity Component System
        /// </summary>
        static void DemoECS()
        {
            Console.WriteLine("=== Entity Component System Demo ===");

            var entityManager = new SimpleEntityManager();

            // Create entities
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            Console.WriteLine($"Created entities: {entity1}, {entity2}");

            // Add components
            entityManager.AddComponent(entity1, new PositionComponent { X = 10, Y = 20 });
            entityManager.AddComponent(entity1, new HealthComponent { Current = 80, Max = 100 });
            entityManager.AddComponent(entity2, new PositionComponent { X = 5, Y = 15 });

            Console.WriteLine($"Entity {entity1} components: Position, Health");
            Console.WriteLine($"Entity {entity2} components: Position");

            // Query entities
            var entitiesWithPosition = entityManager.GetEntitiesWithComponent<PositionComponent>();
            Console.WriteLine($"Entities with Position: {string.Join(", ", entitiesWithPosition)}");

            Console.WriteLine("ECS Demo completed!\n");
        }

        /// <summary>
        /// Demo Character System
        /// </summary>
        static void DemoCharacterSystem()
        {
            Console.WriteLine("=== Character System Demo ===");

            var characters = new List<Character>
            {
                new Character("Alice", 25, Gender.Female),
                new Character("Bob", 30, Gender.Male),
                new Character("Charlie", 28, Gender.Male)
            };

            foreach (var character in characters)
            {
                Console.WriteLine($"Created: {character.GetDescription()}");
                
                // Add some traits
                character.AddTrait("Hardworking", 1.2f);
                character.AddTrait("Friendly", 0.8f);
                
                Console.WriteLine($"  Traits: {string.Join(", ", character.Traits.Keys)}");
            }

            Console.WriteLine("Character System Demo completed!\n");
        }

        /// <summary>
        /// Demo Skills and Needs
        /// </summary>
        static void DemoSkillsAndNeeds()
        {
            Console.WriteLine("=== Skills and Needs Demo ===");

            var character = new Character("Demo Character", 25, Gender.Female);

            // Set up skills
            character.Skills[SkillType.Construction] = new Skill(SkillType.Construction, 5, 250f);
            character.Skills[SkillType.Cooking] = new Skill(SkillType.Cooking, 3, 100f);
            character.Skills[SkillType.Mining] = new Skill(SkillType.Mining, 7, 800f);

            Console.WriteLine("Initial Skills:");
            foreach (var skill in character.Skills.Values.Where(s => s.Level > 0))
            {
                Console.WriteLine($"  {skill.Type}: Level {skill.Level}, Experience {skill.Experience:F0}");
            }

            // Simulate skill training
            Console.WriteLine("\nTraining Construction skill...");
            for (int i = 0; i < 3; i++)
            {
                var leveledUp = character.Skills[SkillType.Construction].AddExperience(300f);
                var skill = character.Skills[SkillType.Construction];
                Console.WriteLine($"  Added 300 exp. Level: {skill.Level}, Experience: {skill.Experience:F0}" +
                    (leveledUp ? " - LEVEL UP!" : ""));
            }

            // Show needs
            Console.WriteLine("\nCharacter Needs:");
            foreach (var need in character.Needs.Values)
            {
                Console.WriteLine($"  {need.Type}: {need.Value:P1} ({need.GetStatus()})");
            }

            // Simulate time passing
            Console.WriteLine("\nSimulating 5 seconds...");
            for (int i = 0; i < 5; i++)
            {
                foreach (var need in character.Needs.Values)
                {
                    need.Update(1.0f);
                }
            }

            Console.WriteLine("Updated Needs:");
            foreach (var need in character.Needs.Values)
            {
                Console.WriteLine($"  {need.Type}: {need.Value:P1} ({need.GetStatus()})");
            }

            Console.WriteLine("Skills and Needs Demo completed!\n");
        }

        /// <summary>
        /// Demo Task System
        /// </summary>
        static void DemoTaskSystem()
        {
            Console.WriteLine("=== Task System Demo ===");

            var taskManager = new SimpleTaskManager();
            var character = new Character("Worker", 30, Gender.Male);
            character.Skills[SkillType.Construction] = new Skill(SkillType.Construction, 6, 400f);

            // Create tasks
            var tasks = new[]
            {
                new SimpleTask("Build Wall", TaskPriority.High, SkillType.Construction, 5),
                new SimpleTask("Cook Meal", TaskPriority.Normal, SkillType.Cooking, 3),
                new SimpleTask("Mine Stone", TaskPriority.Low, SkillType.Mining, 4)
            };

            foreach (var task in tasks)
            {
                taskManager.AddTask(task);
                Console.WriteLine($"Added task: {task.Name} (Priority: {task.Priority}, Required: {task.RequiredSkill} {task.RequiredLevel})");
            }

            // Assign tasks
            Console.WriteLine($"\nAssigning tasks to {character.Name}:");
            foreach (var task in taskManager.GetAvailableTasks())
            {
                if (taskManager.CanAssignTask(task, character))
                {
                    taskManager.AssignTask(task, character);
                    Console.WriteLine($"  Assigned: {task.Name}");
                }
                else
                {
                    Console.WriteLine($"  Cannot assign: {task.Name} (insufficient skill)");
                }
            }

            // Execute tasks
            Console.WriteLine("\nExecuting assigned tasks:");
            var assignedTasks = taskManager.GetAssignedTasks(character);
            foreach (var task in assignedTasks)
            {
                Console.WriteLine($"  Executing: {task.Name}");
                for (int i = 0; i < 5; i++)
                {
                    task.Execute(0.2f);
                    Console.WriteLine($"    Progress: {task.Progress:P1}");
                    if (task.IsCompleted)
                    {
                        Console.WriteLine($"    Task completed!");
                        break;
                    }
                }
            }

            Console.WriteLine("Task System Demo completed!\n");
        }

        /// <summary>
        /// Demo Game Loop (Full version with timing)
        /// </summary>
        static void DemoGameLoop()
        {
            Console.WriteLine("=== Game Loop Demo ===");
            Console.WriteLine("Running a mini game simulation for 10 seconds...");

            var characters = new List<Character>
            {
                new Character("Alice", 25, Gender.Female),
                new Character("Bob", 30, Gender.Male)
            };

            // Set up characters
            foreach (var character in characters)
            {
                character.Skills[SkillType.Construction] = new Skill(SkillType.Construction, 3 + new Random().Next(0, 4), 0);
                character.Skills[SkillType.Cooking] = new Skill(SkillType.Cooking, 2 + new Random().Next(0, 3), 0);
            }

            var taskManager = new SimpleTaskManager();
            taskManager.AddTask(new SimpleTask("Build House", TaskPriority.High, SkillType.Construction, 4));
            taskManager.AddTask(new SimpleTask("Prepare Food", TaskPriority.Normal, SkillType.Cooking, 2));

            // Game loop
            var startTime = DateTime.Now;
            var frameCount = 0;

            while ((DateTime.Now - startTime).TotalSeconds < 10)
            {
                frameCount++;
                var deltaTime = 0.1f;

                // Update characters
                foreach (var character in characters)
                {
                    // Update needs
                    foreach (var need in character.Needs.Values)
                    {
                        need.Update(deltaTime);
                    }

                    // Try to assign tasks
                    var availableTasks = taskManager.GetAvailableTasks();
                    foreach (var task in availableTasks)
                    {
                        if (taskManager.CanAssignTask(task, character))
                        {
                            taskManager.AssignTask(task, character);
                            break;
                        }
                    }

                    // Execute assigned tasks
                    var assignedTasks = taskManager.GetAssignedTasks(character);
                    foreach (var task in assignedTasks.ToList())
                    {
                        task.Execute(deltaTime);
                        if (task.IsCompleted)
                        {
                            Console.WriteLine($"  {character.Name} completed: {task.Name}");
                            taskManager.CompleteTask(task);
                            
                            // Gain experience
                            character.Skills[task.RequiredSkill].AddExperience(50f);
                        }
                    }
                }

                Thread.Sleep(100); // 10 FPS
            }

            Console.WriteLine($"Game loop completed! Processed {frameCount} frames.");
            
            // Show final character states
            Console.WriteLine("\nFinal Character States:");
            foreach (var character in characters)
            {
                Console.WriteLine($"{character.Name}:");
                var topSkills = character.Skills.Values.Where(s => s.Level > 0).OrderByDescending(s => s.Level).Take(2);
                foreach (var skill in topSkills)
                {
                    Console.WriteLine($"  {skill.Type}: Level {skill.Level}, Experience {skill.Experience:F0}");
                }
                
                var mostUrgentNeed = character.Needs.Values.OrderBy(n => n.Value).First();
                Console.WriteLine($"  Most urgent need: {mostUrgentNeed.Type} ({mostUrgentNeed.Value:P1})");
            }

            Console.WriteLine("Game Loop Demo completed!\n");
        }

        /// <summary>
        /// Demo Quick Game Loop (Fast version without delays)
        /// </summary>
        static void DemoQuickGameLoop()
        {
            Console.WriteLine("=== Quick Game Loop Demo ===");

            var characters = new List<Character>
            {
                new Character("Alice", 25, Gender.Female),
                new Character("Bob", 30, Gender.Male)
            };

            // Set up characters
            foreach (var character in characters)
            {
                character.Skills[SkillType.Construction] = new Skill(SkillType.Construction, 3 + new Random().Next(0, 4), 0);
                character.Skills[SkillType.Cooking] = new Skill(SkillType.Cooking, 2 + new Random().Next(0, 3), 0);
                Console.WriteLine($"  {character.Name}: Construction {character.Skills[SkillType.Construction].Level}, Cooking {character.Skills[SkillType.Cooking].Level}");
            }

            var taskManager = new SimpleTaskManager();
            taskManager.AddTask(new SimpleTask("Build House", TaskPriority.High, SkillType.Construction, 4));
            taskManager.AddTask(new SimpleTask("Prepare Food", TaskPriority.Normal, SkillType.Cooking, 2));

            Console.WriteLine("Simulating 50 game frames quickly...");

            // Quick simulation - 50 frames without delays
            for (int frame = 0; frame < 50; frame++)
            {
                var deltaTime = 0.2f; // Larger time steps for faster completion

                // Update characters
                foreach (var character in characters)
                {
                    // Update needs (faster decay for demo)
                    foreach (var need in character.Needs.Values)
                    {
                        need.Update(deltaTime);
                    }

                    // Try to assign tasks
                    var availableTasks = taskManager.GetAvailableTasks();
                    foreach (var task in availableTasks)
                    {
                        if (taskManager.CanAssignTask(task, character))
                        {
                            taskManager.AssignTask(task, character);
                            Console.WriteLine($"  Frame {frame + 1}: {character.Name} started {task.Name}");
                            break;
                        }
                    }

                    // Execute assigned tasks
                    var assignedTasks = taskManager.GetAssignedTasks(character);
                    foreach (var task in assignedTasks.ToList())
                    {
                        task.Execute(deltaTime);
                        if (task.IsCompleted)
                        {
                            Console.WriteLine($"  Frame {frame + 1}: {character.Name} completed {task.Name}!");
                            taskManager.CompleteTask(task);
                            
                            // Gain experience
                            var leveledUp = character.Skills[task.RequiredSkill].AddExperience(100f);
                            if (leveledUp)
                            {
                                Console.WriteLine($"    {character.Name} leveled up {task.RequiredSkill}!");
                            }
                        }
                    }
                }
            }

            // Show final results
            Console.WriteLine("\nFinal Results:");
            foreach (var character in characters)
            {
                Console.WriteLine($"{character.Name}:");
                var improvedSkills = character.Skills.Values.Where(s => s.Level > 0 || s.Experience > 0).OrderByDescending(s => s.Level);
                foreach (var skill in improvedSkills)
                {
                    Console.WriteLine($"  {skill.Type}: Level {skill.Level}, Experience {skill.Experience:F0}");
                }
                
                var criticalNeeds = character.Needs.Values.Where(n => n.Value < 0.5f);
                if (criticalNeeds.Any())
                {
                    Console.WriteLine($"  Critical needs: {string.Join(", ", criticalNeeds.Select(n => $"{n.Type} ({n.Value:P1})"))}");
                }
            }

            Console.WriteLine("Quick Game Loop Demo completed!\n");
        }
    }

    #region Simple ECS Implementation

    public class SimpleEntityManager
    {
        private uint _nextEntityId = 1;
        private readonly Dictionary<uint, Dictionary<Type, object>> _entities = new();

        public uint CreateEntity()
        {
            var id = _nextEntityId++;
            _entities[id] = new Dictionary<Type, object>();
            return id;
        }

        public void AddComponent<T>(uint entityId, T component) where T : class
        {
            if (_entities.TryGetValue(entityId, out var components))
            {
                components[typeof(T)] = component;
            }
        }

        public T? GetComponent<T>(uint entityId) where T : class
        {
            if (_entities.TryGetValue(entityId, out var components) &&
                components.TryGetValue(typeof(T), out var component))
            {
                return component as T;
            }
            return null;
        }

        public List<uint> GetEntitiesWithComponent<T>() where T : class
        {
            return _entities.Where(kvp => kvp.Value.ContainsKey(typeof(T)))
                           .Select(kvp => kvp.Key)
                           .ToList();
        }
    }

    public class PositionComponent
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class HealthComponent
    {
        public float Current { get; set; }
        public float Max { get; set; }
    }

    #endregion

    #region Character System

    public enum Gender { Male, Female, Other }

    public enum SkillType
    {
        Mining, Construction, Growing, Cooking, Crafting, Research, Medicine, Combat, Social, Animals
    }

    public enum NeedType
    {
        Hunger, Rest, Recreation, Comfort, Beauty, Space, Temperature, Safety
    }

    public enum NeedStatus
    {
        Critical, Low, Moderate, Good, Satisfied
    }

    public class Character
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public Dictionary<string, float> Traits { get; set; } = new();
        public Dictionary<SkillType, Skill> Skills { get; set; } = new();
        public Dictionary<NeedType, Need> Needs { get; set; } = new();

        public Character(string name, int age, Gender gender)
        {
            Name = name;
            Age = age;
            Gender = gender;

            // Initialize skills
            foreach (SkillType skillType in Enum.GetValues<SkillType>())
            {
                Skills[skillType] = new Skill(skillType, 0, 0);
            }

            // Initialize needs
            foreach (NeedType needType in Enum.GetValues<NeedType>())
            {
                Needs[needType] = new Need(needType);
            }
        }

        public void AddTrait(string traitName, float intensity)
        {
            Traits[traitName] = Math.Max(-2f, Math.Min(2f, intensity));
        }

        public string GetDescription()
        {
            return $"{Name}, {Age} year old {Gender}";
        }
    }

    public class Skill
    {
        public SkillType Type { get; set; }
        public int Level { get; set; }
        public float Experience { get; set; }

        public Skill(SkillType type, int level, float experience)
        {
            Type = type;
            Level = level;
            Experience = experience;
        }

        public bool AddExperience(float amount)
        {
            Experience += amount;
            var requiredExp = (Level + 1) * 1000f;
            
            if (Experience >= requiredExp && Level < 20)
            {
                Experience -= requiredExp;
                Level++;
                return true;
            }
            return false;
        }
    }

    public class Need
    {
        public NeedType Type { get; set; }
        public float Value { get; set; } = 1.0f;
        public float DecayRate { get; set; }

        public Need(NeedType type)
        {
            Type = type;
            DecayRate = type switch
            {
                NeedType.Hunger => 0.05f,
                NeedType.Rest => 0.03f,
                NeedType.Recreation => 0.02f,
                _ => 0.01f
            };
        }

        public void Update(float deltaTime)
        {
            Value = Math.Max(0f, Value - (DecayRate * deltaTime));
        }

        public NeedStatus GetStatus()
        {
            return Value switch
            {
                >= 0.8f => NeedStatus.Satisfied,
                >= 0.6f => NeedStatus.Good,
                >= 0.4f => NeedStatus.Moderate,
                >= 0.2f => NeedStatus.Low,
                _ => NeedStatus.Critical
            };
        }
    }

    #endregion

    #region Task System

    public enum TaskPriority { Low, Normal, High, Critical }

    public class SimpleTask
    {
        public string Name { get; set; }
        public TaskPriority Priority { get; set; }
        public SkillType RequiredSkill { get; set; }
        public int RequiredLevel { get; set; }
        public float Progress { get; set; } = 0f;
        public Character? AssignedCharacter { get; set; }
        public bool IsCompleted => Progress >= 1f;

        public SimpleTask(string name, TaskPriority priority, SkillType requiredSkill, int requiredLevel)
        {
            Name = name;
            Priority = priority;
            RequiredSkill = requiredSkill;
            RequiredLevel = requiredLevel;
        }

        public void Execute(float deltaTime)
        {
            if (AssignedCharacter != null && !IsCompleted)
            {
                var skill = AssignedCharacter.Skills[RequiredSkill];
                var efficiency = Math.Max(0.1f, skill.Level / (float)Math.Max(1, RequiredLevel));
                Progress += efficiency * deltaTime * 0.2f; // Base progress rate
                Progress = Math.Min(1f, Progress);
            }
        }
    }

    public class SimpleTaskManager
    {
        private readonly List<SimpleTask> _tasks = new();

        public void AddTask(SimpleTask task)
        {
            _tasks.Add(task);
        }

        public List<SimpleTask> GetAvailableTasks()
        {
            return _tasks.Where(t => t.AssignedCharacter == null && !t.IsCompleted).ToList();
        }

        public List<SimpleTask> GetAssignedTasks(Character character)
        {
            return _tasks.Where(t => t.AssignedCharacter == character && !t.IsCompleted).ToList();
        }

        public bool CanAssignTask(SimpleTask task, Character character)
        {
            return task.AssignedCharacter == null && 
                   character.Skills[task.RequiredSkill].Level >= task.RequiredLevel;
        }

        public void AssignTask(SimpleTask task, Character character)
        {
            if (CanAssignTask(task, character))
            {
                task.AssignedCharacter = character;
            }
        }

        public void CompleteTask(SimpleTask task)
        {
            task.AssignedCharacter = null;
            task.Progress = 1f;
        }
    }

    #endregion
}