using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组加载器实现
    /// </summary>
    public class ModLoader : IModLoader
    {
        private readonly Dictionary<string, IMod> _loadedMods;
        private readonly IModValidator _validator;
        private readonly IModSecurityManager _securityManager;

        public ModLoader(IModValidator validator = null, IModSecurityManager securityManager = null)
        {
            _loadedMods = new Dictionary<string, IMod>();
            _validator = validator ?? new DefaultModValidator();
            _securityManager = securityManager ?? new DefaultModSecurityManager();
        }

        public async Task<IMod> LoadModAsync(string modPath)
        {
            if (string.IsNullOrEmpty(modPath) || !Directory.Exists(modPath))
            {
                throw new ArgumentException($"Invalid mod path: {modPath}");
            }

            // 验证模组安全性
            var validationResult = await ValidateModAsync(modPath);
            if (!validationResult.IsValid)
            {
                throw new ModLoadException($"Mod validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            // 读取模组清单
            var manifestPath = Path.Combine(modPath, "mod.json");
            if (!File.Exists(manifestPath))
            {
                throw new ModLoadException($"Mod manifest not found: {manifestPath}");
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<ModManifest>(manifestJson);

            // 检查是否已经加载
            if (_loadedMods.ContainsKey(manifest.Id))
            {
                throw new ModLoadException($"Mod {manifest.Id} is already loaded");
            }

            // 创建模组实例
            var mod = new Mod(manifest, modPath);

            // 检查依赖关系
            var dependencyResult = await CheckDependenciesAsync(mod);
            if (!dependencyResult.IsSatisfied)
            {
                throw new ModLoadException($"Mod dependencies not satisfied: {string.Join(", ", dependencyResult.MissingDependencies.Select(d => d.ModId))}");
            }

            // 加载模组程序集
            await LoadModAssembliesAsync(mod);

            // 初始化模组
            await mod.InitializeAsync();

            // 添加到已加载列表
            _loadedMods[manifest.Id] = mod;

            return mod;
        }

        public async Task UnloadModAsync(IMod mod)
        {
            if (mod == null)
                throw new ArgumentNullException(nameof(mod));

            if (!_loadedMods.ContainsKey(mod.Id))
                return;

            // 停止模组
            if (mod.Status == ModStatus.Running)
            {
                await mod.StopAsync();
            }

            // 清理资源
            await mod.CleanupAsync();

            // 从已加载列表中移除
            _loadedMods.Remove(mod.Id);
        }

        public async Task<ModValidationResult> ValidateModAsync(string modPath)
        {
            return await _validator.ValidateAsync(modPath);
        }

        public IEnumerable<IMod> GetLoadedMods()
        {
            return _loadedMods.Values.ToList();
        }
        public async Task<DependencyCheckResult> CheckDependenciesAsync(IMod mod)
        {
            var result = new DependencyCheckResult { IsSatisfied = true };

            foreach (var dependency in mod.Dependencies)
            {
                // 检查依赖的模组是否已加载
                if (!_loadedMods.TryGetValue(dependency.ModId, out var dependentMod))
                {
                    if (!dependency.IsOptional)
                    {
                        result.MissingDependencies.Add(dependency);
                        result.IsSatisfied = false;
                    }
                    continue;
                }

                // 检查版本兼容性
                if (dependency.MinVersion != null && dependentMod.Version < dependency.MinVersion)
                {
                    result.VersionConflicts.Add(new VersionConflict
                    {
                        ModId = dependency.ModId,
                        RequiredDependency = dependency,
                        ActualVersion = dependentMod.Version,
                        Description = $"Required minimum version {dependency.MinVersion}, but found {dependentMod.Version}"
                    });
                    result.IsSatisfied = false;
                }

                if (dependency.MaxVersion != null && dependentMod.Version > dependency.MaxVersion)
                {
                    result.VersionConflicts.Add(new VersionConflict
                    {
                        ModId = dependency.ModId,
                        RequiredDependency = dependency,
                        ActualVersion = dependentMod.Version,
                        Description = $"Required maximum version {dependency.MaxVersion}, but found {dependentMod.Version}"
                    });
                    result.IsSatisfied = false;
                }
            }

            // 检查循环依赖
            var circularDependencies = DetectCircularDependencies(mod);
            if (circularDependencies.Any())
            {
                result.CircularDependencies.AddRange(circularDependencies);
                result.IsSatisfied = false;
            }

            return result;
        }

        private async Task LoadModAssembliesAsync(Mod mod)
        {
            foreach (var entryPoint in mod.Manifest.EntryPoints)
            {
                if (!string.IsNullOrEmpty(entryPoint.Assembly))
                {
                    var assemblyPath = Path.Combine(mod.Path, entryPoint.Assembly);
                    if (File.Exists(assemblyPath))
                    {
                        // 在安全上下文中加载程序集
                        var assembly = await _securityManager.LoadAssemblySecurelyAsync(assemblyPath);
                        mod.LoadedAssemblies.Add(assembly);
                    }
                }
            }
        }

        private List<CircularDependency> DetectCircularDependencies(IMod mod)
        {
            var circularDependencies = new List<CircularDependency>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            DetectCircularDependenciesRecursive(mod.Id, visited, recursionStack, new List<string>(), circularDependencies);

            return circularDependencies;
        }

        private void DetectCircularDependenciesRecursive(
            string modId, 
            HashSet<string> visited, 
            HashSet<string> recursionStack, 
            List<string> currentPath, 
            List<CircularDependency> circularDependencies)
        {
            if (recursionStack.Contains(modId))
            {
                // 发现循环依赖
                var cycleStart = currentPath.IndexOf(modId);
                var cyclePath = currentPath.Skip(cycleStart).Concat(new[] { modId }).ToList();
                
                circularDependencies.Add(new CircularDependency
                {
                    DependencyPath = cyclePath,
                    Description = $"Circular dependency detected: {string.Join(" -> ", cyclePath)}"
                });
                return;
            }

            if (visited.Contains(modId))
                return;

            visited.Add(modId);
            recursionStack.Add(modId);
            currentPath.Add(modId);

            if (_loadedMods.TryGetValue(modId, out var mod))
            {
                foreach (var dependency in mod.Dependencies)
                {
                    DetectCircularDependenciesRecursive(dependency.ModId, visited, recursionStack, currentPath, circularDependencies);
                }
            }

            recursionStack.Remove(modId);
            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }

    /// <summary>
    /// 模组加载异常
    /// </summary>
    public class ModLoadException : Exception
    {
        public ModLoadException(string message) : base(message) { }
        public ModLoadException(string message, Exception innerException) : base(message, innerException) { }
    }
}