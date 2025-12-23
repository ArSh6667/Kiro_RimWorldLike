using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Serialization
{
    /// <summary>
    /// 版本兼容性管理器实现
    /// </summary>
    public class VersionCompatibilityManager : IVersionCompatibilityManager
    {
        private readonly Dictionary<int, List<IVersionMigrator>> _migrators;
        private readonly int _currentVersion;

        public int CurrentVersion => _currentVersion;

        public VersionCompatibilityManager(int currentVersion = 1)
        {
            _currentVersion = currentVersion;
            _migrators = new Dictionary<int, List<IVersionMigrator>>();
        }

        public void RegisterMigrator(IVersionMigrator migrator)
        {
            if (migrator == null)
                throw new ArgumentNullException(nameof(migrator));

            if (!_migrators.ContainsKey(migrator.SourceVersion))
            {
                _migrators[migrator.SourceVersion] = new List<IVersionMigrator>();
            }

            _migrators[migrator.SourceVersion].Add(migrator);
        }

        public async Task<GameState> MigrateToCurrentVersionAsync(GameState gameState)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            if (gameState.Version == _currentVersion)
                return gameState;

            var migrationPath = GetMigrationPath(gameState.Version, _currentVersion);
            if (migrationPath.Count == 0)
            {
                throw new InvalidOperationException($"No migration path found from version {gameState.Version} to {_currentVersion}");
            }

            var currentState = gameState;
            foreach (var migrator in migrationPath)
            {
                if (!migrator.CanMigrate(currentState))
                {
                    throw new InvalidOperationException($"Cannot migrate from version {currentState.Version} using migrator {migrator.GetType().Name}");
                }

                currentState = await migrator.MigrateAsync(currentState);
            }

            return currentState;
        }

        public VersionCompatibilityInfo CheckCompatibility(int version)
        {
            var info = new VersionCompatibilityInfo();

            if (version == _currentVersion)
            {
                info.IsCompatible = true;
                info.RequiresMigration = false;
                info.Level = CompatibilityLevel.FullyCompatible;
                info.Description = "Version is current and fully compatible";
                return info;
            }

            if (version > _currentVersion)
            {
                info.IsCompatible = false;
                info.RequiresMigration = false;
                info.Level = CompatibilityLevel.Incompatible;
                info.Description = $"Version {version} is newer than current version {_currentVersion}";
                info.Warnings.Add("Loading newer save files may cause data loss or corruption");
                return info;
            }

            // 检查是否有迁移路径
            var migrationPath = GetMigrationPath(version, _currentVersion);
            if (migrationPath.Count > 0)
            {
                info.IsCompatible = true;
                info.RequiresMigration = true;
                info.Level = CompatibilityLevel.RequiresMigration;
                info.Description = $"Version {version} can be migrated to current version {_currentVersion}";
                
                if (migrationPath.Count > 1)
                {
                    info.Warnings.Add($"Migration requires {migrationPath.Count} steps");
                }
            }
            else
            {
                // 检查是否在兼容范围内（例如，只相差1个版本可能直接兼容）
                if (_currentVersion - version <= 1)
                {
                    info.IsCompatible = true;
                    info.RequiresMigration = false;
                    info.Level = CompatibilityLevel.BackwardCompatible;
                    info.Description = $"Version {version} is backward compatible";
                    info.Warnings.Add("Some features may not be available");
                }
                else
                {
                    info.IsCompatible = false;
                    info.RequiresMigration = false;
                    info.Level = CompatibilityLevel.Incompatible;
                    info.Description = $"No migration path available from version {version} to {_currentVersion}";
                }
            }

            return info;
        }
        public List<IVersionMigrator> GetMigrationPath(int sourceVersion, int targetVersion)
        {
            if (sourceVersion == targetVersion)
                return new List<IVersionMigrator>();

            // 使用广度优先搜索找到最短迁移路径
            var visited = new HashSet<int>();
            var queue = new Queue<(int version, List<IVersionMigrator> path)>();
            queue.Enqueue((sourceVersion, new List<IVersionMigrator>()));

            while (queue.Count > 0)
            {
                var (currentVersion, currentPath) = queue.Dequeue();

                if (visited.Contains(currentVersion))
                    continue;

                visited.Add(currentVersion);

                if (currentVersion == targetVersion)
                    return currentPath;

                // 查找从当前版本可以迁移到的版本
                if (_migrators.TryGetValue(currentVersion, out var migrators))
                {
                    foreach (var migrator in migrators)
                    {
                        if (!visited.Contains(migrator.TargetVersion))
                        {
                            var newPath = new List<IVersionMigrator>(currentPath) { migrator };
                            queue.Enqueue((migrator.TargetVersion, newPath));
                        }
                    }
                }
            }

            return new List<IVersionMigrator>(); // 没有找到路径
        }
    }
}