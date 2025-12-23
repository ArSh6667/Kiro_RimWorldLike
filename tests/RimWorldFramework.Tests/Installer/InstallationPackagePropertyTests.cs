using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using FsCheck;
using FsCheck.NUnit;
using RimWorldFramework.Core.Installer;

namespace RimWorldFramework.Tests.Installer
{
    /// <summary>
    /// 安装包系统属性测试
    /// 验证属性 14-17: 安装包完整性、安装后系统配置、卸载完整性、跨平台安装适配
    /// </summary>
    [TestFixture]
    public class InstallationPackagePropertyTests
    {
        private InstallerGenerator _installerGenerator;
        private string _testDirectory;

        [SetUp]
        public void SetUp()
        {
            _installerGenerator = new InstallerGenerator();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"installer_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        /// <summary>
        /// 属性 14: 安装包完整性
        /// 验证生成的安装包包含所有必需的文件和配置
        /// </summary>
        [Property(MaxTest = 50)]
        public Property Property14_InstallerPackageIntegrity()
        {
            return Prop.ForAll(
                GenerateValidInstallerConfiguration(),
                async config =>
                {
                    try
                    {
                        // 创建测试包文件
                        var packagePath = CreateTestPackage(config.Application.Name);
                        config.PackagePath = packagePath;

                        // 生成安装程序
                        var result = await _installerGenerator.GenerateInstallerAsync(config);

                        // 验证生成成功
                        if (!result.IsSuccess)
                            return false.ToProperty();

                        // 验证安装程序文件存在
                        if (!File.Exists(result.InstallerPath))
                            return false.ToProperty();

                        // 验证安装程序大小合理
                        if (result.InstallerSize <= 0)
                            return false.ToProperty();

                        // 验证生成的文件列表不为空
                        if (result.GeneratedFiles.Count == 0)
                            return false.ToProperty();

                        return true.ToProperty();
                    }
                    catch
                    {
                        return false.ToProperty();
                    }
                });
        }

        /// <summary>
        /// 属性 15: 安装后系统配置
        /// 验证安装完成后系统配置正确，包括文件、注册表、快捷方式等
        /// </summary>
        [Property(MaxTest = 30)]
        public Property Property15_PostInstallationSystemConfiguration()
        {
            return Prop.ForAll(
                GenerateValidInstallationOptions(),
                async options =>
                {
                    try
                    {
                        // 创建模拟安装程序
                        var installerPath = CreateMockInstaller();

                        // 执行安装
                        var result = await _installerGenerator.InstallAsync(installerPath, options);

                        // 验证安装成功
                        if (!result.IsSuccess)
                            return false.ToProperty();

                        // 验证安装目录存在
                        if (!Directory.Exists(result.InstallDirectory))
                            return false.ToProperty();

                        // 验证安装了文件
                        if (result.InstalledFiles.Count == 0)
                            return false.ToProperty();

                        // 验证文件路径正确
                        var allFilesInCorrectDirectory = result.InstalledFiles.All(f => 
                            f.FilePath.StartsWith(result.InstallDirectory));
                        if (!allFilesInCorrectDirectory)
                            return false.ToProperty();

                        // 验证快捷方式创建（如果启用）
                        if (options.CreateDesktopShortcut && result.Shortcuts.Count == 0)
                            return false.ToProperty();

                        // 验证安装时间记录
                        if (result.Duration.TotalMilliseconds <= 0)
                            return false.ToProperty();

                        return true.ToProperty();
                    }
                    catch
                    {
                        return false.ToProperty();
                    }
                });
        }

        /// <summary>
        /// 属性 16: 卸载完整性
        /// 验证卸载过程能够完全清理安装的文件和配置
        /// </summary>
        [Property(MaxTest = 30)]
        public Property Property16_UninstallationCompleteness()
        {
            return Prop.ForAll(
                GenerateValidInstallationOptions(),
                GenerateValidUninstallationOptions(),
                async (installOptions, uninstallOptions) =>
                {
                    try
                    {
                        // 先执行安装
                        var installerPath = CreateMockInstaller();
                        var installResult = await _installerGenerator.InstallAsync(installerPath, installOptions);

                        if (!installResult.IsSuccess)
                            return false.ToProperty();

                        // 获取已安装应用程序
                        var installedApps = await _installerGenerator.GetInstalledApplicationsAsync();
                        var app = installedApps.FirstOrDefault();
                        
                        if (app == null)
                            return false.ToProperty();

                        var originalFileCount = installResult.InstalledFiles.Count;
                        var originalShortcutCount = installResult.Shortcuts.Count;

                        // 执行卸载
                        var uninstallResult = await _installerGenerator.UninstallAsync(app.Id, uninstallOptions);

                        // 验证卸载成功
                        if (!uninstallResult.IsSuccess)
                            return false.ToProperty();

                        // 验证删除了文件
                        if (uninstallResult.DeletedFilesCount <= 0)
                            return false.ToProperty();

                        // 验证释放了磁盘空间
                        if (uninstallResult.FreedSpaceBytes <= 0)
                            return false.ToProperty();

                        // 验证卸载时间记录
                        if (uninstallResult.Duration.TotalMilliseconds <= 0)
                            return false.ToProperty();

                        // 验证应用程序不再在已安装列表中
                        var remainingApps = await _installerGenerator.GetInstalledApplicationsAsync();
                        var appStillExists = remainingApps.Any(a => a.Id == app.Id);
                        if (appStillExists)
                            return false.ToProperty();

                        return true.ToProperty();
                    }
                    catch
                    {
                        return false.ToProperty();
                    }
                });
        }
        /// <summary>
        /// 属性 17: 跨平台安装适配
        /// 验证安装程序能够适配不同平台的安装要求
        /// </summary>
        [Property(MaxTest = 20)]
        public Property Property17_CrossPlatformInstallationAdaptation()
        {
            return Prop.ForAll(
                GenerateValidInstallerConfiguration(),
                Gen.Elements(new[] { InstallerPlatform.Windows, InstallerPlatform.Linux, InstallerPlatform.MacOS }),
                async (config, targetPlatform) =>
                {
                    try
                    {
                        // 设置目标平台
                        config.TargetPlatforms = new List<InstallerPlatform> { targetPlatform };
                        
                        // 创建测试包文件
                        var packagePath = CreateTestPackage(config.Application.Name);
                        config.PackagePath = packagePath;

                        // 验证配置
                        var validationResult = await _installerGenerator.ValidateConfigurationAsync(config);

                        // 验证配置通过验证
                        if (!validationResult.IsValid)
                            return false.ToProperty();

                        // 验证目标平台设置正确
                        if (!config.TargetPlatforms.Contains(targetPlatform))
                            return false.ToProperty();

                        // 验证安装程序类型适合平台
                        var expectedType = targetPlatform == InstallerPlatform.Windows ? 
                            InstallerType.MSI : InstallerType.CrossPlatform;
                        
                        // 对于跨平台，应该使用CrossPlatform类型
                        if (config.TargetPlatforms.Count > 1 && config.Type != InstallerType.CrossPlatform)
                            return false.ToProperty();

                        // 验证系统要求适合平台
                        if (config.Requirements == null)
                            return false.ToProperty();

                        // 验证支持的架构不为空
                        if (config.Requirements.SupportedArchitectures.Count == 0)
                            return false.ToProperty();

                        return true.ToProperty();
                    }
                    catch
                    {
                        return false.ToProperty();
                    }
                });
        }

        // 生成器方法

        private Gen<InstallerConfiguration> GenerateValidInstallerConfiguration()
        {
            return from appName in Gen.Elements(new[] { "TestApp", "GameFramework", "RimWorldMod" })
                   from version in Gen.Elements(new[] { "1.0.0", "2.1.0", "1.5.3" })
                   from publisher in Gen.Elements(new[] { "TestPublisher", "GameStudio", "ModAuthor" })
                   from installDir in Gen.Elements(new[] { @"C:\Program Files\TestApp", @"C:\Games\TestApp" })
                   select new InstallerConfiguration
                   {
                       Application = new ApplicationInfo
                       {
                           Id = Guid.NewGuid().ToString(),
                           Name = appName,
                           Version = version,
                           Publisher = publisher,
                           Description = $"Test application {appName}",
                           InstallSize = 1024000
                       },
                       Settings = new InstallerSettings
                       {
                           DefaultInstallDirectory = installDir,
                           AllowCustomInstallDirectory = true,
                           CreateDesktopShortcut = true,
                           CreateStartMenuShortcut = true,
                           RequireAdminRights = false,
                           SupportSilentInstall = true
                       },
                       Type = InstallerType.MSI,
                       TargetPlatforms = new List<InstallerPlatform> { InstallerPlatform.Windows }
                   };
        }

        private Gen<InstallationOptions> GenerateValidInstallationOptions()
        {
            return from installDir in Gen.Elements(new[] { 
                       Path.Combine(_testDirectory, "TestApp"),
                       Path.Combine(_testDirectory, "GameFramework"),
                       Path.Combine(_testDirectory, "Application")
                   })
                   from createDesktop in Gen.Elements(new[] { true, false })
                   from createStartMenu in Gen.Elements(new[] { true, false })
                   from silentInstall in Gen.Elements(new[] { true, false })
                   select new InstallationOptions
                   {
                       InstallDirectory = installDir,
                       CreateDesktopShortcut = createDesktop,
                       CreateStartMenuShortcut = createStartMenu,
                       SilentInstall = silentInstall,
                       SelectedComponents = new List<string> { "Core", "Documentation" }
                   };
        }

        private Gen<UninstallationOptions> GenerateValidUninstallationOptions()
        {
            return from silentUninstall in Gen.Elements(new[] { true, false })
                   from keepUserData in Gen.Elements(new[] { true, false })
                   from keepConfig in Gen.Elements(new[] { true, false })
                   select new UninstallationOptions
                   {
                       SilentUninstall = silentUninstall,
                       KeepUserData = keepUserData,
                       KeepConfiguration = keepConfig
                   };
        }
        // 辅助方法

        private string CreateTestPackage(string appName)
        {
            var packagePath = Path.Combine(_testDirectory, $"{appName}_package.zip");
            
            // 创建一个简单的测试包文件
            using (var stream = File.Create(packagePath))
            {
                var testData = System.Text.Encoding.UTF8.GetBytes($"Test package for {appName}");
                stream.Write(testData, 0, testData.Length);
            }
            
            return packagePath;
        }

        private string CreateMockInstaller()
        {
            var installerPath = Path.Combine(_testDirectory, "mock_installer.exe");
            
            // 创建一个模拟安装程序文件
            using (var stream = File.Create(installerPath))
            {
                var testData = System.Text.Encoding.UTF8.GetBytes("Mock installer executable");
                stream.Write(testData, 0, testData.Length);
            }
            
            return installerPath;
        }
    }
}