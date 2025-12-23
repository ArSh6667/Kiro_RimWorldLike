using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组管理器实现
    /// </summary>
    public class ModManager : IModManager
    {
        private readonly IModLoader _modLoader;
        private readonly Dictionary<string, IMod> _mods;
        private readonly Dictionary<string, bool> _modEnabledStatus;
        private readonly List<string> _loadOrder;
        private readonly IModConflictDetector _conflictDetector;

        public event EventHandler<ModStatusChangedEventArgs> ModStatusChanged;
        public event EventHandler<ModConflictDetectedEventArgs> ModConflictDetected;

        public ModManager(IModLoader modLoader = null, IModConflictDetector conflictDetector = null)
        {
            _modLoader = modLoader ?? new ModLoader();
            _conflictDetector = conflictDetector ?? new DefaultModConflictDetector();
            _mods = new Dictionary<string, IMod>();
            _modEnabledStatus = new Dictionary<string, bool>();
            _loadOrder = new List<string>();
        }

        public async Task<IMod> LoadModAsync(string modPath)
        {
            try
            {
                var mod = await _modLoader.LoadModAsync(modPath);
                
                _mods[mod.Id] = mod;
                _modEnabledStatus[mod.Id] = false; // 默认禁用，需要手动启用
                
                if (!_loadOrder.Contains(mod.Id))
                {
                    _loadOrder.Add(mod.Id);
                }

                OnModStatusChanged(mod.Id, ModStatus.NotLoaded, mod.Status, "Mod loaded");

                // 检测冲突
                await CheckForConflictsAsync();

                return mod;
            }
            catch (Exception ex)
            {
                throw new ModLoadException($"Failed to load mod from {modPath}: {ex.Message}", ex);
            }
        }

        public async Task UnloadModAsync(string modId)
        {
            if (!_mods.TryGetValue(modId, out var mod))
                return;

            var oldStatus = mod.Status;

            // 如果模组正在运行，先停止它
            if (mod.Status == ModStatus.Running)
            {
                await DisableModAsync(modId);
            }

            // 卸载模组
            await _modLoader.UnloadModAsync(mod);

            // 从管理器中移除
            _mods.Remove(modId);
            _modEnabledStatus.Remove(modId);
            _loadOrder.Remove(modId);

            OnModStatusChanged(modId, oldStatus, ModStatus.NotLoaded, "Mod unloaded");
        }

        public async Task EnableModAsync(string modId)
        {
            if (!_mods.TryGetValue(modId, out var mod))
            {
                throw new ArgumentException($"Mod {modId} not found");
            }

            if (_modEnabledStatus.GetValueOrDefault(modId, false))
                return; // 已经启用

            var oldStatus = mod.Status;

            try
            {
                // 检查依赖关系
                var dependencyResult = await _modLoader.CheckDependenciesAsync(mod);
                if (!dependencyResult.IsSatisfied)
                {
                    throw new ModDependencyException($"Dependencies not satisfied for mod {modId}");
                }

                // 启动模组
                await mod.StartAsync();
                _modEnabledStatus[modId] = true;

                OnModStatusChanged(modId, oldStatus, mod.Status, "Mod enabled");

                // 检测冲突
                await CheckForConflictsAsync();
            }
            catch (Exception ex)
            {
                OnModStatusChanged(modId, oldStatus, ModStatus.Error, $"Failed to enable: {ex.Message}");
                throw;
            }
        }

        public async Task DisableModAsync(string modId)
        {
            if (!_mods.TryGetValue(modId, out var mod))
                return;

            if (!_modEnabledStatus.GetValueOrDefault(modId, false))
                return; // 已经禁用

            var oldStatus = mod.Status;

            try
            {
                await mod.StopAsync();
                _modEnabledStatus[modId] = false;

                OnModStatusChanged(modId, oldStatus, mod.Status, "Mod disabled");
            }
            catch (Exception ex)
            {
                OnModStatusChanged(modId, oldStatus, ModStatus.Error, $"Failed to disable: {ex.Message}");
                throw;
            }
        }
        public async Task ReloadModAsync(string modId)
        {
            if (!_mods.TryGetValue(modId, out var mod))
            {
                throw new ArgumentException($"Mod {modId} not found");
            }

            var wasEnabled = _modEnabledStatus.GetValueOrDefault(modId, false);
            var modPath = mod.Path;

            try
            {
                // 卸载模组
                await UnloadModAsync(modId);

                // 重新加载模组
                var reloadedMod = await LoadModAsync(modPath);

                // 如果之前是启用状态，重新启用
                if (wasEnabled)
                {
                    await EnableModAsync(modId);
                }

                OnModStatusChanged(modId, ModStatus.NotLoaded, reloadedMod.Status, "Mod reloaded");
            }
            catch (Exception ex)
            {
                OnModStatusChanged(modId, ModStatus.Error, ModStatus.Error, $"Failed to reload: {ex.Message}");
                throw new ModReloadException($"Failed to reload mod {modId}: {ex.Message}", ex);
            }
        }

        public IEnumerable<IMod> GetAllMods()
        {
            return _mods.Values.ToList();
        }

        public IEnumerable<IMod> GetEnabledMods()
        {
            return _mods.Values.Where(mod => _modEnabledStatus.GetValueOrDefault(mod.Id, false)).ToList();
        }

        public IMod GetMod(string modId)
        {
            return _mods.TryGetValue(modId, out var mod) ? mod : null;
        }

        public async Task<ModConflictDetectionResult> DetectConflictsAsync()
        {
            var result = await _conflictDetector.DetectConflictsAsync(GetEnabledMods());
            
            if (result.HasConflicts)
            {
                OnModConflictDetected(result.Conflicts);
            }

            return result;
        }

        public async Task<ConflictResolutionResult> ResolveConflictsAsync(IEnumerable<ModConflict> conflicts)
        {
            var result = new ConflictResolutionResult();

            foreach (var conflict in conflicts)
            {
                try
                {
                    var resolved = await ResolveConflictAsync(conflict);
                    if (resolved)
                    {
                        result.ResolvedConflicts.Add(conflict);
                    }
                    else
                    {
                        result.UnresolvedConflicts.Add(conflict);
                    }
                }
                catch (Exception ex)
                {
                    result.UnresolvedConflicts.Add(conflict);
                    result.Details += $"Failed to resolve conflict {conflict.Id}: {ex.Message}\n";
                }
            }

            result.IsResolved = result.UnresolvedConflicts.Count == 0;
            return result;
        }

        public IEnumerable<IMod> GetLoadOrder()
        {
            return _loadOrder.Select(modId => _mods.TryGetValue(modId, out var mod) ? mod : null)
                            .Where(mod => mod != null)
                            .ToList();
        }

        public async Task SetLoadOrderAsync(IEnumerable<string> modIds)
        {
            var newOrder = modIds.ToList();
            
            // 验证所有模组ID都存在
            foreach (var modId in newOrder)
            {
                if (!_mods.ContainsKey(modId))
                {
                    throw new ArgumentException($"Mod {modId} not found");
                }
            }

            // 添加任何缺失的模组ID
            foreach (var modId in _mods.Keys)
            {
                if (!newOrder.Contains(modId))
                {
                    newOrder.Add(modId);
                }
            }

            _loadOrder.Clear();
            _loadOrder.AddRange(newOrder);

            // 检测加载顺序冲突
            await CheckForConflictsAsync();
        }
        public async Task<IEnumerable<ModInfo>> ScanModsDirectoryAsync(string modsDirectory)
        {
            if (!Directory.Exists(modsDirectory))
            {
                throw new DirectoryNotFoundException($"Mods directory not found: {modsDirectory}");
            }

            var modInfos = new List<ModInfo>();
            var modDirectories = Directory.GetDirectories(modsDirectory);

            foreach (var modDir in modDirectories)
            {
                try
                {
                    var manifestPath = Path.Combine(modDir, "mod.json");
                    if (!File.Exists(manifestPath))
                        continue;

                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    var manifest = JsonSerializer.Deserialize<ModManifest>(manifestJson);

                    var modInfo = new ModInfo
                    {
                        Path = modDir,
                        Manifest = manifest,
                        IsLoaded = _mods.ContainsKey(manifest.Id),
                        IsEnabled = _modEnabledStatus.GetValueOrDefault(manifest.Id, false)
                    };

                    // 验证模组
                    modInfo.ValidationResult = await _modLoader.ValidateModAsync(modDir);

                    modInfos.Add(modInfo);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续扫描其他模组
                    var errorModInfo = new ModInfo
                    {
                        Path = modDir,
                        IsLoaded = false,
                        IsEnabled = false,
                        ValidationResult = new ModValidationResult
                        {
                            IsValid = false,
                            Errors = new List<ValidationError>
                            {
                                new ValidationError
                                {
                                    Type = ValidationErrorType.InvalidManifest,
                                    Message = "Failed to scan mod",
                                    Details = ex.Message,
                                    FilePath = modDir
                                }
                            }
                        }
                    };
                    modInfos.Add(errorModInfo);
                }
            }

            return modInfos;
        }

        private async Task<bool> ResolveConflictAsync(ModConflict conflict)
        {
            foreach (var resolution in conflict.SuggestedResolutions.Where(r => r.IsAutomatic))
            {
                try
                {
                    switch (resolution.Type)
                    {
                        case ResolutionType.DisableMod:
                            if (resolution.Parameters.TryGetValue("ModId", out var modIdObj) && modIdObj is string modId)
                            {
                                await DisableModAsync(modId);
                                return true;
                            }
                            break;

                        case ResolutionType.ChangeLoadOrder:
                            if (resolution.Parameters.TryGetValue("NewOrder", out var orderObj) && orderObj is IEnumerable<string> newOrder)
                            {
                                await SetLoadOrderAsync(newOrder);
                                return true;
                            }
                            break;

                        // 其他解决方案类型可以在这里添加
                    }
                }
                catch (Exception)
                {
                    // 如果自动解决失败，尝试下一个解决方案
                    continue;
                }
            }

            return false;
        }

        private async Task CheckForConflictsAsync()
        {
            var conflicts = await DetectConflictsAsync();
            // 冲突检测结果已经通过事件通知
        }

        private void OnModStatusChanged(string modId, ModStatus oldStatus, ModStatus newStatus, string reason)
        {
            ModStatusChanged?.Invoke(this, new ModStatusChangedEventArgs
            {
                ModId = modId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                Reason = reason
            });
        }

        private void OnModConflictDetected(IEnumerable<ModConflict> conflicts)
        {
            ModConflictDetected?.Invoke(this, new ModConflictDetectedEventArgs
            {
                Conflicts = conflicts,
                DetectedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 模组依赖异常
    /// </summary>
    public class ModDependencyException : Exception
    {
        public ModDependencyException(string message) : base(message) { }
        public ModDependencyException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 模组重载异常
    /// </summary>
    public class ModReloadException : Exception
    {
        public ModReloadException(string message) : base(message) { }
        public ModReloadException(string message, Exception innerException) : base(message, innerException) { }
    }
}