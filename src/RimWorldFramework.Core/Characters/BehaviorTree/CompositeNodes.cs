using System.Linq;

namespace RimWorldFramework.Core.Characters.BehaviorTree
{
    /// <summary>
    /// 选择器节点 - 依次执行子节点直到有一个成功
    /// </summary>
    public class SelectorNode : CompositeNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            for (int i = _currentChildIndex; i < _children.Count; i++)
            {
                var result = _children[i].Execute(context);
                
                switch (result)
                {
                    case BehaviorResult.Success:
                        Reset();
                        return BehaviorResult.Success;
                    
                    case BehaviorResult.Running:
                        _currentChildIndex = i;
                        return BehaviorResult.Running;
                    
                    case BehaviorResult.Failure:
                        continue; // 尝试下一个子节点
                }
            }
            
            Reset();
            return BehaviorResult.Failure;
        }
    }

    /// <summary>
    /// 序列节点 - 依次执行子节点直到有一个失败
    /// </summary>
    public class SequenceNode : CompositeNode
    {
        public override BehaviorResult Execute(CharacterContext context)
        {
            for (int i = _currentChildIndex; i < _children.Count; i++)
            {
                var result = _children[i].Execute(context);
                
                switch (result)
                {
                    case BehaviorResult.Failure:
                        Reset();
                        return BehaviorResult.Failure;
                    
                    case BehaviorResult.Running:
                        _currentChildIndex = i;
                        return BehaviorResult.Running;
                    
                    case BehaviorResult.Success:
                        continue; // 继续下一个子节点
                }
            }
            
            Reset();
            return BehaviorResult.Success;
        }
    }

    /// <summary>
    /// 并行节点 - 同时执行所有子节点
    /// </summary>
    public class ParallelNode : CompositeNode
    {
        public enum ParallelPolicy
        {
            RequireAll,    // 需要所有子节点成功
            RequireOne     // 只需要一个子节点成功
        }

        public ParallelPolicy SuccessPolicy { get; set; } = ParallelPolicy.RequireAll;
        public ParallelPolicy FailurePolicy { get; set; } = ParallelPolicy.RequireOne;

        public override BehaviorResult Execute(CharacterContext context)
        {
            int successCount = 0;
            int failureCount = 0;
            int runningCount = 0;

            foreach (var child in _children)
            {
                var result = child.Execute(context);
                
                switch (result)
                {
                    case BehaviorResult.Success:
                        successCount++;
                        break;
                    case BehaviorResult.Failure:
                        failureCount++;
                        break;
                    case BehaviorResult.Running:
                        runningCount++;
                        break;
                }
            }

            // 检查失败条件
            if (FailurePolicy == ParallelPolicy.RequireOne && failureCount > 0)
            {
                Reset();
                return BehaviorResult.Failure;
            }
            
            if (FailurePolicy == ParallelPolicy.RequireAll && failureCount == _children.Count)
            {
                Reset();
                return BehaviorResult.Failure;
            }

            // 检查成功条件
            if (SuccessPolicy == ParallelPolicy.RequireOne && successCount > 0)
            {
                Reset();
                return BehaviorResult.Success;
            }
            
            if (SuccessPolicy == ParallelPolicy.RequireAll && successCount == _children.Count)
            {
                Reset();
                return BehaviorResult.Success;
            }

            // 如果有节点还在运行，返回运行状态
            if (runningCount > 0)
            {
                return BehaviorResult.Running;
            }

            // 默认返回失败
            Reset();
            return BehaviorResult.Failure;
        }
    }

    /// <summary>
    /// 随机选择器节点 - 随机选择一个子节点执行
    /// </summary>
    public class RandomSelectorNode : CompositeNode
    {
        private readonly System.Random _random = new();
        private int _selectedIndex = -1;

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_children.Count == 0)
                return BehaviorResult.Failure;

            // 如果没有选择节点或上次执行完成，重新选择
            if (_selectedIndex == -1 || _lastResult != BehaviorResult.Running)
            {
                _selectedIndex = _random.Next(_children.Count);
            }

            var result = _children[_selectedIndex].Execute(context);
            
            if (result != BehaviorResult.Running)
            {
                _selectedIndex = -1;
                Reset();
            }

            return result;
        }

        public override void Reset()
        {
            base.Reset();
            _selectedIndex = -1;
        }
    }

    /// <summary>
    /// 权重选择器节点 - 根据权重选择子节点
    /// </summary>
    public class WeightedSelectorNode : CompositeNode
    {
        private readonly System.Random _random = new();
        private readonly List<float> _weights = new();
        private int _selectedIndex = -1;

        /// <summary>
        /// 添加带权重的子节点
        /// </summary>
        public void AddChild(BehaviorNode child, float weight)
        {
            AddChild(child);
            _weights.Add(weight);
        }

        public override BehaviorResult Execute(CharacterContext context)
        {
            if (_children.Count == 0)
                return BehaviorResult.Failure;

            // 如果没有选择节点或上次执行完成，根据权重选择
            if (_selectedIndex == -1 || _lastResult != BehaviorResult.Running)
            {
                _selectedIndex = SelectWeightedIndex();
            }

            var result = _children[_selectedIndex].Execute(context);
            
            if (result != BehaviorResult.Running)
            {
                _selectedIndex = -1;
                Reset();
            }

            return result;
        }

        private int SelectWeightedIndex()
        {
            var totalWeight = _weights.Sum();
            if (totalWeight <= 0) return 0;

            var randomValue = _random.NextSingle() * totalWeight;
            var currentWeight = 0f;

            for (int i = 0; i < _weights.Count; i++)
            {
                currentWeight += _weights[i];
                if (randomValue <= currentWeight)
                    return i;
            }

            return _weights.Count - 1;
        }

        public override void Reset()
        {
            base.Reset();
            _selectedIndex = -1;
        }
    }
}