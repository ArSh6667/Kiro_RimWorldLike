using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 默认模组冲突检测器实现
    /// </summary>
    public class DefaultModConflictDetector : IModConflictDetector
    {
        public async Task<ModConflictDetectionResult> DetectConflictsAsync(IEnumerable<IMod> mods)
        {
            var result = new ModConflictDetectionResult();
            var modList = mods.ToList();

            // 检测各种类型的冲突
            var dependencyConflicts = await DetectSpecificConflictsAsync(modList, ConflictType.DependencyConflict);
            var versionConflicts = await DetectSpecificConflictsAsync(modList, ConflictType.VersionConflict);
            var resourceConflicts = await DetectSpecificConflictsAsync(modList, ConflictType.ResourceConflict);
            var apiConflicts = await DetectSpecificConflictsAsync(modList, ConflictType.ApiConflict);
            var loadOrderConflicts = await DetectSpecificConflictsAsync(modList, ConflictType.LoadOrderConflict);

            // 合并所有冲突
            result.Conflicts.AddRange(dependencyConflicts);
            result.Conflicts.AddRange(versionConflicts);
            result.Conflicts.AddRange(resourceConflicts);
            result.Conflicts.AddRange(apiConflicts);
            result.Conflicts.AddRange(loadOrderConflicts);

            result.HasConflicts = result.Conflicts.Any();
            result.Details = $"Detected {result.Conflicts.Count} conflicts across {modList.Count} mods";

            return result;
        }

        public async Task<IEnumerable<ModConflict>> DetectSpecificConflictsAsync(IEnumerable<IMod> mods, ConflictType conflictType)
        {
            var conflicts = new List<ModConflict>();
            var modList = mods.ToList();

            switch (conflictType)
            {
                case ConflictType.DependencyConflict:
                    conflicts.AddRange(await DetectDependencyConflictsAsync(modList));
                    break;

                case ConflictType.VersionConflict:
                    conflicts.AddRange(await DetectVersionConflictsAsync(modList));
                    break;

                case ConflictType.ResourceConflict:
                    conflicts.AddRange(await DetectResourceConflictsAsync(modList));
                    break;

                case ConflictType.ApiConflict:
                    conflicts.AddRange(await DetectApiConflictsAsync(modList));
                    break;

                case ConflictType.LoadOrderConflict:
                    conflicts.AddRange(await DetectLoadOrderConflictsAsync(modList));
                    break;
            }

            return conflicts;
        }

        public async Task<IEnumerable<ConflictResolution>> GenerateResolutionSuggestionsAsync(ModConflict conflict)
        {
            var resolutions = new List<ConflictResolution>();

            switch (conflict.Type)
            {
                case ConflictType.DependencyConflict:
                    resolutions.AddRange(GenerateDependencyResolutions(conflict));
                    break;

                case ConflictType.VersionConflict:
                    resolutions.AddRange(GenerateVersionResolutions(conflict));
                    break;

                case ConflictType.ResourceConflict:
                    resolutions.AddRange(GenerateResourceResolutions(conflict));
                    break;

                case ConflictType.LoadOrderConflict:
                    resolutions.AddRange(GenerateLoadOrderResolutions(conflict));
                    break;
            }

            return resolutions;
        }

        private async Task<IEnumerable<ModConflict>> DetectDependencyConflictsAsync(List<IMod> mods)
        {
            var conflicts = new List<ModConflict>();
            var modDict = mods.ToDictionary(m => m.Id);

            foreach (var mod in mods)
            {
                foreach (var dependency in mod.Dependencies)
                {
                    if (!dependency.IsOptional && !modDict.ContainsKey(dependency.ModId))
                    {
                        conflicts.Add(new ModConflict
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = ConflictType.DependencyConflict,
                            Severity = ConflictSeverity.Error,
                            InvolvedMods = new List<string> { mod.Id },
                            Description = $"Missing required dependency: {dependency.ModId}",
                            Details = $"Mod {mod.Name} requires {dependency.ModId} but it is not loaded",
                            SuggestedResolutions = await GenerateResolutionSuggestionsAsync(new ModConflict
                            {
                                Type = ConflictType.DependencyConflict,
                                InvolvedMods = new List<string> { mod.Id }
                            })
                        });
                    }
                }
            }

            return conflicts;
        }

        private async Task<IEnumerable<ModConflict>> DetectVersionConflictsAsync(List<IMod> mods)
        {
            var conflicts = new List<ModConflict>();
            var modDict = mods.ToDictionary(m => m.Id);

            foreach (var mod in mods)
            {
                foreach (var dependency in mod.Dependencies)
                {
                    if (modDict.TryGetValue(dependency.ModId, out var dependentMod))
                    {
                        bool hasVersionConflict = false;
                        string conflictDescription = "";

                        if (dependency.MinVersion != null && dependentMod.Version < dependency.MinVersion)
                        {
                            hasVersionConflict = true;
                            conflictDescription = $"Version too old: required >= {dependency.MinVersion}, found {dependentMod.Version}";
                        }

                        if (dependency.MaxVersion != null && dependentMod.Version > dependency.MaxVersion)
                        {
                            hasVersionConflict = true;
                            conflictDescription = $"Version too new: required <= {dependency.MaxVersion}, found {dependentMod.Version}";
                        }

                        if (hasVersionConflict)
                        {
                            conflicts.Add(new ModConflict
                            {
                                Id = Guid.NewGuid().ToString(),
                                Type = ConflictType.VersionConflict,
                                Severity = ConflictSeverity.Error,
                                InvolvedMods = new List<string> { mod.Id, dependency.ModId },
                                Description = $"Version conflict between {mod.Name} and {dependentMod.Name}",
                                Details = conflictDescription
                            });
                        }
                    }
                }
            }

            return conflicts;
        }
        private async Task<IEnumerable<ModConflict>> DetectResourceConflictsAsync(List<IMod> mods)
        {
            var conflicts = new List<ModConflict>();
            var resourceMap = new Dictionary<string, List<string>>(); // 资源路径 -> 模组ID列表

            foreach (var mod in mods)
            {
                if (mod is Mod concreteMod)
                {
                    var resourceFiles = GetModResourceFiles(concreteMod.Path);
                    
                    foreach (var resourceFile in resourceFiles)
                    {
                        var relativePath = Path.GetRelativePath(concreteMod.Path, resourceFile);
                        
                        if (!resourceMap.ContainsKey(relativePath))
                        {
                            resourceMap[relativePath] = new List<string>();
                        }
                        
                        resourceMap[relativePath].Add(mod.Id);
                    }
                }
            }

            // 检查资源冲突
            foreach (var kvp in resourceMap.Where(r => r.Value.Count > 1))
            {
                conflicts.Add(new ModConflict
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = ConflictType.ResourceConflict,
                    Severity = ConflictSeverity.Warning,
                    InvolvedMods = kvp.Value,
                    Description = $"Resource file conflict: {kvp.Key}",
                    Details = $"Multiple mods provide the same resource: {string.Join(", ", kvp.Value)}",
                    ConflictingResources = new List<string> { kvp.Key }
                });
            }

            return conflicts;
        }

        private async Task<IEnumerable<ModConflict>> DetectApiConflictsAsync(List<IMod> mods)
        {
            var conflicts = new List<ModConflict>();
            
            // 简化的API冲突检测 - 检查是否有模组尝试修改相同的游戏系统
            var systemModifications = new Dictionary<string, List<string>>();

            foreach (var mod in mods)
            {
                // 这里可以分析模组的程序集来检测API使用情况
                // 为了简化，我们假设模组清单中包含了API使用信息
                var apiUsages = GetModApiUsages(mod);
                
                foreach (var apiUsage in apiUsages)
                {
                    if (!systemModifications.ContainsKey(apiUsage))
                    {
                        systemModifications[apiUsage] = new List<string>();
                    }
                    systemModifications[apiUsage].Add(mod.Id);
                }
            }

            // 检查API冲突
            foreach (var kvp in systemModifications.Where(s => s.Value.Count > 1))
            {
                conflicts.Add(new ModConflict
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = ConflictType.ApiConflict,
                    Severity = ConflictSeverity.Warning,
                    InvolvedMods = kvp.Value,
                    Description = $"API conflict in system: {kvp.Key}",
                    Details = $"Multiple mods modify the same system: {string.Join(", ", kvp.Value)}"
                });
            }

            return conflicts;
        }

        private async Task<IEnumerable<ModConflict>> DetectLoadOrderConflictsAsync(List<IMod> mods)
        {
            var conflicts = new List<ModConflict>();
            
            // 检查加载顺序依赖
            for (int i = 0; i < mods.Count; i++)
            {
                var mod = mods[i];
                
                foreach (var dependency in mod.Dependencies)
                {
                    var dependencyIndex = mods.FindIndex(m => m.Id == dependency.ModId);
                    
                    if (dependencyIndex > i)
                    {
                        conflicts.Add(new ModConflict
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = ConflictType.LoadOrderConflict,
                            Severity = ConflictSeverity.Error,
                            InvolvedMods = new List<string> { mod.Id, dependency.ModId },
                            Description = $"Load order conflict: {mod.Name} depends on {dependency.ModId}",
                            Details = $"{mod.Name} must be loaded after its dependency {dependency.ModId}"
                        });
                    }
                }
            }

            return conflicts;
        }

        private List<string> GetModResourceFiles(string modPath)
        {
            var resourceFiles = new List<string>();
            
            if (Directory.Exists(modPath))
            {
                // 获取常见的资源文件类型
                var resourceExtensions = new[] { ".png", ".jpg", ".jpeg", ".wav", ".mp3", ".ogg", ".xml", ".json" };
                
                foreach (var extension in resourceExtensions)
                {
                    resourceFiles.AddRange(Directory.GetFiles(modPath, $"*{extension}", SearchOption.AllDirectories));
                }
            }

            return resourceFiles;
        }

        private List<string> GetModApiUsages(IMod mod)
        {
            // 简化实现 - 在实际应用中，这里应该分析模组的程序集
            // 来检测它使用了哪些游戏API
            var apiUsages = new List<string>();
            
            // 基于模组名称或描述的简单启发式检测
            if (mod.Name.Contains("UI", StringComparison.OrdinalIgnoreCase) || 
                mod.Description.Contains("interface", StringComparison.OrdinalIgnoreCase))
            {
                apiUsages.Add("UI System");
            }
            
            if (mod.Name.Contains("AI", StringComparison.OrdinalIgnoreCase) || 
                mod.Description.Contains("behavior", StringComparison.OrdinalIgnoreCase))
            {
                apiUsages.Add("AI System");
            }

            return apiUsages;
        }
        private List<ConflictResolution> GenerateDependencyResolutions(ModConflict conflict)
        {
            var resolutions = new List<ConflictResolution>();

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.DisableMod,
                Description = "Disable the mod with missing dependencies",
                IsAutomatic = true,
                Parameters = new Dictionary<string, object>
                {
                    { "ModId", conflict.InvolvedMods.First() }
                },
                SideEffects = new List<string> { "Mod functionality will be unavailable" }
            });

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.InstallDependency,
                Description = "Install the missing dependency",
                IsAutomatic = false,
                SideEffects = new List<string> { "Additional mod will be installed" }
            });

            return resolutions;
        }

        private List<ConflictResolution> GenerateVersionResolutions(ModConflict conflict)
        {
            var resolutions = new List<ConflictResolution>();

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.UpdateMod,
                Description = "Update the mod to a compatible version",
                IsAutomatic = false,
                SideEffects = new List<string> { "Mod behavior may change" }
            });

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.DisableMod,
                Description = "Disable one of the conflicting mods",
                IsAutomatic = true,
                Parameters = new Dictionary<string, object>
                {
                    { "ModId", conflict.InvolvedMods.Last() }
                }
            });

            return resolutions;
        }

        private List<ConflictResolution> GenerateResourceResolutions(ModConflict conflict)
        {
            var resolutions = new List<ConflictResolution>();

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.ConfigurationOverride,
                Description = "Configure resource priority",
                IsAutomatic = false,
                SideEffects = new List<string> { "One mod's resources will take precedence" }
            });

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.DisableMod,
                Description = "Disable one of the conflicting mods",
                IsAutomatic = true,
                Parameters = new Dictionary<string, object>
                {
                    { "ModId", conflict.InvolvedMods.Last() }
                }
            });

            return resolutions;
        }

        private List<ConflictResolution> GenerateLoadOrderResolutions(ModConflict conflict)
        {
            var resolutions = new List<ConflictResolution>();

            resolutions.Add(new ConflictResolution
            {
                Id = Guid.NewGuid().ToString(),
                Type = ResolutionType.ChangeLoadOrder,
                Description = "Reorder mods to satisfy dependencies",
                IsAutomatic = true,
                Parameters = new Dictionary<string, object>
                {
                    { "NewOrder", ReorderModsForDependencies(conflict.InvolvedMods) }
                }
            });

            return resolutions;
        }

        private List<string> ReorderModsForDependencies(List<string> involvedMods)
        {
            // 简化的重排序逻辑 - 将依赖项放在前面
            return involvedMods.OrderBy(modId => modId).ToList();
        }
    }
}