namespace RimWorldFramework.Core.ECS
{
    /// <summary>
    /// 组件标记接口
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// 组件所属的实体ID
        /// </summary>
        EntityId EntityId { get; set; }
    }

    /// <summary>
    /// 基础组件抽象类
    /// </summary>
    public abstract class Component : IComponent
    {
        public EntityId EntityId { get; set; }

        protected Component()
        {
            EntityId = EntityId.Invalid;
        }

        protected Component(EntityId entityId)
        {
            EntityId = entityId;
        }
    }
}