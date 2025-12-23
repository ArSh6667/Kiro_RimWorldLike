using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Characters.BehaviorTree;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Systems;

namespace RimWorldFramework.Core.Characters
{
    /// <summary>
    /// 角色系统 - 管理所有角色的行为和状态
    /// </summary>
    public class CharacterSystem : IGameSystem
    {
        private readonly Dictionary<uint, CharacterEntity> _characters = new();
        private readonly BehaviorTreeManager _behaviorTreeManager = new();
        private readonly IEntityManager _entityManager;

        public int Priority => 100;

        public CharacterSystem(IEntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public void Initialize()
        {
            // 初始化默认行为树模板
            _behaviorTreeManager.InitializeDefaultTemplates();
            
            Console.WriteLine("角色系统已初始化");
        }

        public void Update(float deltaTime)
        {
            // 更新所有角色
            foreach (var character in _characters.Values.ToList())
            {
                try
                {
                    character.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新角色 {character.Name} 时出错: {ex.Message}");
                }
            }

            // 更新行为树管理器
            _behaviorTreeManager.UpdateBehaviorTrees(deltaTime);
        }

        public void Shutdown()
        {
            _behaviorTreeManager.Clear();
            _characters.Clear();
            Console.WriteLine("角色系统已关闭");
        }

        /// <summary>
        /// 注册角色
        /// </summary>
        public void RegisterCharacter(CharacterEntity character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (_characters.ContainsKey(character.Id))
                throw new ArgumentException($"角色ID {character.Id} 已存在", nameof(character));

            // 添加角色组件到ECS系统
            _entityManager.AddComponent(character.Id, new PositionComponent());
            _entityManager.AddComponent(character.Id, new SkillComponent());
            _entityManager.AddComponent(character.Id, new NeedComponent());
            _entityManager.AddComponent(character.Id, new InventoryComponent());

            // 设置组件引用
            character.SetComponentReferences(_entityManager);

            // 注册角色
            _characters[character.Id] = character;

            // 分配默认行为树
            _behaviorTreeManager.AssignBehaviorTree(character, "default");

            Console.WriteLine($"角色 {character.Name} 已注册");
        }

        /// <summary>
        /// 注销角色
        /// </summary>
        public void UnregisterCharacter(uint characterId)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                // 移除行为树
                _behaviorTreeManager.RemoveBehaviorTree(characterId);

                // 移除组件
                _entityManager.RemoveComponent<PositionComponent>(characterId);
                _entityManager.RemoveComponent<SkillComponent>(characterId);
                _entityManager.RemoveComponent<NeedComponent>(characterId);
                _entityManager.RemoveComponent<InventoryComponent>(characterId);

                // 移除角色
                _characters.Remove(characterId);

                Console.WriteLine($"角色 {character.Name} 已注销");
            }
        }

        /// <summary>
        /// 获取角色
        /// </summary>
        public CharacterEntity? GetCharacter(uint characterId)
        {
            return _characters.TryGetValue(characterId, out var character) ? character : null;
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        public IEnumerable<CharacterEntity> GetAllCharacters()
        {
            return _characters.Values.ToList();
        }

        /// <summary>
        /// 根据名称查找角色
        /// </summary>
        public CharacterEntity? FindCharacterByName(string name)
        {
            return _characters.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 为角色分配行为树模板
        /// </summary>
        public void AssignBehaviorTree(uint characterId, string templateName)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                _behaviorTreeManager.AssignBehaviorTree(character, templateName);
            }
        }

        /// <summary>
        /// 为角色分配自定义行为树
        /// </summary>
        public void AssignBehaviorTree(uint characterId, BehaviorNode behaviorTree)
        {
            if (_characters.TryGetValue(characterId, out var character))
            {
                _behaviorTreeManager.AssignBehaviorTree(character, behaviorTree);
                character.BehaviorTree = behaviorTree;
            }
        }

        /// <summary>
        /// 暂停角色行为
        /// </summary>
        public void PauseCharacterBehavior(uint characterId)
        {
            _behaviorTreeManager.PauseBehaviorTree(characterId);
        }

        /// <summary>
        /// 恢复角色行为
        /// </summary>
        public void ResumeCharacterBehavior(uint characterId)
        {
            _behaviorTreeManager.ResumeBehaviorTree(characterId);
        }

        /// <summary>
        /// 重置角色行为
        /// </summary>
        public void ResetCharacterBehavior(uint characterId)
        {
            _behaviorTreeManager.ResetBehaviorTree(characterId);
        }

        /// <summary>
        /// 获取角色行为状态
        /// </summary>
        public BehaviorTreeStatus? GetCharacterBehaviorStatus(uint characterId)
        {
            return _behaviorTreeManager.GetBehaviorTreeStatus(characterId);
        }

        /// <summary>
        /// 注册行为树模板
        /// </summary>
        public void RegisterBehaviorTreeTemplate(string templateName, BehaviorNode behaviorTree)
        {
            _behaviorTreeManager.RegisterTemplate(templateName, behaviorTree);
        }

        /// <summary>
        /// 获取所有行为树模板名称
        /// </summary>
        public IEnumerable<string> GetBehaviorTreeTemplates()
        {
            return _behaviorTreeManager.GetTemplateNames();
        }

        /// <summary>
        /// 创建随机角色
        /// </summary>
        public CharacterEntity CreateRandomCharacter(Random? random = null)
        {
            random ??= new Random();
            
            var character = CharacterEntity.GenerateRandom(random);
            
            // 随机化技能
            if (character.Skills != null)
            {
                character.Skills.RandomizeSkills(random, 0, 15);
            }

            // 随机化需求
            if (character.Needs != null)
            {
                character.Needs.RandomizeNeeds(random, 0.4f, 1.0f);
            }

            // 添加一些随机物品
            if (character.Inventory != null)
            {
                character.Inventory.GenerateRandomItems(random, random.Next(3, 8));
            }

            return character;
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public CharacterSystemStats GetStats()
        {
            var characters = _characters.Values.ToList();
            
            return new CharacterSystemStats
            {
                TotalCharacters = characters.Count,
                ActiveBehaviorTrees = _behaviorTreeManager.GetActiveBehaviorTreeCount(),
                AverageHappiness = characters.Any() ? characters.Average(c => c.Needs?.GetOverallHappiness() ?? 0f) : 0f,
                CharactersWithCriticalNeeds = characters.Count(c => c.Needs?.HasCriticalNeeds() ?? false),
                AvailableTemplates = _behaviorTreeManager.GetTemplateNames().Count()
            };
        }
    }

    /// <summary>
    /// 角色系统统计信息
    /// </summary>
    public class CharacterSystemStats
    {
        public int TotalCharacters { get; set; }
        public int ActiveBehaviorTrees { get; set; }
        public float AverageHappiness { get; set; }
        public int CharactersWithCriticalNeeds { get; set; }
        public int AvailableTemplates { get; set; }

        public override string ToString()
        {
            return $"角色总数: {TotalCharacters}, " +
                   $"活跃行为树: {ActiveBehaviorTrees}, " +
                   $"平均幸福度: {AverageHappiness:F2}, " +
                   $"有关键需求的角色: {CharactersWithCriticalNeeds}, " +
                   $"可用模板: {AvailableTemplates}";
        }
    }
}