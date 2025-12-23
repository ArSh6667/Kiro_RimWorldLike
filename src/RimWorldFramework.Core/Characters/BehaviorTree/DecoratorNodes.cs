using System;

namespace RimWorldFramework.Core.Characters.BehaviorTree
{
    /// <summary>
    /// 反转器节点 - 反转子节点的结果
    /// </summary>
    public class InverterNode : DecoratorNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            var result = _child.Execute(context);
            
            return result switch
            {
                BehaviorResult.Success => BehaviorResult.Failure,
                BehaviorResult.Failure => BehaviorResult.Success,
                BehaviorResult.Running => BehaviorResult.Running,
                _ => BehaviorResult.Failure
            };
        }
    }

    /// <summary>
    /// 重复器节点 - 重复执行子节点指定次数
    /// </summary>
    public class RepeaterNode : DecoratorNode
    {
        public int RepeatCount { get; set; } = 1;
        private int _currentCount = 0;

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            while (_currentCount < RepeatCount)
            {
                var result = _child.Execute(context);
                
                if (result == BehaviorResult.Running)
                    return BehaviorResult.Running;
                
                if (result == BehaviorResult.Failure)
                {
                    Reset();
                    return BehaviorResult.Failure;
                }
                
                _currentCount++;
                _child.Reset();
            }

            Reset();
            return BehaviorResult.Success;
        }

        public override void Reset()
        {
            base.Reset();
            _currentCount = 0;
        }
    }

    /// <summary>
    /// 直到失败节点 - 重复执行子节点直到失败
    /// </summary>
    public class UntilFailNode : DecoratorNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            var result = _child.Execute(context);
            
            return result switch
            {
                BehaviorResult.Failure => BehaviorResult.Success,
                BehaviorResult.Success => BehaviorResult.Running,
                BehaviorResult.Running => BehaviorResult.Running,
                _ => BehaviorResult.Failure
            };
        }
    }

    /// <summary>
    /// 直到成功节点 - 重复执行子节点直到成功
    /// </summary>
    public class UntilSuccessNode : DecoratorNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            var result = _child.Execute(context);
            
            return result switch
            {
                BehaviorResult.Success => BehaviorResult.Success,
                BehaviorResult.Failure => BehaviorResult.Running,
                BehaviorResult.Running => BehaviorResult.Running,
                _ => BehaviorResult.Failure
            };
        }
    }

    /// <summary>
    /// 冷却节点 - 在指定时间内阻止子节点执行
    /// </summary>
    public class CooldownNode : DecoratorNode
    {
        public float CooldownTime { get; set; } = 1.0f;
        private float _lastExecutionTime = -1f;

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            var currentTime = Time.time;
            
            // 检查冷却时间
            if (_lastExecutionTime >= 0 && currentTime - _lastExecutionTime < CooldownTime)
            {
                return BehaviorResult.Failure;
            }

            var result = _child.Execute(context);
            
            if (result != BehaviorResult.Running)
            {
                _lastExecutionTime = currentTime;
            }

            return result;
        }

        public override void Reset()
        {
            base.Reset();
            _lastExecutionTime = -1f;
        }
    }

    /// <summary>
    /// 条件节点 - 根据条件决定是否执行子节点
    /// </summary>
    public class ConditionalNode : DecoratorNode
    {
        private readonly Func<CharacterContext, bool> _condition;

        public ConditionalNode(Func<CharacterContext, bool> condition)
        {
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            if (!_condition(context))
                return BehaviorResult.Failure;

            return _child.Execute(context);
        }
    }

    /// <summary>
    /// 超时节点 - 限制子节点的执行时间
    /// </summary>
    public class TimeoutNode : DecoratorNode
    {
        public float TimeoutDuration { get; set; } = 5.0f;
        private float _startTime = -1f;

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            var currentTime = Time.time;
            
            // 记录开始时间
            if (_startTime < 0)
            {
                _startTime = currentTime;
            }

            // 检查是否超时
            if (currentTime - _startTime >= TimeoutDuration)
            {
                Reset();
                return BehaviorResult.Failure;
            }

            var result = _child.Execute(context);
            
            if (result != BehaviorResult.Running)
            {
                Reset();
            }

            return result;
        }

        public override void Reset()
        {
            base.Reset();
            _startTime = -1f;
        }
    }

    /// <summary>
    /// 成功节点 - 总是返回成功
    /// </summary>
    public class SucceederNode : DecoratorNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Success;

            _child.Execute(context);
            return BehaviorResult.Success;
        }
    }

    /// <summary>
    /// 失败节点 - 总是返回失败
    /// </summary>
    public class FailerNode : DecoratorNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_child == null)
                return BehaviorResult.Failure;

            _child.Execute(context);
            return BehaviorResult.Failure;
        }
    }
}