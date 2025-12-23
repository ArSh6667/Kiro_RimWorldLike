using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldFramework.Core.Characters.BehaviorTree
{
    /// <summary>
    /// 行为树管理器 - 管理所有角色的行为树
    /// </summary>
    public class BehaviorTreeManager
    {
        private readonly Dictionary<uint, BehaviorTreeInstance> _behaviorTrees = new();
        private readonly Dictionary<string, BehaviorNode> _behaviorTemplates = new();

        /// <summary>
        /// 行为树实例
        /// </summary>
        private class BehaviorTreeInstance
        {
            public BehaviorNode RootNode { get; set; }
            public CharacterContext Context { get; set; }
            public bool IsActive { get; set; } = true;
            public float LastUpdateTime { get; set; }

            public BehaviorTreeInstance(BehaviorNode rootNode, CharacterContext context)
            {
                RootNode = rootNode;
                Context = context;
            }
        }

        /// <summary>
        /// 注册行为树模板
        /// </summary>
        public void RegisterTemplate(string templateName, BehaviorNode behaviorTree)
        {
            if (string.IsNullOrEmpty(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));
            
            if (behaviorTree == null)
                throw new ArgumentNullException(nameof(behaviorTree));

            _behaviorTemplates[templateName] = behaviorTree;
        }

        /// <summary>
        /// 为角色分配行为树
        /// </summary>
        public void AssignBehaviorTree(CharacterEntity character, string templateName)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));

            if (!_behaviorTemplates.TryGetValue(templateName, out var template))
                throw new ArgumentException($"未找到行为树模板: {templateName}", nameof(templateName));

            // 克隆模板（简单实现，实际可能需要深度克隆）
            var behaviorTree = CloneBehaviorTree(template);
            var context = new CharacterContext(character, 0f);
            var instance = new BehaviorTreeInstance(behaviorTree, context);

            _behaviorTrees[character.Id] = instance;
        }

        /// <summary>
        /// 为角色分配自定义行为树
        /// </summary>
        public void AssignBehaviorTree(CharacterEntity character, BehaviorNode behaviorTree)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            
            if (behaviorTree == null)
                throw new ArgumentNullException(nameof(behaviorTree));

            var context = new CharacterContext(character, 0f);
            var instance = new BehaviorTreeInstance(behaviorTree, context);

            _behaviorTrees[character.Id] = instance;
        }

        /// <summary>
        /// 移除角色的行为树
        /// </summary>
        public void RemoveBehaviorTree(uint characterId)
        {
            _behaviorTrees.Remove(characterId);
        }

        /// <summary>
        /// 更新所有活跃的行为树
        /// </summary>
        public void UpdateBehaviorTrees(float deltaTime)
        {
            var currentTime = Time.time;
            
            foreach (var kvp in _behaviorTrees.ToList())
            {
                var instance = kvp.Value;
                
                if (!instance.IsActive)
                    continue;

                // 更新上下文
                instance.Context.DeltaTime = deltaTime;
                instance.LastUpdateTime = currentTime;

                try
                {
                    // 执行行为树
                    var result = instance.RootNode.Execute(instance.Context);
                    
                    // 根据结果处理（可以添加更多逻辑）
                    if (result == BehaviorResult.Failure)
                    {
                        // 行为树失败，可能需要重置或切换到备用行为
                        instance.RootNode.Reset();
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误并暂停该角色的行为树
                    Console.WriteLine($"角色 {instance.Context.Character.Name} 的行为树执行出错: {ex.Message}");
                    instance.IsActive = false;
                }
            }
        }

        /// <summary>
        /// 暂停角色的行为树
        /// </summary>
        public void PauseBehaviorTree(uint characterId)
        {
            if (_behaviorTrees.TryGetValue(characterId, out var instance))
            {
                instance.IsActive = false;
            }
        }

        /// <summary>
        /// 恢复角色的行为树
        /// </summary>
        public void ResumeBehaviorTree(uint characterId)
        {
            if (_behaviorTrees.TryGetValue(characterId, out var instance))
            {
                instance.IsActive = true;
            }
        }

        /// <summary>
        /// 重置角色的行为树
        /// </summary>
        public void ResetBehaviorTree(uint characterId)
        {
            if (_behaviorTrees.TryGetValue(characterId, out var instance))
            {
                instance.RootNode.Reset();
                instance.Context.Blackboard.Clear();
            }
        }

        /// <summary>
        /// 获取角色的行为树状态
        /// </summary>
        public BehaviorTreeStatus? GetBehaviorTreeStatus(uint characterId)
        {
            if (!_behaviorTrees.TryGetValue(characterId, out var instance))
                return null;

            return new BehaviorTreeStatus
            {
                CharacterId = characterId,
                IsActive = instance.IsActive,
                LastUpdateTime = instance.LastUpdateTime,
                BlackboardData = new Dictionary<string, object>(instance.Context.Blackboard)
            };
        }

        /// <summary>
        /// 获取所有注册的模板名称
        /// </summary>
        public IEnumerable<string> GetTemplateNames()
        {
            return _behaviorTemplates.Keys.ToList();
        }

        /// <summary>
        /// 获取活跃的行为树数量
        /// </summary>
        public int GetActiveBehaviorTreeCount()
        {
            return _behaviorTrees.Values.Count(instance => instance.IsActive);
        }

        /// <summary>
        /// 清理所有行为树
        /// </summary>
        public void Clear()
        {
            _behaviorTrees.Clear();
        }

        /// <summary>
        /// 初始化默认模板
        /// </summary>
        public void InitializeDefaultTemplates()
        {
            RegisterTemplate("default", BehaviorTreeBuilder.CreateDefaultCharacterBehavior());
            RegisterTemplate("worker", BehaviorTreeBuilder.CreateWorkerBehavior());
        }

        /// <summary>
        /// 简单的行为树克隆（实际实现可能需要更复杂的深度克隆）
        /// </summary>
        private BehaviorNode CloneBehaviorTree(BehaviorNode original)
        {
            // 这里是一个简化的实现
            // 实际项目中可能需要实现完整的深度克隆
            return original;
        }
    }

    /// <summary>
    /// 行为树状态信息
    /// </summary>
    public class BehaviorTreeStatus
    {
        public uint CharacterId { get; set; }
        public bool IsActive { get; set; }
        public float LastUpdateTime { get; set; }
        public Dictionary<string, object> BlackboardData { get; set; } = new();
    }
}