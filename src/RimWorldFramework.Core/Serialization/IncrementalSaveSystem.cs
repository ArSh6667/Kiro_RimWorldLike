using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 增量保存系统，只保存变化的数据
    /// </summary>
    public class IncrementalSaveSystem
    {
        private readonly IGameStateSerializer _serializer;
        private GameState _baselineState;

        public IncrementalSaveSystem(IGameStateSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// 设置基准状态
        /// </summary>
        /// <param name="baselineState">基准状态</param>
        public void SetBaseline(GameState baselineState)
        {
            _baselineState = baselineState ?? throw new ArgumentNullException(nameof(baselineState));
        }

        /// <summary>
        /// 创建增量保存
        /// </summary>
        /// <param name="currentState">当前状态</param>
        /// <param name="stream">输出流</param>
        /// <returns>增量保存任务</returns>
        public async Task CreateIncrementalSaveAsync(GameState currentState, Stream stream)
        {
            if (_baselineState == null)
            {
                throw new InvalidOperationException("Baseline state must be set before creating incremental save");
            }

            var delta = CreateDelta(_baselineState, currentState);
            await _serializer.SerializeAsync(delta, stream, new SerializationOptions
            {
                EnableCompression = true,
                EnableIncrementalSave = true
            });
        }

        /// <summary>
        /// 应用增量保存到基准状态
        /// </summary>
        /// <param name="deltaStream">增量数据流</param>
        /// <returns>合并后的完整状态</returns>
        public async Task<GameState> ApplyIncrementalSaveAsync(Stream deltaStream)
        {
            if (_baselineState == null)
            {
                throw new InvalidOperationException("Baseline state must be set before applying incremental save");
            }

            var delta = await _serializer.DeserializeAsync(deltaStream);
            return MergeStates(_baselineState, delta);
        }

        /// <summary>
        /// 创建状态差异
        /// </summary>
        /// <param name="baseline">基准状态</param>
        /// <param name="current">当前状态</param>
        /// <returns>差异状态</returns>
        private GameState CreateDelta(GameState baseline, GameState current)
        {
            var delta = new GameState
            {
                Version = current.Version,
                CreatedAt = baseline.CreatedAt,
                LastSavedAt = current.LastSavedAt,
                GameTime = current.GameTime,
                Configuration = current.Configuration
            };

            // 比较角色数据
            delta.Characters = GetCharacterDeltas(baseline.Characters, current.Characters);

            // 比较任务状态
            delta.TaskState = GetTaskStateDeltas(baseline.TaskState, current.TaskState);

            // 比较地图数据（通常地图不会频繁变化，所以可能不需要增量）
            if (!AreMapDataEqual(baseline.MapData, current.MapData))
            {
                delta.MapData = current.MapData;
            }

            // 比较系统状态
            delta.SystemStates = GetSystemStateDeltas(baseline.SystemStates, current.SystemStates);

            // 比较自定义数据
            delta.CustomData = GetCustomDataDeltas(baseline.CustomData, current.CustomData);

            delta.UpdateChecksum();
            return delta;
        }
        /// <summary>
        /// 合并基准状态和增量状态
        /// </summary>
        /// <param name="baseline">基准状态</param>
        /// <param name="delta">增量状态</param>
        /// <returns>合并后的状态</returns>
        private GameState MergeStates(GameState baseline, GameState delta)
        {
            var merged = new GameState
            {
                Version = delta.Version,
                CreatedAt = baseline.CreatedAt,
                LastSavedAt = delta.LastSavedAt,
                GameTime = delta.GameTime,
                Configuration = delta.Configuration ?? baseline.Configuration
            };

            // 合并角色数据
            merged.Characters = MergeCharacterData(baseline.Characters, delta.Characters);

            // 合并任务状态
            merged.TaskState = MergeTaskState(baseline.TaskState, delta.TaskState);

            // 合并地图数据
            merged.MapData = delta.MapData ?? baseline.MapData;

            // 合并系统状态
            merged.SystemStates = MergeSystemStates(baseline.SystemStates, delta.SystemStates);

            // 合并自定义数据
            merged.CustomData = MergeCustomData(baseline.CustomData, delta.CustomData);

            merged.UpdateChecksum();
            return merged;
        }

        private List<CharacterEntityData> GetCharacterDeltas(List<CharacterEntityData> baseline, List<CharacterEntityData> current)
        {
            var deltas = new List<CharacterEntityData>();
            var baselineDict = baseline?.ToDictionary(c => c.EntityId) ?? new Dictionary<uint, CharacterEntityData>();

            if (current != null)
            {
                foreach (var currentChar in current)
                {
                    if (!baselineDict.TryGetValue(currentChar.EntityId, out var baselineChar) ||
                        !AreCharactersEqual(baselineChar, currentChar))
                    {
                        deltas.Add(currentChar);
                    }
                }
            }

            return deltas;
        }

        private TaskTreeState GetTaskStateDeltas(TaskTreeState baseline, TaskTreeState current)
        {
            if (baseline == null) return current;
            if (current == null) return null;

            var delta = new TaskTreeState();

            // 比较任务列表
            var baselineTasks = baseline.Tasks?.ToDictionary(t => t.Id) ?? new Dictionary<string, TaskData>();
            if (current.Tasks != null)
            {
                foreach (var task in current.Tasks)
                {
                    if (!baselineTasks.TryGetValue(task.Id, out var baselineTask) ||
                        !AreTasksEqual(baselineTask, task))
                    {
                        delta.Tasks.Add(task);
                    }
                }
            }

            // 比较任务状态
            delta.TaskStatuses = GetDictionaryDeltas(baseline.TaskStatuses, current.TaskStatuses);

            // 比较依赖关系
            if (!AreDependenciesEqual(baseline.Dependencies, current.Dependencies))
            {
                delta.Dependencies = current.Dependencies;
            }

            // 比较任务进度
            delta.TaskProgress = GetDictionaryDeltas(baseline.TaskProgress, current.TaskProgress);

            return delta;
        }
        private Dictionary<string, object> GetSystemStateDeltas(Dictionary<string, object> baseline, Dictionary<string, object> current)
        {
            return GetDictionaryDeltas(baseline, current);
        }

        private Dictionary<string, object> GetCustomDataDeltas(Dictionary<string, object> baseline, Dictionary<string, object> current)
        {
            return GetDictionaryDeltas(baseline, current);
        }

        private Dictionary<TKey, TValue> GetDictionaryDeltas<TKey, TValue>(Dictionary<TKey, TValue> baseline, Dictionary<TKey, TValue> current)
        {
            var deltas = new Dictionary<TKey, TValue>();
            baseline ??= new Dictionary<TKey, TValue>();
            current ??= new Dictionary<TKey, TValue>();

            foreach (var kvp in current)
            {
                if (!baseline.TryGetValue(kvp.Key, out var baselineValue) ||
                    !Equals(baselineValue, kvp.Value))
                {
                    deltas[kvp.Key] = kvp.Value;
                }
            }

            return deltas;
        }

        private List<CharacterEntityData> MergeCharacterData(List<CharacterEntityData> baseline, List<CharacterEntityData> delta)
        {
            var merged = new List<CharacterEntityData>(baseline ?? new List<CharacterEntityData>());
            var mergedDict = merged.ToDictionary(c => c.EntityId);

            if (delta != null)
            {
                foreach (var deltaChar in delta)
                {
                    mergedDict[deltaChar.EntityId] = deltaChar;
                }
            }

            return mergedDict.Values.ToList();
        }

        private TaskTreeState MergeTaskState(TaskTreeState baseline, TaskTreeState delta)
        {
            if (baseline == null) return delta;
            if (delta == null) return baseline;

            var merged = new TaskTreeState
            {
                Tasks = MergeTaskList(baseline.Tasks, delta.Tasks),
                TaskStatuses = MergeDictionaries(baseline.TaskStatuses, delta.TaskStatuses),
                Dependencies = delta.Dependencies ?? baseline.Dependencies,
                TaskProgress = MergeDictionaries(baseline.TaskProgress, delta.TaskProgress)
            };

            return merged;
        }

        private List<TaskData> MergeTaskList(List<TaskData> baseline, List<TaskData> delta)
        {
            var merged = new List<TaskData>(baseline ?? new List<TaskData>());
            var mergedDict = merged.ToDictionary(t => t.Id);

            if (delta != null)
            {
                foreach (var deltaTask in delta)
                {
                    mergedDict[deltaTask.Id] = deltaTask;
                }
            }

            return mergedDict.Values.ToList();
        }

        private Dictionary<string, object> MergeSystemStates(Dictionary<string, object> baseline, Dictionary<string, object> delta)
        {
            return MergeDictionaries(baseline, delta);
        }

        private Dictionary<string, object> MergeCustomData(Dictionary<string, object> baseline, Dictionary<string, object> delta)
        {
            return MergeDictionaries(baseline, delta);
        }

        private Dictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(Dictionary<TKey, TValue> baseline, Dictionary<TKey, TValue> delta)
        {
            var merged = new Dictionary<TKey, TValue>(baseline ?? new Dictionary<TKey, TValue>());
            
            if (delta != null)
            {
                foreach (var kvp in delta)
                {
                    merged[kvp.Key] = kvp.Value;
                }
            }

            return merged;
        }
        // 辅助方法：比较对象是否相等
        private bool AreCharactersEqual(CharacterEntityData a, CharacterEntityData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.EntityId == b.EntityId &&
                   a.Name == b.Name &&
                   a.Position.Equals(b.Position) &&
                   AreSkillDataEqual(a.Skills, b.Skills) &&
                   AreNeedDataEqual(a.Needs, b.Needs) &&
                   AreInventoryDataEqual(a.Inventory, b.Inventory);
        }

        private bool AreTasksEqual(TaskData a, TaskData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Id == b.Id &&
                   a.Name == b.Name &&
                   a.Type == b.Type &&
                   a.Priority == b.Priority &&
                   a.Location.Equals(b.Location);
        }

        private bool AreMapDataEqual(GameMapData a, GameMapData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Width == b.Width &&
                   a.Height == b.Height &&
                   a.Seed == b.Seed;
            // 注意：这里简化了比较，实际应用中可能需要更详细的比较
        }

        private bool AreSkillDataEqual(SkillData a, SkillData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return AreDictionariesEqual(a.SkillLevels, b.SkillLevels) &&
                   AreDictionariesEqual(a.SkillExperience, b.SkillExperience);
        }

        private bool AreNeedDataEqual(NeedData a, NeedData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return Math.Abs(a.Hunger - b.Hunger) < 0.001f &&
                   Math.Abs(a.Sleep - b.Sleep) < 0.001f &&
                   Math.Abs(a.Recreation - b.Recreation) < 0.001f &&
                   Math.Abs(a.Comfort - b.Comfort) < 0.001f &&
                   AreDictionariesEqual(a.CustomNeeds, b.CustomNeeds);
        }

        private bool AreInventoryDataEqual(InventoryData a, InventoryData b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.MaxCapacity == b.MaxCapacity &&
                   a.Items?.Count == b.Items?.Count;
            // 简化比较，实际应用中需要比较具体物品
        }

        private bool AreDependenciesEqual(List<TaskDependency> a, List<TaskDependency> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Count == b.Count;
            // 简化比较，实际应用中需要详细比较依赖关系
        }

        private bool AreDictionariesEqual<TKey, TValue>(Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;

            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var bValue) || !Equals(kvp.Value, bValue))
                {
                    return false;
                }
            }

            return true;
        }
    }
}