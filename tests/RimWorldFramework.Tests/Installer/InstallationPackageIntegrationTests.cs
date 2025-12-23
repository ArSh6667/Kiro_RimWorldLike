using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldFramework.Core.Installer;

namespace RimWorldFramework.Tests.Installer
{
    /// <summary>
    /// 安装包系统集成测试
    /// 测试完整的构建、安装、卸载工作流程
    /// </summary>
    [TestFixture]
    public class InstallationPackageIntegrationTests
    {
        private InstallerGenerator _installerGenerator;
        private string _testDirectory;

        [SetUp]
        public void SetUp()
        {
            _installerGenerator = new InstallerGenerator();
            _testDirectory = Path.Combine(Path.GetTempPath(), $"installer_integration_test_{Guid.NewGuid()}");
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

        [Test]
        public async Task CompleteInstallationWorkflow_ShouldSucceed()
        {
            // Arrange
            var appInfo = new ApplicationInfo
            {
                Id = Guid.NewGuid().ToString(),
                Name = "TestApplication",
                Version = "1.0.0",
                Publisher = "Test Publisher",
                Description = "Test application for integration testing",
                InstallSize = 1024000
            };

            var settings = new InstallerSettings
            {
                DefaultInstallDirectory = Path.Combine(_testDirectory, "TestApp"),
                CreateDesktopShortcut = true,
                CreateStartMenuShortcut = true,
                RequireAdminRights = false,
                SupportSilentInstall = true
            };

            // 创建测试包文件
            var packagePath = CreateTestPackage("TestApplication");

            // Act & Assert - 创建安装程序配置
            var configuration = _installerGenerator.CreateInstallerConfiguration(packagePath, settings);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration.PackagePath, Is.EqualTo(packagePath));
            Assert.That(configuration.Settings, Is.EqualTo(settings));

            // Act & Assert - 验证配置
            var validationResult = await _installerGenerator.ValidateConfigurationAsync(configuration);
            Assert.That(validationResult.IsValid, Is.True, 
                $"Configuration validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");

            // Act & Assert - 生成安装程序
            var generationResult = await _installerGenerator.GenerateInstallerAsync(configuration);
            Assert.That(generationResult.IsSuccess, Is.True, 
                $"Installer generation failed: {string.Join(", ", generationResult.Errors)}");
            Assert.That(File.Exists(generationResult.InstallerPath), Is.True);
            Assert.That(generationResult.InstallerSize, Is.GreaterThan(0));

            // Act & Assert - 执行安装
            var installOptions = new InstallationOptions
            {
                InstallDirectory = Path.Combine(_testDirectory, "InstalledApp"),
                CreateDesktopShortcut = true,
                CreateStartMenuShortcut = true,
                SilentInstall = false
            };

            var installResult = await _installerGenerator.InstallAsync(generationResult.InstallerPath, installOptions);
            Assert.That(installResult.IsSuccess, Is.True, 
                $"Installation failed: {string.Join(", ", installResult.Errors.Select(e => e.Message))}");
            Assert.That(Directory.Exists(installResult.InstallDirectory), Is.True);
            Assert.That(installResult.InstalledFiles.Count, Is.GreaterThan(0));

            // Act & Assert - 检查安装状态
            var installedApps = await _installerGenerator.GetInstalledApplicationsAsync();
            var installedApp = installedApps.FirstOrDefault();
            Assert.That(installedApp, Is.Not.Null);

            var installStatus = await _installerGenerator.CheckInstallationStatusAsync(installedApp.Id);
            Assert.That(installStatus.IsInstalled, Is.True);
            Assert.That(installStatus.Status, Is.EqualTo(ApplicationStatus.Normal));

            // Act & Assert - 执行卸载
            var uninstallOptions = new UninstallationOptions
            {
                SilentUninstall = false,
                KeepUserData = false,
                KeepConfiguration = false
            };

            var uninstallResult = await _installerGenerator.UninstallAsync(installedApp.Id, uninstallOptions);
            Assert.That(uninstallResult.IsSuccess, Is.True, 
                $"Uninstallation failed: {string.Join(", ", uninstallResult.Errors)}");
            Assert.That(uninstallResult.DeletedFilesCount, Is.GreaterThan(0));
            Assert.That(uninstallResult.FreedSpaceBytes, Is.GreaterThan(0));

            // 验证应用程序已从已安装列表中移除
            var remainingApps = await _installerGenerator.GetInstalledApplicationsAsync();
            Assert.That(remainingApps.Any(a => a.Id == installedApp.Id), Is.False);
        }

        [Test]
        public async Task InstallationWithEvents_ShouldTriggerCorrectEvents()
        {
            // Arrange
            var progressEvents = new List<InstallProgressEventArgs>();
            var completedEvents = new List<InstallCompletedEventArgs>();
            var errorEvents = new List<InstallErrorEventArgs>();

            _installerGenerator.InstallProgress += (sender, e) => progressEvents.Add(e);
            _installerGenerator.InstallCompleted += (sender, e) => completedEvents.Add(e);
            _installerGenerator.InstallError += (sender, e) => errorEvents.Add(e);

            var packagePath = CreateTestPackage("EventTestApp");
            var settings = new InstallerSettings
            {
                DefaultInstallDirectory = Path.Combine(_testDirectory, "EventTestApp")
            };

            var configuration = _installerGenerator.CreateInstallerConfiguration(packagePath, settings);
            var generationResult = await _installerGenerator.GenerateInstallerAsync(configuration);

            var installOptions = new InstallationOptions
            {
                InstallDirectory = Path.Combine(_testDirectory, "EventTestAppInstalled")
            };

            // Act
            var installResult = await _installerGenerator.InstallAsync(generationResult.InstallerPath, installOptions);

            // Assert
            Assert.That(installResult.IsSuccess, Is.True);
            Assert.That(progressEvents.Count, Is.GreaterThan(0), "Should have progress events");
            Assert.That(completedEvents.Count, Is.EqualTo(1), "Should have one completion event");
            Assert.That(errorEvents.Count, Is.EqualTo(0), "Should have no error events");

            // 验证进度事件的顺序和内容
            Assert.That(progressEvents.First().ProgressPercentage, Is.EqualTo(0));
            Assert.That(progressEvents.Last().ProgressPercentage, Is.EqualTo(100));
            
            // 验证完成事件
            var completedEvent = completedEvents.First();
            Assert.That(completedEvent.Result, Is.EqualTo(installResult));
            Assert.That(completedEvent.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task RepairInstallation_ShouldRestoreCorruptedFiles()
        {
            // Arrange - 先安装应用程序
            var packagePath = CreateTestPackage("RepairTestApp");
            var settings = new InstallerSettings
            {
                DefaultInstallDirectory = Path.Combine(_testDirectory, "RepairTestApp")
            };

            var configuration = _installerGenerator.CreateInstallerConfiguration(packagePath, settings);
            var generationResult = await _installerGenerator.GenerateInstallerAsync(configuration);

            var installOptions = new InstallationOptions
            {
                InstallDirectory = Path.Combine(_testDirectory, "RepairTestAppInstalled")
            };

            var installResult = await _installerGenerator.InstallAsync(generationResult.InstallerPath, installOptions);
            Assert.That(installResult.IsSuccess, Is.True);

            var installedApps = await _installerGenerator.GetInstalledApplicationsAsync();
            var app = installedApps.First();

            // Act - 执行修复
            var repairResult = await _installerGenerator.RepairInstallationAsync(app.Id);

            // Assert
            Assert.That(repairResult.IsSuccess, Is.True, 
                $"Repair failed: {string.Join(", ", repairResult.Errors)}");
            Assert.That(repairResult.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task UpdateApplication_ShouldUpgradeToNewVersion()
        {
            // Arrange - 先安装应用程序
            var packagePath = CreateTestPackage("UpdateTestApp");
            var settings = new InstallerSettings
            {
                DefaultInstallDirectory = Path.Combine(_testDirectory, "UpdateTestApp")
            };

            var configuration = _installerGenerator.CreateInstallerConfiguration(packagePath, settings);
            var generationResult = await _installerGenerator.GenerateInstallerAsync(configuration);

            var installOptions = new InstallationOptions
            {
                InstallDirectory = Path.Combine(_testDirectory, "UpdateTestAppInstalled")
            };

            var installResult = await _installerGenerator.InstallAsync(generationResult.InstallerPath, installOptions);
            Assert.That(installResult.IsSuccess, Is.True);

            var installedApps = await _installerGenerator.GetInstalledApplicationsAsync();
            var app = installedApps.First();
            var originalVersion = app.Version;

            // 创建更新包
            var updatePackagePath = CreateTestPackage("UpdateTestApp_v2");

            // Act - 执行更新
            var updateResult = await _installerGenerator.UpdateApplicationAsync(app.Id, updatePackagePath);

            // Assert
            Assert.That(updateResult.IsSuccess, Is.True, 
                $"Update failed: {string.Join(", ", updateResult.Errors)}");
            Assert.That(updateResult.OldVersion, Is.EqualTo(originalVersion));
            Assert.That(updateResult.NewVersion, Is.Not.EqualTo(originalVersion));
            Assert.That(updateResult.UpdatedFilesCount, Is.GreaterThan(0));
            Assert.That(updateResult.Duration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task CrossPlatformConfiguration_ShouldValidateCorrectly()
        {
            // Arrange
            var packagePath = CreateTestPackage("CrossPlatformApp");
            var settings = new InstallerSettings
            {
                DefaultInstallDirectory = "/opt/CrossPlatformApp"
            };

            var configuration = _installerGenerator.CreateInstallerConfiguration(packagePath, settings);
            configuration.Type = InstallerType.CrossPlatform;
            configuration.TargetPlatforms = new List<InstallerPlatform> 
            { 
                InstallerPlatform.Windows, 
                InstallerPlatform.Linux, 
                InstallerPlatform.MacOS 
            };

            // Act
            var validationResult = await _installerGenerator.ValidateConfigurationAsync(configuration);

            // Assert
            Assert.That(validationResult.IsValid, Is.True, 
                $"Cross-platform validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            Assert.That(configuration.TargetPlatforms.Count, Is.EqualTo(3));
            Assert.That(configuration.Type, Is.EqualTo(InstallerType.CrossPlatform));
        }

        private string CreateTestPackage(string appName)
        {
            var packagePath = Path.Combine(_testDirectory, $"{appName}_package.zip");
            
            // 创建一个简单的测试包文件
            using (var stream = File.Create(packagePath))
            {
                var testData = System.Text.Encoding.UTF8.GetBytes($"Test package for {appName} - {DateTime.UtcNow}");
                stream.Write(testData, 0, testData.Length);
            }
            
            return packagePath;
        }
    }
}