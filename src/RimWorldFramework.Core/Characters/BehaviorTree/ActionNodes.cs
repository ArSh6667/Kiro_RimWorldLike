using System;
using RimWorldFramework.Core.Characters.Components;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Characters.BehaviorTree
{
    /// <summary>
    /// 移动到位置节点
    /// </summary>
    public class MoveToPositionNode : LeafNode
    {
        public Vector3 TargetPosition { get; set; }
        public float AcceptableDistance { get; set; } = 0.5f;

        public MoveToPositionNode(Vector3 targetPosition)
        {
            TargetPosition = targetPosition;
            Name = "移动到位置";
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            var position = context.Character.Position;
            if (position == null)
                return BehaviorResult.Failure;

            var distance = position.DistanceTo(TargetPosition);
            
            // 如果已经到达目标位置
            if (distance <= AcceptableDistance)
            {
                position.StopMovement();
                return BehaviorResult.Success;
            }

            // 开始移动
            if (!position.IsMoving)
            {
                position.StartMovementTo(TargetPosition, Time.time);
            }

            return BehaviorResult.Running;
        }
    }

    /// <summary>
    /// 满足需求节点
    /// </summary>
    public class SatisfyNeedNode : LeafNode
    {
        public NeedType NeedType { get; set; }
        public float SatisfactionAmount { get; set; } = 0.5f;
        public float Duration { get; set; } = 2.0f;

        private float _startTime = -1f;

        public SatisfyNeedNode(NeedType needType)
        {
            NeedType = needType;
            Name = $"满足{needType}需求";
        }

        protected override void OnEnter(CharacterContext context)
        {
            _startTime = Time.time;
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            var needs = context.Character.Needs;
            if (needs == null)
                return BehaviorResult.Failure;

            var currentTime = Time.time;
            var elapsedTime = currentTime - _startTime;

            // 检查是否完成
            if (elapsedTime >= Duration)
            {
                needs.SatisfyNeed(NeedType, SatisfactionAmount);
                return BehaviorResult.Success;
            }

            return BehaviorResult.Running;
        }

        protected override void OnExit(CharacterContext context)
        {
            _startTime = -1f;
        }
    }

    /// <summary>
    /// 等待节点
    /// </summary>
    public class WaitNode : LeafNode
    {
        public float WaitTime { get; set; }
        private float _startTime = -1f;

        public WaitNode(float waitTime)
        {
            WaitTime = waitTime;
            Name = $"等待{waitTime}秒";
        }

        protected override void OnEnter(CharacterContext context)
        {
            _startTime = Time.time;
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            var elapsedTime = Time.time - _startTime;
            
            if (elapsedTime >= WaitTime)
                return BehaviorResult.Success;
            
            return BehaviorResult.Running;
        }

        protected override void OnExit(CharacterContext context)
        {
            _startTime = -1f;
        }
    }

    /// <summary>
    /// 检查需求节点
    /// </summary>
    public class CheckNeedNode : LeafNode
    {
        public NeedType NeedType { get; set; }
        public float Threshold { get; set; } = 0.3f;
        public bool CheckBelow { get; set; } = true; // true表示检查是否低于阈值

        public CheckNeedNode(NeedType needType, float threshold = 0.3f, bool checkBelow = true)
        {
            NeedType = needType;
            Threshold = threshold;
            CheckBelow = checkBelow;
            Name = $"检查{needType}需求";
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            var needs = context.Character.Needs;
            if (needs == null)
                return BehaviorResult.Failure;

            var need = needs.GetNeed(NeedType);
            var condition = CheckBelow ? need.Value < Threshold : need.Value >= Threshold;
            
            return condition ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }

    /// <summary>
    /// 检查技能节点
    /// </summary>
    public class CheckSkillNode : LeafNode
    {
        public SkillType SkillType { get; set; }
        public int MinLevel { get; set; }

        public CheckSkillNode(SkillType skillType, int minLevel)
        {
            SkillType = skillType;
            MinLevel = minLevel;
            Name = $"检查{skillType}技能";
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            var skills = context.Character.Skills;
            if (skills == null)
                return BehaviorResult.Failure;

            var skill = skills.GetSkill(SkillType);
            return skill.Level >= MinLevel ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }

    /// <summary>
    /// 自定义动作节点
    /// </summary>
    public class CustomActionNode : LeafNode
    {
        private readonly Func<CharacterContext, BehaviorResult> _action;

        public CustomActionNode(string name, Func<CharacterContext, BehaviorResult> action)
        {
            Name = name;
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            return _action(context);
        }
    }

    /// <summary>
    /// 日志节点 - 用于调试
    /// </summary>
    public class LogNode : LeafNode
    {
        public string Message { get; set; }

        public LogNode(string message)
        {
            Message = message;
            Name = "日志";
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            Console.WriteLine($"[{context.Character.Name}] {Message}");
            return BehaviorResult.Success;
        }
    }

    /// <summary>
    /// 空闲节点 - 角色无事可做时的默认行为
    /// </summary>
    public class IdleNode : LeafNode
    {
        private readonly System.Random _random = new();
        private float _nextActionTime = -1f;

        public IdleNode()
        {
            Name = "空闲";
        }

        protected override void OnEnter(CharacterContext context)
        {
            // 随机设置下次行动时间
            _nextActionTime = Time.time + _random.NextSingle() * 3f + 1f;
        }

        protected override BehaviorResult OnUpdate(CharacterContext context)
        {
            if (Time.time >= _nextActionTime)
            {
                // 执行一些随机的空闲行为
                var actions = new[] { "四处张望", "整理物品", "伸懒腰", "思考" };
                var action = actions[_random.Next(actions.Length)];
                
                context.SetBlackboardValue("last_idle_action", action);
                
                return BehaviorResult.Success;
            }

            return BehaviorResult.Running;
        }

        protected override void OnExit(CharacterContext context)
        {
            _nextActionTime = -1f;
        }
    }
}