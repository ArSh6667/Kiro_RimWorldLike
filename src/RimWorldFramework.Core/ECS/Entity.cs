using System;

namespace RimWorldFramework.Core.ECS
{
    /// <summary>
    /// 表示游戏世界中的实体ID
    /// </summary>
    public readonly struct EntityId : IEquatable<EntityId>
    {
        public readonly uint Value;

        public EntityId(uint value)
        {
            Value = value;
        }

        public bool Equals(EntityId other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"Entity({Value})";

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);

        public static readonly EntityId Invalid = new(0);
    }

    /// <summary>
    /// 基础实体类
    /// </summary>
    public abstract class Entity
    {
        public EntityId Id { get; internal set; }
        public bool IsActive { get; internal set; } = true;

        protected Entity()
        {
            Id = EntityId.Invalid;
        }

        protected Entity(EntityId id)
        {
            Id = id;
        }
    }
}