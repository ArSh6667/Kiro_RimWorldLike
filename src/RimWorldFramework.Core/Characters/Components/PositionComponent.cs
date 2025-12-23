using RimWorldFramework.Core.ECS;
using RimWorldFramework.Core.Common;

namespace RimWorldFramework.Core.Characters.Components
{
    /// <summary>
    /// 位置组件
    /// </summary>
    [ComponentDescription("角色在游戏世界中的位置信息")]
    public class PositionComponent : Component
    {
        /// <summary>
        /// 当前位置
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero;

        /// <summary>
        /// 旋转角度（弧度）
        /// </summary>
        public float Rotation { get; set; } = 0f;

        /// <summary>
        /// 移动速度
        /// </summary>
        public float MovementSpeed { get; set; } = 1.0f;

        /// <summary>
        /// 是否正在移动
        /// </summary>
        public bool IsMoving { get; set; } = false;

        /// <summary>
        /// 目标位置（如果正在移动）
        /// </summary>
        public Vector3? TargetPosition { get; set; }

        /// <summary>
        /// 移动开始时间
        /// </summary>
        public float MovementStartTime { get; set; }

        /// <summary>
        /// 预计到达时间
        /// </summary>
        public float EstimatedArrivalTime { get; set; }

        public PositionComponent()
        {
        }

        public PositionComponent(Vector3 position)
        {
            Position = position;
        }

        public PositionComponent(float x, float y, float z = 0f)
        {
            Position = new Vector3(x, y, z);
        }

        /// <summary>
        /// 计算到目标位置的距离
        /// </summary>
        public float DistanceTo(Vector3 target)
        {
            return Vector3.Distance(Position, target);
        }

        /// <summary>
        /// 开始移动到目标位置
        /// </summary>
        public void StartMovementTo(Vector3 target, float currentTime)
        {
            TargetPosition = target;
            IsMoving = true;
            MovementStartTime = currentTime;
            
            var distance = DistanceTo(target);
            EstimatedArrivalTime = currentTime + (distance / MovementSpeed);
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMovement()
        {
            IsMoving = false;
            TargetPosition = null;
        }

        /// <summary>
        /// 更新移动状态
        /// </summary>
        public void UpdateMovement(float currentTime, float deltaTime)
        {
            if (!IsMoving || !TargetPosition.HasValue)
                return;

            var target = TargetPosition.Value;
            var direction = (target - Position).Normalized;
            var moveDistance = MovementSpeed * deltaTime;

            // 检查是否已到达目标
            if (DistanceTo(target) <= moveDistance)
            {
                Position = target;
                StopMovement();
            }
            else
            {
                Position += direction * moveDistance;
            }
        }
    }
}