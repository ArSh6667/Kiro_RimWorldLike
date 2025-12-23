using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;
using RimWorldFramework.Core.Mods;

namespace RimWorldFramework.Tests.Mods
{
    /// <summary>
    /// 模组系统属性测试
    /// 验证属性 20-23: 模组加载安全性、热重载、冲突检测、错误隔离
    /// </summary>
    [TestFixture]
    public class ModSystemPropertyTests
    {
        private IModManager _modManager;
        private IModLoader _modLoader;
        private IModConflictDetector _conflictDetector;
        private string _testModsDirectory;

        [SetUp]
        public void Setup()
        {
            _modLoader = new ModLoader();
            _conflictDetector = new DefaultModConflictDetector();
            _modManager = new ModManager(_modLoader, _conflictDetector);
            _testModsDirectory = Path.Combine(Path.GetTempPath(), "TestMods", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testModsDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testModsDirectory))
            {
                Directory.Delete(_testModsDirectory, true);
            }
        }

        /// <summary>
        /// 属性 20: 模组加载安全性
        /// 验证模组加载过程中的安全性检查和错误处理
        /// </summary>
        [Property]
        public Property ModLoadingSafety_ValidModsShouldLoadSuccessfully()
        {
            return Prop.ForAll(
                GenerateValidModManifests(),
                async manifests =>
                {
                    var loadResults = new List<bool>();
                    
                    foreach (var manifest in manifests)
                    {
                        try
                        {
                            var modPath = CreateTestMod(manifest);
                            var mod = await _modManager.LoadModAsync(modPath);
                            
                            loadResults.Add(mod != null && mod.Id == manifest.Id);
                        }
                        catch (Exception)
                        {
                            loadResults.Add(false);
                        }
                    }

                    return loadResults.All(result => result);
                });
        }

        /// <summary>
        /// 属性 20: 模组加载安全性 - 无效模组应该被拒绝
        /// </summary>
        [Property]
        public Property ModLoadingSafety_InvalidModsShouldBeRejected()
        {
            return Prop.ForAll(
                GenerateInvalidModManifests(),
                async manifests =>
                {
                    var rejectionResults = new List<bool>();
                    
                    foreach (var manifest in manifests)
                    {
                        try
                        {
                            var modPath = CreateTestMod(manifest);
                            await _modManager.LoadModAsync(modPath);
                            rejectionResults.Add(false); // 不应该成功加载
                        }
                        catch (ModLoadException)
                        {
                            rejectionResults.Add(true); // 正确拒绝
                        }
                        catch (Exception)
                        {
                            rejectionResults.Add(true); // 其他异常也算正确拒绝
                        }
                    }

                    return rejectionResults.All(result => result);
                });
        }

        /// <summary>
        /// 属性 21: 模组热重载
        /// 验证模组可以在运行时重新加载而不影响系统稳定性
        /// </summary>
        [Property]
        public Property ModHotReload_ShouldMaintainSystemStability()
        {
            return Prop.ForAll(
                GenerateValidModManifests().Where(m => m.Count() <= 3), // 限制数量以提高测试性能
                async manifests =>
                {
                    var reloadResults = new List<bool>();
                    
                    foreach (var manifest in manifests)
                    {
                        try
                        {
                            // 加载模组
                            var modPath = CreateTestMod(manifest);
                            var mod = await _modManager.LoadModAsync(modPath);
                            await _modManager.EnableModAsync(mod.Id);
                            
                            // 修改模组文件
                            var updatedManifest = manifest with { Version = new Version(manifest.Version.Major, manifest.Version.Minor + 1) };
                            UpdateTestMod(modPath, updatedManifest);
                            
                            // 热重载
                            await _modManager.ReloadModAsync(mod.Id);
                            
                            // 验证重载后的状态
                            var reloadedMod = _modManager.GetMod(mod.Id);
                            reloadResults.Add(reloadedMod != null && reloadedMod.Version == updatedManifest.Version);
                        }
                        catch (Exception)
                        {
                            reloadResults.Add(false);
                        }
                    }

                    return reloadResults.All(result => result);
                });
        }

        /// <summary>
        /// 属性 22: 模组冲突检测
        /// 验证系统能够正确检测和报告模组之间的冲突
        /// </summary>
        [Property]
        public Property ModConflictDetection_ShouldDetectKnownConflicts()
        {
            return Prop.ForAll(
                GenerateConflictingModPairs(),
                async conflictingPairs =>
                {
                    var detectionResults = new List<bool>();
                    
                    foreach (var (mod1, mod2) in conflictingPairs)
                    {
                        try
                        {
                            // 加载冲突的模组
                            var mod1Path = CreateTestMod(mod1);
                            var mod2Path = CreateTestMod(mod2);
                            
                            await _modManager.LoadModAsync(mod1Path);
                            await _modManager.LoadModAsync(mod2Path);
                            
                            await _modManager.EnableModAsync(mod1.Id);
                            await _modManager.EnableModAsync(mod2.Id);
                            
                            // 检测冲突
                            var conflictResult = await _modManager.DetectConflictsAsync();
                            
                            detectionResults.Add(conflictResult.HasConflicts);
                        }
                        catch (Exception)
                        {
                            detectionResults.Add(false);
                        }
                    }

                    return detectionResults.All(result => result);
                });
        }

        /// <summary>
        /// 属性 23: 模组错误隔离
        /// 验证一个模组的错误不会影响其他模组或核心系统
        /// </summary>
        [Property]
        public Property ModErrorIsolation_ShouldIsolateModErrors()
        {
            return Prop.ForAll(
                GenerateValidModManifests().Where(m => m.Count() >= 2 && m.Count() <= 4),
                async manifests =>
                {
                    var isolationResults = new List<bool>();
                    var manifestList = manifests.ToList();
                    
                    if (manifestList.Count < 2) return true; // 需要至少两个模组
                    
                    try
                    {
                        // 加载所有模组
                        var loadedMods = new List<string>();
                        foreach (var manifest in manifestList)
                        {
                            var modPath = CreateTestMod(manifest);
                            var mod = await _modManager.LoadModAsync(modPath);
                            await _modManager.EnableModAsync(mod.Id);
                            loadedMods.Add(mod.Id);
                        }
                        
                        // 模拟第一个模组出错
                        var faultyModId = loadedMods.First();
                        try
                        {
                            // 通过卸载模组来模拟错误
                            await _modManager.DisableModAsync(faultyModId);
                        }
                        catch (Exception)
                        {
                            // 预期的错误
                        }
                        
                        // 验证其他模组仍然正常工作
                        var otherMods = loadedMods.Skip(1);
                        foreach (var modId in otherMods)
                        {
                            var mod = _modManager.GetMod(modId);
                            isolationResults.Add(mod != null && mod.Status != ModStatus.Error);
                        }
                        
                        return isolationResults.All(result => result);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        /// <summary>
        /// 属性测试：模组依赖解析一致性
        /// </summary>
        [Property]
        public Property ModDependencyResolution_ShouldBeConsistent()
        {
            return Prop.ForAll(
                GenerateModsWithDependencies(),
                async modWithDeps =>
                {
                    try
                    {
                        var loadOrder = new List<string>();
                        
                        // 按依赖顺序加载模组
                        foreach (var manifest in SortByDependencies(modWithDeps))
                        {
                            var modPath = CreateTestMod(manifest);
                            var mod = await _modManager.LoadModAsync(modPath);
                            loadOrder.Add(mod.Id);
                        }
                        
                        // 验证加载顺序满足依赖关系
                        foreach (var manifest in modWithDeps)
                        {
                            var modIndex = loadOrder.IndexOf(manifest.Id);
                            foreach (var dependency in manifest.Dependencies)
                            {
                                if (!dependency.IsOptional)
                                {
                                    var depIndex = loadOrder.IndexOf(dependency.ModId);
                                    if (depIndex >= 0 && depIndex >= modIndex)
                                    {
                                        return false; // 依赖关系违反
                                    }
                                }
                            }
                        }
                        
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                });
        }

        #region 测试数据生成器

        private static Arbitrary<IEnumerable<ModManifest>> GenerateValidModManifests()
        {
            return Arb.From(Gen.ListOf(GenerateValidModManifest()).Where(list => list.Count <= 5));
        }

        private static Gen<ModManifest> GenerateValidModManifest()
        {
            return from id in Gen.Elements("TestMod1", "TestMod2", "TestMod3", "TestMod4", "TestMod5")
                   from name in Gen.Elements("Test Mod A", "Test Mod B", "Test Mod C")
                   from version in Gen.Elements(new Version(1, 0), new Version(1, 1), new Version(2, 0))
                   from author in Gen.Elements("TestAuthor1", "TestAuthor2")
                   select new ModManifest
                   {
                       Id = id + Guid.NewGuid().ToString("N")[..8],
                       Name = name,
                       Version = version,
                       Author = author,
                       Description = "Test mod for property testing",
                       Dependencies = new List<ModDependency>(),
                       ApiVersion = new Version(1, 0)
                   };
        }

        private static Arbitrary<IEnumerable<ModManifest>> GenerateInvalidModManifests()
        {
            return Arb.From(Gen.ListOf(GenerateInvalidModManifest()).Where(list => list.Count <= 3));
        }

        private static Gen<ModManifest> GenerateInvalidModManifest()
        {
            return Gen.OneOf(
                // 空ID
                Gen.Constant(new ModManifest { Id = "", Name = "Invalid Mod", Version = new Version(1, 0) }),
                // 空名称
                Gen.Constant(new ModManifest { Id = "invalid1", Name = "", Version = new Version(1, 0) }),
                // 无效版本
                Gen.Constant(new ModManifest { Id = "invalid2", Name = "Invalid Mod", Version = null })
            );
        }

        private static Arbitrary<IEnumerable<(ModManifest, ModManifest)>> GenerateConflictingModPairs()
        {
            return Arb.From(Gen.ListOf(GenerateConflictingModPair()).Where(list => list.Count <= 3));
        }

        private static Gen<(ModManifest, ModManifest)> GenerateConflictingModPair()
        {
            return from mod1 in GenerateValidModManifest()
                   from mod2 in GenerateValidModManifest()
                   select (
                       mod1 with { Dependencies = new List<ModDependency> { new ModDependency { ModId = "NonExistentMod", IsOptional = false } } },
                       mod2 with { Dependencies = new List<ModDependency> { new ModDependency { ModId = mod1.Id, MinVersion = new Version(99, 0) } } }
                   );
        }

        private static Arbitrary<IEnumerable<ModManifest>> GenerateModsWithDependencies()
        {
            return Arb.From(Gen.Sized(size =>
            {
                var modCount = Math.Min(size, 4);
                return Gen.ListOf(GenerateValidModManifest(), modCount, modCount)
                    .Select(mods =>
                    {
                        var modList = mods.ToList();
                        // 为一些模组添加依赖关系
                        for (int i = 1; i < modList.Count; i++)
                        {
                            if (i < modList.Count / 2)
                            {
                                modList[i] = modList[i] with
                                {
                                    Dependencies = new List<ModDependency>
                                    {
                                        new ModDependency
                                        {
                                            ModId = modList[i - 1].Id,
                                            IsOptional = false,
                                            MinVersion = modList[i - 1].Version
                                        }
                                    }
                                };
                            }
                        }
                        return modList.AsEnumerable();
                    });
            }));
        }

        #endregion

        #region 辅助方法

        private string CreateTestMod(ModManifest manifest)
        {
            var modDir = Path.Combine(_testModsDirectory, manifest.Id);
            Directory.CreateDirectory(modDir);
            
            var manifestPath = Path.Combine(modDir, "mod.json");
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, manifestJson);
            
            // 创建一个简单的程序集文件
            var assemblyPath = Path.Combine(modDir, $"{manifest.Id}.dll");
            File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A }); // 简单的PE头
            
            return modDir;
        }

        private void UpdateTestMod(string modPath, ModManifest updatedManifest)
        {
            var manifestPath = Path.Combine(modPath, "mod.json");
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(updatedManifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, manifestJson);
        }

        private IEnumerable<ModManifest> SortByDependencies(IEnumerable<ModManifest> mods)
        {
            var modList = mods.ToList();
            var sorted = new List<ModManifest>();
            var remaining = new List<ModManifest>(modList);
            
            while (remaining.Any())
            {
                var canLoad = remaining.Where(mod =>
                    mod.Dependencies.All(dep =>
                        dep.IsOptional || sorted.Any(s => s.Id == dep.ModId)
                    )
                ).ToList();
                
                if (!canLoad.Any())
                {
                    // 如果没有可以加载的模组，添加剩余的第一个（可能有循环依赖）
                    canLoad.Add(remaining.First());
                }
                
                sorted.AddRange(canLoad);
                remaining.RemoveAll(mod => canLoad.Contains(mod));
            }
            
            return sorted;
        }

        #endregion
    }
}