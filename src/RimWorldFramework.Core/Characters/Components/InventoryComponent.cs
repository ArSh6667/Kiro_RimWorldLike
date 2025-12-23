using System;
using System.Collections.Generic;
using System.Linq;
using RimWorldFramework.Core.ECS;

namespace RimWorldFramework.Core.Characters.Components
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Food,        // 食物
        Tool,        // 工具
        Weapon,      // 武器
        Clothing,    // 衣物
        Material,    // 材料
        Medicine,    // 药品
        Furniture,   // 家具
        Electronics, // 电子设备
        Art,         // 艺术品
        Other        // 其他
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    public class Item
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public int Quantity { get; set; } = 1;
        public float Weight { get; set; } = 1.0f;
        public float Value { get; set; } = 1.0f;
        public float Quality { get; set; } = 1.0f; // 0.0 到 2.0，1.0 为普通品质
        public bool IsStackable { get; set; } = true;
        public int MaxStackSize { get; set; } = 100;
        public Dictionary<string, object> Properties { get; set; } = new();

        public Item()
        {
        }

        public Item(string id, string name, ItemType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// 获取总重量
        /// </summary>
        public float GetTotalWeight()
        {
            return Weight * Quantity;
        }

        /// <summary>
        /// 获取总价值
        /// </summary>
        public float GetTotalValue()
        {
            return Value * Quantity * Quality;
        }

        /// <summary>
        /// 尝试添加数量
        /// </summary>
        public int TryAddQuantity(int amount)
        {
            if (!IsStackable) return 0;

            var canAdd = Math.Min(amount, MaxStackSize - Quantity);
            Quantity += canAdd;
            return canAdd;
        }

        /// <summary>
        /// 尝试移除数量
        /// </summary>
        public int TryRemoveQuantity(int amount)
        {
            var canRemove = Math.Min(amount, Quantity);
            Quantity -= canRemove;
            return canRemove;
        }

        /// <summary>
        /// 克隆物品
        /// </summary>
        public Item Clone()
        {
            return new Item
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Quantity = Quantity,
                Weight = Weight,
                Value = Value,
                Quality = Quality,
                IsStackable = IsStackable,
                MaxStackSize = MaxStackSize,
                Properties = new Dictionary<string, object>(Properties)
            };
        }

        /// <summary>
        /// 获取品质描述
        /// </summary>
        public string GetQualityDescription()
        {
            return Quality switch
            {
                >= 1.8f => "传奇",
                >= 1.6f => "大师级",
                >= 1.4f => "优秀",
                >= 1.2f => "良好",
                >= 0.8f => "普通",
                >= 0.6f => "粗糙",
                _ => "劣质"
            };
        }

        public override string ToString()
        {
            var qualityDesc = Quality != 1.0f ? $" ({GetQualityDescription()})" : "";
            var quantityDesc = Quantity > 1 ? $" x{Quantity}" : "";
            return $"{Name}{qualityDesc}{quantityDesc}";
        }
    }

    /// <summary>
    /// 库存组件
    /// </summary>
    [ComponentDescription("角色的库存系统，管理携带的物品")]
    public class InventoryComponent : Component
    {
        private readonly List<Item> _items = new();
        
        public float MaxWeight { get; set; } = 50.0f;
        public int MaxSlots { get; set; } = 20;

        /// <summary>
        /// 获取当前重量
        /// </summary>
        public float CurrentWeight => _items.Sum(item => item.GetTotalWeight());

        /// <summary>
        /// 获取已使用槽位数
        /// </summary>
        public int UsedSlots => _items.Count;

        /// <summary>
        /// 检查是否有空间
        /// </summary>
        public bool HasSpace => UsedSlots < MaxSlots;

        /// <summary>
        /// 检查重量是否超限
        /// </summary>
        public bool IsOverweight => CurrentWeight > MaxWeight;

        /// <summary>
        /// 获取所有物品
        /// </summary>
        public IReadOnlyList<Item> GetAllItems()
        {
            return _items.AsReadOnly();
        }

        /// <summary>
        /// 添加物品
        /// </summary>
        public bool TryAddItem(Item item)
        {
            if (item == null || item.Quantity <= 0)
                return false;

            // 检查重量限制
            if (CurrentWeight + item.GetTotalWeight() > MaxWeight)
                return false;

            // 尝试堆叠到现有物品
            if (item.IsStackable)
            {
                var existingItem = _items.FirstOrDefault(i => 
                    i.Id == item.Id && 
                    i.Quality == item.Quality &&
                    i.Quantity < i.MaxStackSize);

                if (existingItem != null)
                {
                    var addedQuantity = existingItem.TryAddQuantity(item.Quantity);
                    item.Quantity -= addedQuantity;
                    
                    if (item.Quantity <= 0)
                        return true; // 完全堆叠成功
                }
            }

            // 检查槽位限制
            if (!HasSpace)
                return false;

            // 添加新物品
            _items.Add(item.Clone());
            return true;
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool TryRemoveItem(string itemId, int quantity = 1)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null || item.Quantity < quantity)
                return false;

            var removedQuantity = item.TryRemoveQuantity(quantity);
            
            if (item.Quantity <= 0)
            {
                _items.Remove(item);
            }

            return removedQuantity == quantity;
        }

        /// <summary>
        /// 获取特定物品数量
        /// </summary>
        public int GetItemQuantity(string itemId)
        {
            return _items.Where(i => i.Id == itemId).Sum(i => i.Quantity);
        }

        /// <summary>
        /// 检查是否有特定物品
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetItemQuantity(itemId) >= quantity;
        }

        /// <summary>
        /// 获取特定类型的物品
        /// </summary>
        public IEnumerable<Item> GetItemsByType(ItemType type)
        {
            return _items.Where(i => i.Type == type);
        }

        /// <summary>
        /// 获取最有价值的物品
        /// </summary>
        public Item? GetMostValuableItem()
        {
            return _items.OrderByDescending(i => i.GetTotalValue()).FirstOrDefault();
        }

        /// <summary>
        /// 获取总价值
        /// </summary>
        public float GetTotalValue()
        {
            return _items.Sum(i => i.GetTotalValue());
        }

        /// <summary>
        /// 清空库存
        /// </summary>
        public void Clear()
        {
            _items.Clear();
        }

        /// <summary>
        /// 整理库存（合并可堆叠物品）
        /// </summary>
        public void OrganizeInventory()
        {
            var itemGroups = _items
                .Where(i => i.IsStackable)
                .GroupBy(i => new { i.Id, i.Quality })
                .ToList();

            foreach (var group in itemGroups)
            {
                var items = group.ToList();
                if (items.Count <= 1) continue;

                var firstItem = items[0];
                var totalQuantity = items.Sum(i => i.Quantity);

                // 移除所有相同物品
                foreach (var item in items)
                {
                    _items.Remove(item);
                }

                // 重新添加合并后的物品
                while (totalQuantity > 0)
                {
                    var newItem = firstItem.Clone();
                    newItem.Quantity = Math.Min(totalQuantity, newItem.MaxStackSize);
                    _items.Add(newItem);
                    totalQuantity -= newItem.Quantity;
                }
            }
        }

        /// <summary>
        /// 丢弃最重的物品（用于减重）
        /// </summary>
        public Item? DropHeaviestItem()
        {
            var heaviestItem = _items.OrderByDescending(i => i.GetTotalWeight()).FirstOrDefault();
            if (heaviestItem != null)
            {
                _items.Remove(heaviestItem);
                return heaviestItem;
            }
            return null;
        }

        /// <summary>
        /// 获取库存摘要
        /// </summary>
        public string GetInventorySummary()
        {
            var weightPercent = (CurrentWeight / MaxWeight) * 100;
            var slotPercent = ((float)UsedSlots / MaxSlots) * 100;
            
            return $"重量: {CurrentWeight:F1}/{MaxWeight} ({weightPercent:F0}%) | " +
                   $"槽位: {UsedSlots}/{MaxSlots} ({slotPercent:F0}%)";
        }

        /// <summary>
        /// 随机生成物品（用于测试）
        /// </summary>
        public void GenerateRandomItems(Random random, int itemCount = 5)
        {
            var itemTypes = Enum.GetValues<ItemType>();
            
            for (int i = 0; i < itemCount; i++)
            {
                var type = itemTypes[random.Next(itemTypes.Length)];
                var item = new Item
                {
                    Id = $"item_{type}_{i}",
                    Name = $"{type} {i + 1}",
                    Type = type,
                    Quantity = random.Next(1, 6),
                    Weight = random.NextSingle() * 5f + 0.5f,
                    Value = random.NextSingle() * 100f + 10f,
                    Quality = random.NextSingle() * 1.5f + 0.5f
                };
                
                TryAddItem(item);
            }
        }
    }
}