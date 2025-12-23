using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.SimpleDemo
{
    /// <summary>
    /// 简化的RimWorld游戏框架演示程序
    /// </summary>
    class Program
    {
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

            Console.WriteLine("=== RimWorld Game Framework Simple Demo ===");
            Console.WriteLine("This demo shows the core components working independently");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();
            Console.WriteLine();

            try
            {
                // Demo ECS System
                DemoECSSystem();

                // Demo Character Components
                DemoCharacterComponents();

                // Demo Skills System
                DemoSkillsSystem();

                // Demo Needs System
                DemoNeedsSystem();

                Console.WriteLine("\n=== Demo Completed Successfully! ===");
                Console.WriteLine("All core components are working correctly.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Demo error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Demo ECS System
        /// </summary>
        static void DemoECSSystem()
        {
            Console.WriteLine("=== ECS System Demo ===");

            // Create entity manager
            var entityManager = new EntityManager();
            Console.WriteLine("EntityManager created");

            // Create entities
            var entity1 = entityManager.CreateEntity();
            var entity2 = entityManager.CreateEntity();
            Console.WriteLine($"Created entities: {entity1}, {entity2}");

            // Add components
            var testComponent = new TestComponent { Value = 42 };
            entityManager.AddComponent(entity1, testComponent);
            Console.WriteLine($"Added TestComponent to entity {entity1}");

            // Retrieve component
            var retrieved = entityManager.GetComponent<TestComponent>(entity1);
            Console.WriteLine($"Retrieved component value: {retrieved?.Value}");

            // Check component existence
            Console.WriteLine($"Entity {entity1} has TestComponent: {entityManager.HasComponent<TestComponent>(entity1)}");
            Console.WriteLine($"Entity {entity2} has TestComponent: {entityManager.HasComponent<TestComponent>(entity2)}");

            Console.WriteLine("ECS System Demo completed!\n");
        }

        /// <summary>
        /// Demo Character Components
        /// </summary>
        static void DemoCharacterComponents()
        {
            Console.WriteLine("=== Character Components Demo ===");

            // Create character component
            var character = new CharacterComponent("Alice")
            {
                Age = 25,
                Gender = Gender.Female,
                Biography = "A skilled engineer seeking new opportunities"
            };

            Console.WriteLine($"Created character: {character.GetDescription()}");
            Console.WriteLine($"Detailed info: {character.GetDetailedInfo()}");

            // Add traits
            character.AddTrait("Hardworking", 1.5f);
            character.AddTrait("Friendly", 0.8f);
            Console.WriteLine($"Added traits. Hardworking intensity: {character.GetTraitIntensity("Hardworking")}");

            // Generate random character
            var random = new Random();
            var randomCharacter = CharacterComponent.GenerateRandom(random);
            Console.WriteLine($"Generated random character: {randomCharacter.GetDescription()}");

            Console.WriteLine("Character Components Demo completed!\n");
        }

        /// <summary>
        /// Demo Skills System
        /// </summary>
        static void DemoSkillsSystem()
        {
            Console.WriteLine("=== Skills System Demo ===");

            // Create skill component
            var skills = new SkillComponent();
            Console.WriteLine("SkillComponent created with default skills");

            // Set skill levels
            skills.SetSkillLevel(SkillType.Construction, 5);
            skills.SetSkillLevel(SkillType.Mining, 3);
            skills.SetSkillLevel(SkillType.Cooking, 7);

            Console.WriteLine("Set initial skill levels:");
            foreach (var skill in skills.GetAllSkills())
            {
                if (skill.Level > 0)
                {
                    Console.WriteLine($"  {skill.Type}: Level {skill.Level}, Experience {skill.Experience:F1}");
                }
            }

            // Add experience and check for level ups
            Console.WriteLine("\nAdding experience to Construction skill...");
            for (int i = 0; i < 5; i++)
            {
                var leveledUp = skills.AddSkillExperience(SkillType.Construction, 250f);
                var constructionSkill = skills.GetSkill(SkillType.Construction);
                Console.WriteLine($"  Added 250 exp. Level: {constructionSkill.Level}, Experience: {constructionSkill.Experience:F1}" +
                    (leveledUp ? " - LEVEL UP!" : ""));
            }

            // Show highest skill
            var highestSkill = skills.GetHighestSkill();
            Console.WriteLine($"\nHighest skill: {highestSkill.Type} at level {highestSkill.Level}");

            // Show task efficiency
            var taskEfficiency = skills.GetTaskEfficiency(SkillType.Construction, SkillType.Mining);
            Console.WriteLine($"Task efficiency (Construction + Mining): {taskEfficiency:F2}");

            Console.WriteLine("Skills System Demo completed!\n");
        }

        /// <summary>
        /// Demo Needs System
        /// </summary>
        static void DemoNeedsSystem()
        {
            Console.WriteLine("=== Needs System Demo ===");

            // Create need component
            var needs = new NeedComponent();
            Console.WriteLine("NeedComponent created with default needs");

            // Show initial needs
            Console.WriteLine("Initial needs status:");
            foreach (var need in needs.GetAllNeeds())
            {
                Console.WriteLine($"  {need.GetName()}: {need.Value:P1} ({need.GetStatus()})");
            }

            // Simulate time passing
            Console.WriteLine("\nSimulating 10 seconds of time...");
            for (int i = 0; i < 10; i++)
            {
                needs.UpdateNeeds(1.0f); // 1 second
                Thread.Sleep(100); // Small delay for visualization
            }

            // Show updated needs
            Console.WriteLine("Needs after 10 seconds:");
            foreach (var need in needs.GetAllNeeds())
            {
                Console.WriteLine($"  {need.GetName()}: {need.Value:P1} ({need.GetStatus()})");
            }

            // Show most urgent need
            var mostUrgent = needs.GetMostUrgentNeed();
            Console.WriteLine($"\nMost urgent need: {mostUrgent.GetName()} (Urgency: {mostUrgent.GetUrgency():F2})");

            // Show overall happiness
            var happiness = needs.GetOverallHappiness();
            Console.WriteLine($"Overall happiness: {happiness:P1}");

            // Show needs summary
            Console.WriteLine($"Needs summary: {needs.GetNeedsSummary()}");

            // Satisfy some needs
            Console.WriteLine("\nSatisfying hunger and rest needs...");
            needs.SatisfyNeed(NeedType.Hunger, 0.8f);
            needs.SatisfyNeed(NeedType.Rest, 0.6f);

            Console.WriteLine("Updated needs summary: " + needs.GetNeedsSummary());

            Console.WriteLine("Needs System Demo completed!\n");
        }
    }

    /// <summary>
    /// Simple test component for ECS demo
    /// </summary>
    public class TestComponent : Component
    {
        public int Value { get; set; }
    }
}