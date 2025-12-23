using System;
using System.Linq;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization.Migrators
{
    /// <summary>
    /// 版本1到版本2的迁移器示例
    /// </summary>
    public class Version1To2Migrator : IVersionMigrator
    {
        public int SourceVersion => 1;
        public int TargetVersion => 2;

        public bool CanMigrate(GameState gameState)
        {
            return gameState != null && gameState.Version == SourceVersion;
        }

        public async Task<GameState> MigrateAsync(GameState gameState)
        {
            if (!CanMigrate(gameState))
            {
                throw new InvalidOperationException($"Cannot migrate game state from version {gameState?.Version} to {TargetVersion}");
            }

            // 创建新的游戏状态
            var migratedState = new GameState
            {
                Version = TargetVersion,
                CreatedAt = gameState.CreatedAt,
                LastSavedAt = DateTime.UtcNow,
                GameTime = gameState.GameTime,
                Configuration = gameState.Configuration
            };

            // 迁移角色数据
            migratedState.Characters = await MigrateCharacterDataAsync(gameState.Characters);

            // 迁移任务状态
            migratedState.TaskState = await MigrateTaskStateAsync(gameState.TaskState);

            // 迁移地图数据
            migratedState.MapData = await MigrateMapDataAsync(gameState.MapData);

            // 迁移系统状态
            migratedState.SystemStates = MigrateSystemStates(gameState.SystemStates);

            // 迁移自定义数据
            migratedState.CustomData = MigrateCustomData(gameState.CustomData);

            // 更新校验和
            migratedState.UpdateChecksum();

            return migratedState;
        }

        private async Task<System.Collections.Generic.List<CharacterEntityData>> MigrateCharacterDataAsync(System.Collections.Generic.List<CharacterEntityData> characters)
        {
            if (characters == null) return null;

            var migratedCharacters = new System.Collections.Generic.List<CharacterEntityData>();

            foreach (var character in characters)
            {
                var migratedCharacter = new CharacterEntityData
                {
                    EntityId = character.EntityId,
                    Name = character.Name,
                    Position = character.Position,
                    Skills = MigrateSkillData(character.Skills),
                    Needs = MigrateNeedData(character.Needs),
                    Inventory = MigrateInventoryData(character.Inventory),
                    CustomComponents = character.CustomComponents ?? new System.Collections.Generic.Dictionary<string, object>()
                };

                migratedCharacters.Add(migratedCharacter);
            }

            return migratedCharacters;
        }

        private SkillData MigrateSkillData(SkillData skills)
        {
            if (skills == null) return new SkillData();

            // 在版本2中，我们可能添加了新的技能类型
            var migratedSkills = new SkillData
            {
                SkillLevels = new System.Collections.Generic.Dictionary<string, int>(skills.SkillLevels ?? new System.Collections.Generic.Dictionary<string, int>()),
                SkillExperience = new System.Collections.Generic.Dictionary<string, float>(skills.SkillExperience ?? new System.Collections.Generic.Dictionary<string, float>())
            };

            // 添加新的默认技能（如果不存在）
            var defaultSkills = new[] { "Construction", "Mining", "Cooking", "Research", "Combat" };
            foreach (var skill in defaultSkills)
            {
                if (!migratedSkills.SkillLevels.ContainsKey(skill))
                {
                    migratedSkills.SkillLevels[skill] = 0;
                    migratedSkills.SkillExperience[skill] = 0f;
                }
            }

            return migratedSkills;
        }
        private NeedData MigrateNeedData(NeedData needs)
        {
            if (needs == null) return new NeedData();

            // 在版本2中，我们可能重新平衡了需求值
            var migratedNeeds = new NeedData
            {
                Hunger = Math.Max(0f, Math.Min(1f, needs.Hunger)),
                Sleep = Math.Max(0f, Math.Min(1f, needs.Sleep)),
                Recreation = Math.Max(0f, Math.Min(1f, needs.Recreation)),
                Comfort = Math.Max(0f, Math.Min(1f, needs.Comfort)),
                CustomNeeds = new System.Collections.Generic.Dictionary<string, float>(needs.CustomNeeds ?? new System.Collections.Generic.Dictionary<string, float>())
            };

            return migratedNeeds;
        }

        private InventoryData MigrateInventoryData(InventoryData inventory)
        {
            if (inventory == null) return new InventoryData();

            var migratedInventory = new InventoryData
            {
                MaxCapacity = inventory.MaxCapacity,
                Items = inventory.Items?.Select(MigrateItemData).ToList() ?? new System.Collections.Generic.List<ItemData>()
            };

            return migratedInventory;
        }

        private ItemData MigrateItemData(ItemData item)
        {
            if (item == null) return null;

            // 在版本2中，我们可能重命名了一些物品类型
            var migratedItem = new ItemData
            {
                ItemType = MigrateItemType(item.ItemType),
                Quantity = item.Quantity,
                Quality = Math.Max(0f, Math.Min(1f, item.Quality)),
                Properties = new System.Collections.Generic.Dictionary<string, object>(item.Properties ?? new System.Collections.Generic.Dictionary<string, object>())
            };

            return migratedItem;
        }

        private string MigrateItemType(string itemType)
        {
            // 物品类型迁移映射
            var typeMapping = new System.Collections.Generic.Dictionary<string, string>
            {
                { "OldWood", "Wood" },
                { "OldStone", "Stone" },
                { "OldMetal", "Steel" }
            };

            return typeMapping.TryGetValue(itemType, out var newType) ? newType : itemType;
        }

        private async Task<TaskTreeState> MigrateTaskStateAsync(TaskTreeState taskState)
        {
            if (taskState == null) return new TaskTreeState();

            var migratedTaskState = new TaskTreeState
            {
                Tasks = taskState.Tasks?.ToList() ?? new System.Collections.Generic.List<TaskData>(),
                TaskStatuses = new System.Collections.Generic.Dictionary<string, TaskStatus>(taskState.TaskStatuses ?? new System.Collections.Generic.Dictionary<string, TaskStatus>()),
                Dependencies = taskState.Dependencies?.ToList() ?? new System.Collections.Generic.List<TaskDependency>(),
                TaskProgress = new System.Collections.Generic.Dictionary<string, object>(taskState.TaskProgress ?? new System.Collections.Generic.Dictionary<string, object>())
            };

            return migratedTaskState;
        }

        private async Task<GameMapData> MigrateMapDataAsync(GameMapData mapData)
        {
            if (mapData == null) return null;

            // 地图数据通常不需要迁移，除非格式发生了重大变化
            var migratedMapData = new GameMapData
            {
                Width = mapData.Width,
                Height = mapData.Height,
                Seed = mapData.Seed,
                Terrain = mapData.Terrain,
                HeightMap = mapData.HeightMap,
                Resources = mapData.Resources?.ToList() ?? new System.Collections.Generic.List<ResourcePointData>(),
                CustomMapData = new System.Collections.Generic.Dictionary<string, object>(mapData.CustomMapData ?? new System.Collections.Generic.Dictionary<string, object>())
            };

            return migratedMapData;
        }

        private System.Collections.Generic.Dictionary<string, object> MigrateSystemStates(System.Collections.Generic.Dictionary<string, object> systemStates)
        {
            return new System.Collections.Generic.Dictionary<string, object>(systemStates ?? new System.Collections.Generic.Dictionary<string, object>());
        }

        private System.Collections.Generic.Dictionary<string, object> MigrateCustomData(System.Collections.Generic.Dictionary<string, object> customData)
        {
            return new System.Collections.Generic.Dictionary<string, object>(customData ?? new System.Collections.Generic.Dictionary<string, object>());
        }
    }
}