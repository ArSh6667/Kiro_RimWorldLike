using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldFramework.Core.Mods;

namespace RimWorldFramework.Tests.Mods
{
    /// <summary>
    /// 模组系统集成测试
    /// 验证模组加载、管理、冲突检测和错误隔离的完整工作流程
    /// </summary>
    [TestFixture]
    public class ModSystemIntegrationTests
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

        [Test]
        public async Task CompleteModWorkflow_ShouldWorkEndToEnd()
        {
            // 创建测试模组
            var mod1 = CreateTestMod("TestMod1", "Test Mod 1", new Version(1, 0));
            var mod2 = CreateTestMod("TestMod2", "Test Mod 2", new Version(1, 0));
            var mod3 = CreateTestMod("TestMod3", "Test Mod 3", new Version(1, 0), 
                dependencies: new[] { new ModDependency { ModId = "TestMod1", IsOptional = false } });

            // 1. 扫描模组目录
            var scannedMods = await _modManager.ScanModsDirectoryAsync(_testModsDirectory);
            Assert.That(scannedMods.Count(), Is.EqualTo(3));

            // 2. 加载模组
            var loadedMod1 = await _modManager.LoadModAsync(mod1);
            var loadedMod2 = await _modManager.LoadModAsync(mod2);
            var loadedMod3 = await _modManager.LoadModAsync(mod3);

            Assert.That(loadedMod1.Id, Is.EqualTo("TestMod1"));
            Assert.That(loadedMod2.Id, Is.EqualTo("TestMod2"));
            Assert.That(loadedMod3.Id, Is.EqualTo("TestMod3"));

            // 3. 验证加载顺序
            var loadOrder = _modManager.GetLoadOrder().ToList();
            Assert.That(loadOrder.Count, Is.EqualTo(3));

            // 4. 启用模组（按依赖顺序）
            await _modManager.EnableModAsync("TestMod1");
            await _modManager.EnableModAsync("TestMod2");
            await _modManager.EnableModAsync("TestMod3");

            var enabledMods = _modManager.GetEnabledMods().ToList();
            Assert.That(enabledMods.Count, Is.EqualTo(3));

            // 5. 检测冲突
            var conflictResult = await _modManager.DetectConflictsAsync();
            Assert.That(conflictResult.HasConflicts, Is.False);

            // 6. 禁用模组
            await _modManager.DisableModAsync("TestMod2");
            var enabledAfterDisable = _modManager.GetEnabledMods().ToList();
            Assert.That(enabledAfterDisable.Count, Is.EqualTo(2));

            // 7. 卸载模组
            await _modManager.UnloadModAsync("TestMod2");
            var allMods = _modManager.GetAllMods().ToList();
            Assert.That(allMods.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task ModConflictDetection_ShouldDetectAndResolveConflicts()
        {
            // 创建有冲突的模组
            var mod1 = CreateTestMod("ConflictMod1", "Conflict Mod 1", new Version(1, 0));
            var mod2 = CreateTestMod("ConflictMod2", "Conflict Mod 2", new Version(1, 0),
                dependencies: new[] { new ModDependency { ModId = "NonExistentMod", IsOptional = false } });

            // 加载模组
            await _modManager.LoadModAsync(mod1);
            await _modManager.LoadModAsync(mod2);

            // 启用模组
            await _modManager.EnableModAsync("ConflictMod1");
            
            // 尝试启用有依赖问题的模组应该失败
            Assert.ThrowsAsync<ModDependencyException>(async () =>
            {
                await _modManager.EnableModAsync("ConflictMod2");
            });

            // 检测冲突
            var conflictResult = await _modManager.DetectConflictsAsync();
            Assert.That(conflictResult.HasConflicts, Is.True);
            Assert.That(conflictResult.Conflicts.Any(c => c.Type == ConflictType.DependencyConflict), Is.True);

            // 尝试解决冲突
            var resolutionResult = await _modManager.ResolveConflictsAsync(conflictResult.Conflicts);
            Assert.That(resolutionResult, Is.Not.Null);
        }

        [Test]
        public async Task ModHotReload_ShouldUpdateModSuccessfully()
        {
            // 创建测试模组
            var modPath = CreateTestMod("ReloadMod", "Reload Test Mod", new Version(1, 0));
            
            // 加载并启用模组
            var mod = await _modManager.LoadModAsync(modPath);
            await _modManager.EnableModAsync(mod.Id);
            
            Assert.That(mod.Version, Is.EqualTo(new Version(1, 0)));

            // 更新模组文件
            UpdateTestMod(modPath, "ReloadMod", "Reload Test Mod Updated", new Version(1, 1));

            // 热重载
            await _modManager.ReloadModAsync(mod.Id);

            // 验证更新
            var reloadedMod = _modManager.GetMod(mod.Id);
            Assert.That(reloadedMod.Version, Is.EqualTo(new Version(1, 1)));
            Assert.That(reloadedMod.Name, Is.EqualTo("Reload Test Mod Updated"));
        }

        [Test]
        public async Task ModErrorIsolation_ShouldNotAffectOtherMods()
        {
            // 创建多个模组
            var mod1 = CreateTestMod("StableMod1", "Stable Mod 1", new Version(1, 0));
            var mod2 = CreateTestMod("StableMod2", "Stable Mod 2", new Version(1, 0));
            var faultyMod = CreateTestMod("FaultyMod", "Faulty Mod", new Version(1, 0));

            // 加载所有模组
            await _modManager.LoadModAsync(mod1);
            await _modManager.LoadModAsync(mod2);
            await _modManager.LoadModAsync(faultyMod);

            // 启用所有模组
            await _modManager.EnableModAsync("StableMod1");
            await _modManager.EnableModAsync("StableMod2");
            await _modManager.EnableModAsync("FaultyMod");

            // 模拟错误模组出现问题
            await _modManager.DisableModAsync("FaultyMod");

            // 验证其他模组仍然正常
            var stableMod1 = _modManager.GetMod("StableMod1");
            var stableMod2 = _modManager.GetMod("StableMod2");

            Assert.That(stableMod1.Status, Is.Not.EqualTo(ModStatus.Error));
            Assert.That(stableMod2.Status, Is.Not.EqualTo(ModStatus.Error));

            var enabledMods = _modManager.GetEnabledMods().ToList();
            Assert.That(enabledMods.Count, Is.EqualTo(2));
            Assert.That(enabledMods.Any(m => m.Id == "StableMod1"), Is.True);
            Assert.That(enabledMods.Any(m => m.Id == "StableMod2"), Is.True);
        }

        [Test]
        public async Task ModLoadOrder_ShouldRespectDependencies()
        {
            // 创建有依赖关系的模组
            var baseModPath = CreateTestMod("BaseMod", "Base Mod", new Version(1, 0));
            var dependentModPath = CreateTestMod("DependentMod", "Dependent Mod", new Version(1, 0),
                dependencies: new[] { new ModDependency { ModId = "BaseMod", IsOptional = false } });

            // 加载模组
            var baseMod = await _modManager.LoadModAsync(baseModPath);
            var dependentMod = await _modManager.LoadModAsync(dependentModPath);

            // 设置加载顺序（依赖模组在前）
            await _modManager.SetLoadOrderAsync(new[] { "BaseMod", "DependentMod" });

            // 启用模组
            await _modManager.EnableModAsync("BaseMod");
            await _modManager.EnableModAsync("DependentMod");

            // 验证加载顺序
            var loadOrder = _modManager.GetLoadOrder().ToList();
            var baseModIndex = loadOrder.FindIndex(m => m.Id == "BaseMod");
            var dependentModIndex = loadOrder.FindIndex(m => m.Id == "DependentMod");

            Assert.That(baseModIndex, Is.LessThan(dependentModIndex));

            // 检测冲突（不应该有加载顺序冲突）
            var conflictResult = await _modManager.DetectConflictsAsync();
            var loadOrderConflicts = conflictResult.Conflicts.Where(c => c.Type == ConflictType.LoadOrderConflict);
            Assert.That(loadOrderConflicts.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task ModEvents_ShouldFireCorrectly()
        {
            var statusChangedEvents = new List<ModStatusChangedEventArgs>();
            var conflictDetectedEvents = new List<ModConflictDetectedEventArgs>();

            // 订阅事件
            _modManager.ModStatusChanged += (sender, args) => statusChangedEvents.Add(args);
            _modManager.ModConflictDetected += (sender, args) => conflictDetectedEvents.Add(args);

            // 创建和加载模组
            var modPath = CreateTestMod("EventMod", "Event Test Mod", new Version(1, 0));
            var mod = await _modManager.LoadModAsync(modPath);

            // 验证状态变化事件
            Assert.That(statusChangedEvents.Count, Is.GreaterThan(0));
            Assert.That(statusChangedEvents.Any(e => e.ModId == "EventMod"), Is.True);

            // 启用模组
            await _modManager.EnableModAsync(mod.Id);

            // 验证更多状态变化事件
            Assert.That(statusChangedEvents.Count(e => e.ModId == "EventMod"), Is.GreaterThan(1));

            // 创建冲突模组来触发冲突事件
            var conflictModPath = CreateTestMod("ConflictEventMod", "Conflict Event Mod", new Version(1, 0),
                dependencies: new[] { new ModDependency { ModId = "NonExistentMod", IsOptional = false } });
            
            await _modManager.LoadModAsync(conflictModPath);
            
            try
            {
                await _modManager.EnableModAsync("ConflictEventMod");
            }
            catch (ModDependencyException)
            {
                // 预期的异常
            }

            // 检测冲突应该触发冲突事件
            await _modManager.DetectConflictsAsync();
            Assert.That(conflictDetectedEvents.Count, Is.GreaterThan(0));
        }

        #region 辅助方法

        private string CreateTestMod(string id, string name, Version version, ModDependency[] dependencies = null)
        {
            var modDir = Path.Combine(_testModsDirectory, id);
            Directory.CreateDirectory(modDir);

            var manifest = new ModManifest
            {
                Id = id,
                Name = name,
                Version = version,
                Author = "Test Author",
                Description = "Test mod for integration testing",
                Dependencies = dependencies?.ToList() ?? new List<ModDependency>(),
                ApiVersion = new Version(1, 0)
            };

            var manifestPath = Path.Combine(modDir, "mod.json");
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, manifestJson);

            // 创建一个简单的程序集文件
            var assemblyPath = Path.Combine(modDir, $"{id}.dll");
            File.WriteAllBytes(assemblyPath, new byte[] { 0x4D, 0x5A }); // 简单的PE头

            return modDir;
        }

        private void UpdateTestMod(string modPath, string id, string name, Version version)
        {
            var manifest = new ModManifest
            {
                Id = id,
                Name = name,
                Version = version,
                Author = "Test Author",
                Description = "Updated test mod for integration testing",
                Dependencies = new List<ModDependency>(),
                ApiVersion = new Version(1, 0)
            };

            var manifestPath = Path.Combine(modPath, "mod.json");
            var manifestJson = System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, manifestJson);
        }

        #endregion
    }
}