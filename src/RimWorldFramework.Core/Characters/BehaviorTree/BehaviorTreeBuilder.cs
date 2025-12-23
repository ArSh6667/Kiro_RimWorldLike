using System;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Characters.BehaviorTree
{
    /// <summary>
    /// 行为树构建器 - 提供流畅的API来构建行为树
    /// </summary>
    public class BehaviorTreeBuilder
    {
        private BehaviorNode? _currentNode;
        private readonly System.Collections.Generic.Stack<CompositeNode> _nodeStack = new();

        /// <summary>
        /// 创建选择器节点
        /// </summary>
        public BehaviorTreeBuilder Selector(string name = "选择器")
        {
            var selector = new SelectorNode { Name = name };
            AddNode(selector);
            _nodeStack.Push(selector);
            return this;
        }

        /// <summary>
        /// 创建序列节点
        /// </summary>
        public BehaviorTreeBuilder Sequence(string name = "序列")
        {
            var sequence = new SequenceNode { Name = name };
            AddNode(sequence);
            _nodeStack.Push(sequence);
            return this;
        }

        /// <summary>
        /// 创建并行节点
        /// </summary>
        public BehaviorTreeBuilder Parallel(string name = "并行", 
            ParallelNode.ParallelPolicy successPolicy = ParallelNode.ParallelPolicy.RequireAll,
            ParallelNode.ParallelPolicy failurePolicy = ParallelNode.ParallelPolicy.RequireOne)
        {
            var parallel = new ParallelNode 
            { 
                Name = name,
                SuccessPolicy = successPolicy,
                FailurePolicy = failurePolicy
            };
            AddNode(parallel);
            _nodeStack.Push(parallel);
            return this;
        }

        /// <summary>
        /// 创建随机选择器节点
        /// </summary>
        public BehaviorTreeBuilder RandomSelector(string name = "随机选择器")
        {
            var randomSelector = new RandomSelectorNode { Name = name };
            AddNode(randomSelector);
            _nodeStack.Push(randomSelector);
            return this;
        }

        /// <summary>
        /// 创建权重选择器节点
        /// </summary>
        public BehaviorTreeBuilder WeightedSelector(string name = "权重选择器")
        {
            var weightedSelector = new WeightedSelectorNode { Name = name };
            AddNode(weightedSelector);
            _nodeStack.Push(weightedSelector);
            return this;
        }

        /// <summary>
        /// 结束当前复合节点
        /// </summary>
        public BehaviorTreeBuilder End()
        {
            if (_nodeStack.Count > 0)
            {
                _nodeStack.Pop();
            }
            return this;
        }

        /// <summary>
        /// 添加反转器装饰器
        /// </summary>
        public BehaviorTreeBuilder Inverter(string name = "反转器")
        {
            var inverter = new InverterNode { Name = name };
            AddNode(inverter);
            return this;
        }

        /// <summary>
        /// 添加重复器装饰器
        /// </summary>
        public BehaviorTreeBuilder Repeater(int count, string name = "重复器")
        {
            var repeater = new RepeaterNode { RepeatCount = count, Name = name };
            AddNode(repeater);
            return this;
        }

        /// <summary>
        /// 添加冷却装饰器
        /// </summary>
        public BehaviorTreeBuilder Cooldown(float cooldownTime, string name = "冷却")
        {
            var cooldown = new CooldownNode { CooldownTime = cooldownTime, Name = name };
            AddNode(cooldown);
            return this;
        }

        /// <summary>
        /// 添加条件装饰器
        /// </summary>
        public BehaviorTreeBuilder Condition(Func<CharacterContext, bool> condition, string name = "条件")
        {
            var conditional = new ConditionalNode(condition) { Name = name };
            AddNode(conditional);
            return this;
        }

        /// <summary>
        /// 添加超时装饰器
        /// </summary>
        public BehaviorTreeBuilder Timeout(float timeoutDuration, string name = "超时")
        {
            var timeout = new TimeoutNode { TimeoutDuration = timeoutDuration, Name = name };
            AddNode(timeout);
            return this;
        }

        /// <summary>
        /// 添加移动到位置动作
        /// </summary>
        public BehaviorTreeBuilder MoveTo(Vector3 position, float acceptableDistance = 0.5f)
        {
            var moveNode = new MoveToPositionNode(position) { AcceptableDistance = acceptableDistance };
            AddNode(moveNode);
            return this;
        }

        /// <summary>
        /// 添加满足需求动作
        /// </summary>
        public BehaviorTreeBuilder SatisfyNeed(NeedType needType, float amount = 0.5f, float duration = 2.0f)
        {
            var satisfyNode = new SatisfyNeedNode(needType) 
            { 
                SatisfactionAmount = amount, 
                Duration = duration 
            };
            AddNode(satisfyNode);
            return this;
        }

        /// <summary>
        /// 添加等待动作
        /// </summary>
        public BehaviorTreeBuilder Wait(float waitTime)
        {
            var waitNode = new WaitNode(waitTime);
            AddNode(waitNode);
            return this;
        }

        /// <summary>
        /// 添加检查需求条件
        /// </summary>
        public BehaviorTreeBuilder CheckNeed(NeedType needType, float threshold = 0.3f, bool checkBelow = true)
        {
            var checkNode = new CheckNeedNode(needType, threshold, checkBelow);
            AddNode(checkNode);
            return this;
        }

        /// <summary>
        /// 添加检查技能条件
        /// </summary>
        public BehaviorTreeBuilder CheckSkill(SkillType skillType, int minLevel)
        {
            var checkNode = new CheckSkillNode(skillType, minLevel);
            AddNode(checkNode);
            return this;
        }

        /// <summary>
        /// 添加自定义动作
        /// </summary>
        public BehaviorTreeBuilder Action(string name, Func<CharacterContext, BehaviorResult> action)
        {
            var actionNode = new CustomActionNode(name, action);
            AddNode(actionNode);
            return this;
        }

        /// <summary>
        /// 添加日志动作
        /// </summary>
        public BehaviorTreeBuilder Log(string message)
        {
            var logNode = new LogNode(message);
            AddNode(logNode);
            return this;
        }

        /// <summary>
        /// 添加空闲动作
        /// </summary>
        public BehaviorTreeBuilder Idle()
        {
            var idleNode = new IdleNode();
            AddNode(idleNode);
            return this;
        }

        /// <summary>
        /// 构建行为树
        /// </summary>
        public BehaviorNode Build()
        {
            if (_currentNode == null)
                throw new InvalidOperationException("行为树为空，无法构建");

            return _currentNode;
        }

        /// <summary>
        /// 添加节点到当前构建的树中
        /// </summary>
        private void AddNode(BehaviorNode node)
        {
            if (_currentNode == null)
            {
                _currentNode = node;
            }
            else if (_nodeStack.Count > 0)
            {
                var parent = _nodeStack.Peek();
                parent.AddChild(node);
            }
            else if (_currentNode is DecoratorNode decorator)
            {
                decorator.SetChild(node);
            }
            else
            {
                throw new InvalidOperationException("无法添加节点：当前节点不是复合节点或装饰器节点");
            }
        }

        /// <summary>
        /// 创建默认的角色行为树
        /// </summary>
        public static BehaviorNode CreateDefaultCharacterBehavior()
        {
            return new BehaviorTreeBuilder()
                .Selector("角色主行为")
                    // 优先处理关键需求
                    .Sequence("处理饥饿")
                        .CheckNeed(NeedType.Hunger, 0.3f, true)
                        .SatisfyNeed(NeedType.Hunger, 0.8f, 3.0f)
                    .End()
                    .Sequence("处理休息")
                        .CheckNeed(NeedType.Rest, 0.2f, true)
                        .SatisfyNeed(NeedType.Rest, 0.9f, 5.0f)
                    .End()
                    // 处理其他需求
                    .Sequence("处理娱乐")
                        .CheckNeed(NeedType.Recreation, 0.4f, true)
                        .SatisfyNeed(NeedType.Recreation, 0.6f, 2.0f)
                    .End()
                    // 默认空闲行为
                    .Idle()
                .End()
                .Build();
        }

        /// <summary>
        /// 创建工作导向的行为树
        /// </summary>
        public static BehaviorNode CreateWorkerBehavior()
        {
            return new BehaviorTreeBuilder()
                .Selector("工人行为")
                    // 关键需求检查
                    .Sequence("生存需求")
                        .CheckNeed(NeedType.Hunger, 0.2f, true)
                        .SatisfyNeed(NeedType.Hunger, 0.8f, 2.0f)
                    .End()
                    .Sequence("休息需求")
                        .CheckNeed(NeedType.Rest, 0.1f, true)
                        .SatisfyNeed(NeedType.Rest, 1.0f, 6.0f)
                    .End()
                    // 工作行为（这里可以扩展具体的工作任务）
                    .Sequence("工作")
                        .Log("开始工作")
                        .Wait(5.0f)
                        .Log("完成工作")
                    .End()
                    // 空闲
                    .Idle()
                .End()
                .Build();
        }
    }
}