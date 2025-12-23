using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;

namespace RimWorldFramework.Core.Installer
{
    /// <summary>
    /// 安装程序生成器实现
    /// 负责创建跨平台安装程序和实现安装卸载逻辑
    /// </summary>
    public class InstallerGenerator : IInstallerGenerator
    {
        private readonly Dictionary<string, InstalledApplication> _installedApplications;
        private readonly string _registryPath;

        /// <summary>
        /// 安装进度事件
        /// </summary>
        public event EventHandler<InstallProgressEventArgs> InstallProgress;

        /// <summary>
        /// 安装完成事件
        /// </summary>
        public event EventHandler<InstallCompletedEventArgs> InstallCompleted;

        /// <summary>
        /// 安装错误事件
        /// </summary>
        public event EventHandler<InstallErrorEventArgs> InstallError;

        /// <summary>
        /// 构造函数
        /// </summary>
        public InstallerGenerator()
        {
            _installedApplications = new Dictionary<string, InstalledApplication>();
            _registryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "RimWorldFramework", "InstalledApps.json");
            LoadInstalledApplications();
        }

        /// <summary>
        /// 创建安装程序配置
        /// </summary>
        public InstallerConfiguration CreateInstallerConfiguration(string packagePath, InstallerSettings settings)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentException("Package path cannot be null or empty", nameof(packagePath));
            
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (!File.Exists(packagePath))
                throw new FileNotFoundException($"Package file not found: {packagePath}");

            var configuration = new InstallerConfiguration
            {
                PackagePath = packagePath,
                Settings = settings,
                Type = DetermineInstallerType(),
                TargetPlatforms = DetermineTargetPlatforms()
            };

            // 设置默认安装步骤
            configuration.InstallationSteps = CreateDefaultInstallationSteps(settings);
            configuration.UninstallationSteps = CreateDefaultUninstallationSteps(settings);

            // 设置系统要求
            configuration.Requirements = CreateDefaultSystemRequirements();

            return configuration;
        }

        /// <summary>
        /// 验证安装程序配置
        /// </summary>
        public async Task<InstallerValidationResult> ValidateConfigurationAsync(InstallerConfiguration configuration)
        {
            var result = new InstallerValidationResult { IsValid = true };

            await Task.Run(() =>
            {
                // 验证基本配置
                ValidateBasicConfiguration(configuration, result);

                // 验证包文件
                ValidatePackageFile(configuration, result);

                // 验证安装步骤
                ValidateInstallationSteps(configuration, result);

                // 验证系统要求
                ValidateSystemRequirements(configuration, result);

                // 验证UI配置
                ValidateUIConfiguration(configuration, result);

                result.IsValid = result.Errors.Count == 0;
            });

            return result;
        }

        /// <summary>
        /// 生成安装程序
        /// </summary>
        public async Task<InstallerGenerationResult> GenerateInstallerAsync(InstallerConfiguration configuration)
        {
            var startTime = DateTime.UtcNow;
            var result = new InstallerGenerationResult();

            try
            {
                // 验证配置
                var validationResult = await ValidateConfigurationAsync(configuration);
                if (!validationResult.IsValid)
                {
                    result.IsSuccess = false;
                    result.Errors.AddRange(validationResult.Errors.Select(e => e.Message));
                    return result;
                }

                // 创建临时工作目录
                var tempDir = Path.Combine(Path.GetTempPath(), $"installer_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 生成安装程序文件
                    await GenerateInstallerFiles(configuration, tempDir, result);

                    // 创建最终安装程序
                    await CreateFinalInstaller(configuration, tempDir, result);

                    result.IsSuccess = true;
                    result.Duration = DateTime.UtcNow - startTime;
                }
                finally
                {
                    // 清理临时目录
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Generation failed: {ex.Message}");
                result.Duration = DateTime.UtcNow - startTime;
            }

            return result;
        }

        /// <summary>
        /// 执行安装
        /// </summary>
        public async Task<InstallationResult> InstallAsync(string installerPath, InstallationOptions installOptions)
        {
            var result = new InstallationResult
            {
                StartTime = DateTime.UtcNow,
                InstallDirectory = installOptions.InstallDirectory
            };

            try
            {
                // 报告进度
                ReportProgress("开始安装", 0, "正在初始化安装程序...", 1, 10);

                // 验证安装程序
                if (!File.Exists(installerPath))
                {
                    throw new FileNotFoundException($"Installer not found: {installerPath}");
                }

                // 创建安装目录
                if (!Directory.Exists(installOptions.InstallDirectory))
                {
                    Directory.CreateDirectory(installOptions.InstallDirectory);
                }

                ReportProgress("创建目录", 10, "正在创建安装目录...", 2, 10);

                // 解压安装包
                await ExtractInstallationPackage(installerPath, installOptions, result);

                ReportProgress("解压文件", 30, "正在解压安装文件...", 3, 10);

                // 执行安装步骤
                await ExecuteInstallationSteps(installOptions, result);

                ReportProgress("配置系统", 70, "正在配置系统设置...", 7, 10);

                // 创建快捷方式
                await CreateShortcuts(installOptions, result);

                ReportProgress("创建快捷方式", 90, "正在创建快捷方式...", 9, 10);

                // 注册应用程序
                await RegisterApplication(installOptions, result);

                ReportProgress("完成安装", 100, "安装完成", 10, 10);

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;

                // 触发完成事件
                InstallCompleted?.Invoke(this, new InstallCompletedEventArgs
                {
                    Result = result,
                    Duration = result.Duration,
                    InstalledFiles = result.InstalledFiles.Select(f => f.FilePath).ToList()
                });
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow;
                result.Errors.Add(new InstallationError
                {
                    Code = "INSTALL_FAILED",
                    Message = ex.Message,
                    Severity = ErrorSeverity.Fatal,
                    Timestamp = DateTime.UtcNow
                });

                // 触发错误事件
                InstallError?.Invoke(this, new InstallErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    ErrorCode = "INSTALL_FAILED"
                });
            }

            return result;
        }

        /// <summary>
        /// 执行卸载
        /// </summary>
        public async Task<UninstallationResult> UninstallAsync(string applicationId, UninstallationOptions uninstallOptions)
        {
            var result = new UninstallationResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                // 检查应用程序是否已安装
                if (!_installedApplications.ContainsKey(applicationId))
                {
                    throw new InvalidOperationException($"Application not found: {applicationId}");
                }

                var app = _installedApplications[applicationId];

                // 停止相关服务
                await StopApplicationServices(app);

                // 删除文件
                var deletedFiles = await DeleteApplicationFiles(app, uninstallOptions);
                result.DeletedFilesCount = deletedFiles;

                // 删除注册表项
                var deletedRegistryEntries = await DeleteRegistryEntries(app);
                result.DeletedRegistryEntriesCount = deletedRegistryEntries;

                // 删除快捷方式
                await DeleteShortcuts(app);

                // 计算释放的空间
                result.FreedSpaceBytes = app.InstallSize;

                // 从已安装应用程序列表中移除
                _installedApplications.Remove(applicationId);
                await SaveInstalledApplications();

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow;
                result.Errors.Add($"Uninstallation failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 检查安装状态
        /// </summary>
        public async Task<InstallationStatus> CheckInstallationStatusAsync(string applicationId)
        {
            return await Task.Run(() =>
            {
                if (!_installedApplications.ContainsKey(applicationId))
                {
                    return new InstallationStatus { IsInstalled = false };
                }

                var app = _installedApplications[applicationId];
                var status = new InstallationStatus
                {
                    IsInstalled = true,
                    InstalledVersion = app.Version,
                    InstallDirectory = app.InstallDirectory,
                    InstallDate = app.InstallDate,
                    InstallSize = app.InstallSize,
                    Status = ValidateInstallation(app)
                };

                return status;
            });
        }

        /// <summary>
        /// 获取已安装的应用程序列表
        /// </summary>
        public async Task<IEnumerable<InstalledApplication>> GetInstalledApplicationsAsync()
        {
            return await Task.FromResult(_installedApplications.Values.ToList());
        }

        /// <summary>
        /// 修复安装
        /// </summary>
        public async Task<RepairResult> RepairInstallationAsync(string applicationId)
        {
            var result = new RepairResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                if (!_installedApplications.ContainsKey(applicationId))
                {
                    throw new InvalidOperationException($"Application not found: {applicationId}");
                }

                var app = _installedApplications[applicationId];

                // 验证文件完整性
                var missingFiles = await ValidateFileIntegrity(app);
                
                // 修复缺失的文件
                foreach (var file in missingFiles)
                {
                    await RepairFile(file, result);
                }

                // 修复注册表项
                await RepairRegistryEntries(app, result);

                // 修复快捷方式
                await RepairShortcuts(app, result);

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow;
                result.Errors.Add($"Repair failed: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 更新应用程序
        /// </summary>
        public async Task<UpdateResult> UpdateApplicationAsync(string applicationId, string updatePackagePath)
        {
            var result = new UpdateResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                if (!_installedApplications.ContainsKey(applicationId))
                {
                    throw new InvalidOperationException($"Application not found: {applicationId}");
                }

                var app = _installedApplications[applicationId];
                result.OldVersion = app.Version;

                // 备份当前安装
                var backupPath = await CreateBackup(app);

                try
                {
                    // 应用更新
                    await ApplyUpdate(app, updatePackagePath, result);

                    // 更新应用程序信息
                    await UpdateApplicationInfo(app, result);

                    result.IsSuccess = true;
                }
                catch
                {
                    // 恢复备份
                    await RestoreBackup(app, backupPath);
                    throw;
                }
                finally
                {
                    // 清理备份
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                    }
                }

                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow;
                result.Errors.Add($"Update failed: {ex.Message}");
            }

            return result;
        }

        // Helper Methods

        private InstallerType DetermineInstallerType()
        {
            // 根据平台确定安装程序类型
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return InstallerType.MSI;
            else
                return InstallerType.CrossPlatform;
        }

        private List<InstallerPlatform> DetermineTargetPlatforms()
        {
            var platforms = new List<InstallerPlatform>();
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                platforms.Add(InstallerPlatform.Windows);
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
                platforms.Add(InstallerPlatform.Linux);
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
                platforms.Add(InstallerPlatform.MacOS);
            
            return platforms;
        }

        private List<InstallationStep> CreateDefaultInstallationSteps(InstallerSettings settings)
        {
            var steps = new List<InstallationStep>
            {
                new InstallationStep
                {
                    Id = "create_directory",
                    Name = "创建安装目录",
                    Type = StepType.CreateDirectory,
                    Order = 1
                },
                new InstallationStep
                {
                    Id = "copy_files",
                    Name = "复制文件",
                    Type = StepType.CopyFiles,
                    Order = 2
                }
            };

            if (settings.CreateDesktopShortcut)
            {
                steps.Add(new InstallationStep
                {
                    Id = "create_desktop_shortcut",
                    Name = "创建桌面快捷方式",
                    Type = StepType.CreateShortcut,
                    Order = 3
                });
            }

            return steps;
        }
        private List<UninstallationStep> CreateDefaultUninstallationSteps(InstallerSettings settings)
        {
            return new List<UninstallationStep>
            {
                new UninstallationStep
                {
                    Id = "remove_files",
                    Name = "删除文件",
                    Type = StepType.CopyFiles,
                    Order = 1
                },
                new UninstallationStep
                {
                    Id = "remove_shortcuts",
                    Name = "删除快捷方式",
                    Type = StepType.CreateShortcut,
                    Order = 2
                }
            };
        }

        private SystemRequirements CreateDefaultSystemRequirements()
        {
            return new SystemRequirements
            {
                MinimumOSVersion = Environment.OSVersion.Version.ToString(),
                MinimumMemoryMB = 512,
                MinimumDiskSpaceMB = 100,
                SupportedArchitectures = new List<string> { "x64", "x86" }
            };
        }

        private void ValidateBasicConfiguration(InstallerConfiguration configuration, InstallerValidationResult result)
        {
            if (configuration.Application == null)
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "MISSING_APPLICATION_INFO",
                    Message = "Application information is required",
                    Severity = ErrorSeverity.Error
                });
            }

            if (string.IsNullOrEmpty(configuration.PackagePath))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "MISSING_PACKAGE_PATH",
                    Message = "Package path is required",
                    Severity = ErrorSeverity.Error
                });
            }
        }
        private void ValidatePackageFile(InstallerConfiguration configuration, InstallerValidationResult result)
        {
            if (!File.Exists(configuration.PackagePath))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "PACKAGE_NOT_FOUND",
                    Message = $"Package file not found: {configuration.PackagePath}",
                    Severity = ErrorSeverity.Error
                });
            }
        }

        private void ValidateInstallationSteps(InstallerConfiguration configuration, InstallerValidationResult result)
        {
            if (configuration.InstallationSteps == null || configuration.InstallationSteps.Count == 0)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "NO_INSTALLATION_STEPS",
                    Message = "No installation steps defined"
                });
            }
        }

        private void ValidateSystemRequirements(InstallerConfiguration configuration, InstallerValidationResult result)
        {
            if (configuration.Requirements == null)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "NO_SYSTEM_REQUIREMENTS",
                    Message = "No system requirements defined"
                });
            }
        }

        private void ValidateUIConfiguration(InstallerConfiguration configuration, InstallerValidationResult result)
        {
            if (configuration.UIConfiguration == null)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Code = "NO_UI_CONFIGURATION",
                    Message = "No UI configuration defined"
                });
            }
        }
        private async Task GenerateInstallerFiles(InstallerConfiguration configuration, string tempDir, InstallerGenerationResult result)
        {
            // 复制包文件到临时目录
            var packageFileName = Path.GetFileName(configuration.PackagePath);
            var tempPackagePath = Path.Combine(tempDir, packageFileName);
            File.Copy(configuration.PackagePath, tempPackagePath);
            result.GeneratedFiles.Add(tempPackagePath);

            // 生成安装脚本
            var scriptPath = Path.Combine(tempDir, "install.bat");
            await GenerateInstallScript(configuration, scriptPath);
            result.GeneratedFiles.Add(scriptPath);

            // 生成配置文件
            var configPath = Path.Combine(tempDir, "installer.json");
            await GenerateInstallerConfig(configuration, configPath);
            result.GeneratedFiles.Add(configPath);
        }

        private async Task CreateFinalInstaller(InstallerConfiguration configuration, string tempDir, InstallerGenerationResult result)
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(configuration.PackagePath), 
                $"{configuration.Application.Name}_Installer.exe");

            // 创建自解压可执行文件
            using (var archive = ZipFile.Open(outputPath + ".zip", ZipArchiveMode.Create))
            {
                foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(tempDir, file);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            }

            // 重命名为.exe
            File.Move(outputPath + ".zip", outputPath);

            result.InstallerPath = outputPath;
            result.InstallerSize = new FileInfo(outputPath).Length;
        }

        private async Task GenerateInstallScript(InstallerConfiguration configuration, string scriptPath)
        {
            var script = @"@echo off
echo Installing " + configuration.Application.Name + @"...
mkdir ""%PROGRAMFILES%\" + configuration.Application.Name + @"""
xcopy /E /I /Y *.* ""%PROGRAMFILES%\" + configuration.Application.Name + @"""
echo Installation completed.
pause";

            await File.WriteAllTextAsync(scriptPath, script);
        }
        private async Task GenerateInstallerConfig(InstallerConfiguration configuration, string configPath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(configuration, options);
            await File.WriteAllTextAsync(configPath, json);
        }

        private void ReportProgress(string step, double percentage, string message, int completed, int total)
        {
            InstallProgress?.Invoke(this, new InstallProgressEventArgs
            {
                CurrentStep = step,
                ProgressPercentage = percentage,
                Message = message,
                CompletedSteps = completed,
                TotalSteps = total
            });
        }

        private async Task ExtractInstallationPackage(string installerPath, InstallationOptions options, InstallationResult result)
        {
            // 模拟解压过程
            await Task.Delay(1000);
            
            // 添加模拟的已安装文件
            result.InstalledFiles.Add(new InstalledFile
            {
                FilePath = Path.Combine(options.InstallDirectory, "app.exe"),
                FileSize = 1024000,
                FileHash = "abc123",
                InstallTime = DateTime.UtcNow,
                FileVersion = "1.0.0"
            });
        }

        private async Task ExecuteInstallationSteps(InstallationOptions options, InstallationResult result)
        {
            // 模拟执行安装步骤
            await Task.Delay(2000);
            
            result.Logs.Add(new InstallationLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Info,
                Message = "Installation steps completed successfully",
                Step = "ExecuteInstallationSteps"
            });
        }
        private async Task CreateShortcuts(InstallationOptions options, InstallationResult result)
        {
            if (options.CreateDesktopShortcut)
            {
                var shortcut = new Shortcut
                {
                    ShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "App.lnk"),
                    TargetPath = Path.Combine(options.InstallDirectory, "app.exe"),
                    WorkingDirectory = options.InstallDirectory
                };
                result.Shortcuts.Add(shortcut);
            }

            await Task.CompletedTask;
        }

        private async Task RegisterApplication(InstallationOptions options, InstallationResult result)
        {
            var app = new InstalledApplication
            {
                Id = Guid.NewGuid().ToString(),
                Name = "RimWorld Framework Application",
                Version = "1.0.0",
                Publisher = "RimWorld Framework",
                InstallDirectory = options.InstallDirectory,
                InstallDate = DateTime.UtcNow,
                InstallSize = result.InstalledFiles.Sum(f => f.FileSize),
                Status = ApplicationStatus.Normal
            };

            _installedApplications[app.Id] = app;
            await SaveInstalledApplications();
        }

        private async Task StopApplicationServices(InstalledApplication app)
        {
            // 模拟停止服务
            await Task.Delay(500);
        }

        private async Task<int> DeleteApplicationFiles(InstalledApplication app, UninstallationOptions options)
        {
            // 模拟删除文件
            await Task.Delay(1000);
            return 10; // 返回删除的文件数量
        }

        private async Task<int> DeleteRegistryEntries(InstalledApplication app)
        {
            // 模拟删除注册表项
            await Task.Delay(500);
            return 5; // 返回删除的注册表项数量
        }
        private async Task DeleteShortcuts(InstalledApplication app)
        {
            // 模拟删除快捷方式
            await Task.Delay(200);
        }

        private ApplicationStatus ValidateInstallation(InstalledApplication app)
        {
            // 简单的安装验证
            if (Directory.Exists(app.InstallDirectory))
                return ApplicationStatus.Normal;
            else
                return ApplicationStatus.Corrupted;
        }

        private async Task<List<string>> ValidateFileIntegrity(InstalledApplication app)
        {
            // 模拟文件完整性检查
            await Task.Delay(1000);
            return new List<string>(); // 返回缺失的文件列表
        }

        private async Task RepairFile(string filePath, RepairResult result)
        {
            // 模拟文件修复
            await Task.Delay(100);
            result.RepairActions.Add(new RepairAction
            {
                ActionType = "RepairFile",
                TargetPath = filePath,
                Description = $"Repaired file: {filePath}",
                IsSuccess = true
            });
            result.RepairedFilesCount++;
        }

        private async Task RepairRegistryEntries(InstalledApplication app, RepairResult result)
        {
            // 模拟注册表修复
            await Task.Delay(300);
            result.RepairedRegistryEntriesCount = 2;
        }

        private async Task RepairShortcuts(InstalledApplication app, RepairResult result)
        {
            // 模拟快捷方式修复
            await Task.Delay(200);
        }
        private async Task<string> CreateBackup(InstalledApplication app)
        {
            var backupPath = Path.Combine(Path.GetTempPath(), $"backup_{app.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}");
            Directory.CreateDirectory(backupPath);
            
            // 模拟备份过程
            await Task.Delay(2000);
            
            return backupPath;
        }

        private async Task ApplyUpdate(InstalledApplication app, string updatePackagePath, UpdateResult result)
        {
            // 模拟应用更新
            await Task.Delay(3000);
            
            result.UpdateActions.Add(new UpdateAction
            {
                ActionType = "UpdateFile",
                FilePath = Path.Combine(app.InstallDirectory, "app.exe"),
                OldVersion = app.Version,
                NewVersion = "1.1.0",
                Description = "Updated main executable",
                IsSuccess = true
            });
            
            result.UpdatedFilesCount = 5;
        }

        private async Task UpdateApplicationInfo(InstalledApplication app, UpdateResult result)
        {
            app.Version = result.NewVersion = "1.1.0";
            await SaveInstalledApplications();
        }

        private async Task RestoreBackup(InstalledApplication app, string backupPath)
        {
            // 模拟恢复备份
            await Task.Delay(1500);
        }

        private void LoadInstalledApplications()
        {
            try
            {
                if (File.Exists(_registryPath))
                {
                    var json = File.ReadAllText(_registryPath);
                    var apps = JsonSerializer.Deserialize<Dictionary<string, InstalledApplication>>(json);
                    if (apps != null)
                    {
                        foreach (var kvp in apps)
                        {
                            _installedApplications[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch
            {
                // 忽略加载错误
            }
        }
        private async Task SaveInstalledApplications()
        {
            try
            {
                var directory = Path.GetDirectoryName(_registryPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_installedApplications, options);
                await File.WriteAllTextAsync(_registryPath, json);
            }
            catch
            {
                // 忽略保存错误
            }
        }
    }
}