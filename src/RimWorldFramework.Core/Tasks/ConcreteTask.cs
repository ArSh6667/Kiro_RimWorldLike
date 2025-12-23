using System;
using RimWorldFramework.Core.Characters.Components;

namespace RimWorldFramework.Core.Tasks
{
    /// <summary>
    /// 具体任务实现 - 通用任务类
    /// </summary>
    public class ConcreteTask : BaseTask
    {
        private float _workDone = 0f;
        private readonly float _totalWork;

        public ConcreteTask(TaskDefinition definition) : base(definition)
        {
            _totalWork = definition.EstimatedDuration * 100f; // 假设每秒100个工作单位
        }

        protected override TaskResult OnUpdate(float deltaTime)
        {
            // 计算工作效率
            var efficiency = CalculateWorkEfficiency();
            
            // 执行工作
            var workThisFrame = efficiency * deltaTime * 100f;
            _workDone += workThisFrame;

            // 更新进度
            SetProgress(_workDone / _totalWork);

            // 检查是否完成
            if (_workDone >= _totalWork)
            {
                return TaskResult.Success;
            }

            return TaskResult.InProgress;
        }

        protected override void OnReset()
        {
            _workDone = 0f;
        }

        /// <summary>
        /// 计算工作效率
        /// </summary>
        private float CalculateWorkEfficiency()
        {
            float totalEfficiency = 0f;
            int characterCount = AssignedCharacters.Count;

            if (characterCount == 0) return 0f;

            // 基础效率（假设每个角色都有基础效率）
            float baseEfficiency = 1.0f;

            // 根据任务类型和技能需求计算效率
            foreach (var requirement in Definition.SkillRequirements)
            {
                // 这里简化处理，实际应该获取分配角色的技能
                // 假设平均技能等级为需求等级
                float skillEfficiency = Math.Max(0.1f, requirement.MinLevel / 20f);
                totalEfficiency += skillEfficiency * requirement.Weight;
            }

            // 如果没有技能需求，使用基础效率
            if (Definition.SkillRequirements.Count == 0)
            {
                totalEfficiency = baseEfficiency;
            }

            // 多人协作效率调整
            if (characterCount > 1)
            {
                // 多人协作有额外效率，但边际递减
                float collaborationBonus = 1f + (characterCount - 1) * 0.3f;
                totalEfficiency *= collaborationBonus;
            }

            return Math.Max(0.1f, totalEfficiency);
        }
    }

    /// <summary>
    /// 建造任务
    /// </summary>
    public class ConstructionTask : BaseTask
    {
        private float _constructionProgress = 0f;
        private readonly float _requiredWork;

        public ConstructionTask(TaskDefinition definition) : base(definition)
        {
            _requiredWork = definition.EstimatedDuration * 150f; // 建造需要更多工作
        }

        protected override TaskResult OnUpdate(float deltaTime)
        {
            // 检查是否有建造技能的角色
            var efficiency = CalculateConstructionEfficiency();
            
            if (efficiency <= 0)
            {
                return TaskResult.Blocked; // 没有合适的角色
            }

            var workDone = efficiency * deltaTime * 100f;
            _constructionProgress += workDone;

            SetProgress(_constructionProgress / _requiredWork);

            if (_constructionProgress >= _requiredWork)
            {
                return TaskResult.Success;
            }

            return TaskResult.InProgress;
        }

        private float CalculateConstructionEfficiency()
        {
            // 建造任务需要建造技能
            var constructionReq = Definition.SkillRequirements
                .Find(req => req.SkillType == SkillType.Construction);
            
            if (constructionReq == null) return 1.0f;

            // 简化计算，假设分配的角色都满足技能要求
            return Math.Max(0.5f, constructionReq.MinLevel / 15f);
        }

        protected override void OnReset()
        {
            _constructionProgress = 0f;
        }
    }

    /// <summary>
    /// 挖掘任务
    /// </summary>
    public class MiningTask : BaseTask
    {
        private float _miningProgress = 0f;
        private readonly float _requiredWork;

        public MiningTask(TaskDefinition definition) : base(definition)
        {
            _requiredWork = definition.EstimatedDuration * 120f; // 挖掘工作量
        }

        protected override TaskResult OnUpdate(float deltaTime)
        {
            var efficiency = CalculateMiningEfficiency();
            
            if (efficiency <= 0)
            {
                return TaskResult.Blocked;
            }

            var workDone = efficiency * deltaTime * 80f; // 挖掘速度稍慢
            _miningProgress += workDone;

            SetProgress(_miningProgress / _requiredWork);

            if (_miningProgress >= _requiredWork)
            {
                return TaskResult.Success;
            }

            return TaskResult.InProgress;
        }

        private float CalculateMiningEfficiency()
        {
            var miningReq = Definition.SkillRequirements
                .Find(req => req.SkillType == SkillType.Mining);
            
            if (miningReq == null) return 1.0f;

            return Math.Max(0.3f, miningReq.MinLevel / 12f);
        }

        protected override void OnReset()
        {
            _miningProgress = 0f;
        }
    }

    /// <summary>
    /// 研究任务
    /// </summary>
    public class ResearchTask : BaseTask
    {
        private float _researchProgress = 0f;
        private readonly float _requiredWork;

        public ResearchTask(TaskDefinition definition) : base(definition)
        {
            _requiredWork = definition.EstimatedDuration * 200f; // 研究需要大量时间
        }

        protected override TaskResult OnUpdate(float deltaTime)
        {
            var efficiency = CalculateResearchEfficiency();
            
            if (efficiency <= 0)
            {
                return TaskResult.Blocked;
            }

            var workDone = efficiency * deltaTime * 50f; // 研究速度较慢但稳定
            _researchProgress += workDone;

            SetProgress(_researchProgress / _requiredWork);

            if (_researchProgress >= _requiredWork)
            {
                return TaskResult.Success;
            }

            return TaskResult.InProgress;
        }

        private float CalculateResearchEfficiency()
        {
            var researchReq = Definition.SkillRequirements
                .Find(req => req.SkillType == SkillType.Research);
            
            if (researchReq == null) return 0.1f; // 研究必须有研究技能

            return Math.Max(0.2f, researchReq.MinLevel / 10f);
        }

        protected override void OnReset()
        {
            _researchProgress = 0f;
        }
    }
}