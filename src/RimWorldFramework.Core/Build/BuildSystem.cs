using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Build
{
    /// <summary>
    /// 构建系统实现
    /// 负责自动化构建和打包流程，依赖项收集和验证
    /// </summary>
    public class BuildSystem : IBuildSystem
    {
        private readonly ConcurrentDictionary<string, BuildRecord> _buildHistory;
        private readonly ConcurrentDictionary<string, BuildStatistics> _buildStatistics;
        private readonly object _lockObject = new object();

        public event EventHandler<BuildProgressEventArgs> BuildProgress;
        public event EventHandler<BuildCompletedEventArgs> BuildCompleted;
        public event EventHandler<BuildErrorEventArgs> BuildError;

        /// <summary>
        /// 构造函数
        /// </summary>
        public BuildSystem()
        {
            _buildHistory = new ConcurrentDictionary<string, BuildRecord>();
            _buildStatistics = new ConcurrentDictionary<string, BuildStatistics>();
        }

        /// <summary>
        /// 创建构建配置
        /// </summary>
        public BuildConfiguration CreateBuildConfiguration(string projectPath, BuildSettings settings)
        {
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));

            if (!Directory.Exists(projectPath))
                throw new DirectoryNotFoundException($"Project directory not found: {projectPath}");

            return new BuildConfiguration
            {
                ProjectPath = projectPath,
                OutputDirectory = Path.Combine(projectPath, "bin", settings.EnableOptimization ? "Release" : "Debug"),
                Settings = settings ?? new BuildSettings(),
                IncludePatterns = new List<string> { "**/*.cs", "**/*.csproj", "**/*.json" },
                ExcludePatterns = new List<string> { "**/bin/**", "**/obj/**", "**/.git/**" }
            };
        }
        /// <summary>
        /// 验证构建配置
        /// </summary>
        public async Task<BuildValidationResult> ValidateBuildConfigurationAsync(BuildConfiguration configuration)
        {
            var result = new BuildValidationResult { IsValid = true };

            try
            {
                // 验证项目路径
                if (string.IsNullOrEmpty(configuration.ProjectPath))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "BUILD001",
                        Message = "Project path is required",
                        Property = nameof(configuration.ProjectPath),
                        Severity = ErrorSeverity.Error
                    });
                }
                else if (!Directory.Exists(configuration.ProjectPath))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "BUILD002",
                        Message = $"Project directory does not exist: {configuration.ProjectPath}",
                        Property = nameof(configuration.ProjectPath),
                        Severity = ErrorSeverity.Error
                    });
                }

                // 验证输出目录
                if (string.IsNullOrEmpty(configuration.OutputDirectory))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "BUILD101",
                        Message = "Output directory not specified, using default",
                        Property = nameof(configuration.OutputDirectory)
                    });
                }

                // 验证构建设置
                if (configuration.Settings.MaxParallelism <= 0)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "BUILD003",
                        Message = "Max parallelism must be greater than 0",
                        Property = "Settings.MaxParallelism",
                        Severity = ErrorSeverity.Error
                    });
                }

                result.IsValid = result.Errors.Count == 0;
                result.Details = $"Validation completed with {result.Errors.Count} errors and {result.Warnings.Count} warnings";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Code = "BUILD999",
                    Message = $"Validation failed: {ex.Message}",
                    Severity = ErrorSeverity.Fatal
                });
            }

            return result;
        }
        /// <summary>
        /// 收集项目依赖项
        /// </summary>
        public async Task<IEnumerable<BuildDependency>> CollectDependenciesAsync(string projectPath)
        {
            var dependencies = new List<BuildDependency>();

            try
            {
                // 收集程序集依赖项
                var assemblyFiles = Directory.GetFiles(projectPath, "*.dll", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"));

                foreach (var assemblyFile in assemblyFiles)
                {
                    var fileInfo = new FileInfo(assemblyFile);
                    dependencies.Add(new BuildDependency
                    {
                        Name = Path.GetFileNameWithoutExtension(assemblyFile),
                        Version = GetAssemblyVersion(assemblyFile),
                        Type = DependencyType.Assembly,
                        SourcePath = assemblyFile,
                        TargetPath = Path.Combine("lib", fileInfo.Name),
                        FileSize = fileInfo.Length,
                        FileHash = await CalculateFileHashAsync(assemblyFile)
                    });
                }

                // 收集配置文件
                var configFiles = Directory.GetFiles(projectPath, "*.json", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(projectPath, "*.xml", SearchOption.AllDirectories))
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"));

                foreach (var configFile in configFiles)
                {
                    var fileInfo = new FileInfo(configFile);
                    dependencies.Add(new BuildDependency
                    {
                        Name = Path.GetFileName(configFile),
                        Version = "1.0.0",
                        Type = DependencyType.Configuration,
                        SourcePath = configFile,
                        TargetPath = Path.GetFileName(configFile),
                        FileSize = fileInfo.Length,
                        FileHash = await CalculateFileHashAsync(configFile)
                    });
                }

                // 收集资源文件
                var resourceExtensions = new[] { ".png", ".jpg", ".wav", ".mp3", ".txt" };
                var resourceFiles = resourceExtensions
                    .SelectMany(ext => Directory.GetFiles(projectPath, $"*{ext}", SearchOption.AllDirectories))
                    .Where(f => !f.Contains("bin") && !f.Contains("obj"));

                foreach (var resourceFile in resourceFiles)
                {
                    var fileInfo = new FileInfo(resourceFile);
                    dependencies.Add(new BuildDependency
                    {
                        Name = Path.GetFileName(resourceFile),
                        Version = "1.0.0",
                        Type = DependencyType.Resource,
                        SourcePath = resourceFile,
                        TargetPath = Path.Combine("resources", Path.GetFileName(resourceFile)),
                        FileSize = fileInfo.Length,
                        FileHash = await CalculateFileHashAsync(resourceFile)
                    });
                }
            }
            catch (Exception ex)
            {
                OnBuildError(new BuildErrorEventArgs
                {
                    ErrorMessage = $"Failed to collect dependencies: {ex.Message}",
                    Exception = ex,
                    Step = "Dependency Collection",
                    ErrorCode = "BUILD004"
                });
            }

            return dependencies;
        }
        /// <summary>
        /// 验证依赖项
        /// </summary>
        public async Task<DependencyValidationResult> ValidateDependenciesAsync(IEnumerable<BuildDependency> dependencies)
        {
            var result = new DependencyValidationResult { IsValid = true };

            try
            {
                foreach (var dependency in dependencies)
                {
                    // 检查文件是否存在
                    if (!File.Exists(dependency.SourcePath))
                    {
                        result.MissingDependencies.Add(dependency);
                        continue;
                    }

                    // 验证文件哈希
                    var currentHash = await CalculateFileHashAsync(dependency.SourcePath);
                    if (!string.IsNullOrEmpty(dependency.FileHash) && currentHash != dependency.FileHash)
                    {
                        result.CorruptedDependencies.Add(dependency);
                    }
                }

                result.IsValid = result.MissingDependencies.Count == 0 && result.CorruptedDependencies.Count == 0;
                result.Details = $"Validated {dependencies.Count()} dependencies. " +
                               $"Missing: {result.MissingDependencies.Count}, " +
                               $"Corrupted: {result.CorruptedDependencies.Count}";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Details = $"Dependency validation failed: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 执行构建
        /// </summary>
        public async Task<BuildResult> BuildAsync(BuildConfiguration configuration)
        {
            var result = new BuildResult
            {
                StartTime = DateTime.UtcNow,
                Configuration = configuration
            };

            try
            {
                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Initializing Build",
                    ProgressPercentage = 0,
                    Message = "Starting build process",
                    CompletedTasks = 0,
                    TotalTasks = 5
                });

                // 1. 验证配置
                var validationResult = await ValidateBuildConfigurationAsync(configuration);
                if (!validationResult.IsValid)
                {
                    result.IsSuccess = false;
                    result.Errors.AddRange(validationResult.Errors.Select(e => new BuildError
                    {
                        Code = e.Code,
                        Message = e.Message,
                        Severity = e.Severity
                    }));
                    return result;
                }

                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Configuration Validated",
                    ProgressPercentage = 20,
                    Message = "Build configuration is valid",
                    CompletedTasks = 1,
                    TotalTasks = 5
                });

                // 2. 收集依赖项
                var dependencies = await CollectDependenciesAsync(configuration.ProjectPath);
                var dependencyValidation = await ValidateDependenciesAsync(dependencies);

                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Dependencies Collected",
                    ProgressPercentage = 40,
                    Message = $"Collected {dependencies.Count()} dependencies",
                    CompletedTasks = 2,
                    TotalTasks = 5
                });

                // 3. 准备输出目录
                if (configuration.Settings.CleanOutputDirectory && Directory.Exists(configuration.OutputDirectory))
                {
                    Directory.Delete(configuration.OutputDirectory, true);
                }
                Directory.CreateDirectory(configuration.OutputDirectory);

                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Output Directory Prepared",
                    ProgressPercentage = 60,
                    Message = "Output directory is ready",
                    CompletedTasks = 3,
                    TotalTasks = 5
                });

                // 4. 执行构建（模拟）
                await SimulateBuildProcessAsync(configuration, result);

                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Build Completed",
                    ProgressPercentage = 80,
                    Message = "Build process completed successfully",
                    CompletedTasks = 4,
                    TotalTasks = 5
                });

                // 5. 生成构建产物
                await GenerateBuildArtifactsAsync(configuration, result);

                result.IsSuccess = true;
                result.EndTime = DateTime.UtcNow;

                OnBuildProgress(new BuildProgressEventArgs
                {
                    CurrentStep = "Artifacts Generated",
                    ProgressPercentage = 100,
                    Message = "All build artifacts generated",
                    CompletedTasks = 5,
                    TotalTasks = 5
                });

                // 记录构建历史
                RecordBuildHistory(result);

                OnBuildCompleted(new BuildCompletedEventArgs
                {
                    Result = result,
                    Duration = result.Duration,
                    OutputFiles = result.Artifacts.Select(a => a.FilePath).ToList()
                });
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.EndTime = DateTime.UtcNow;
                result.Errors.Add(new BuildError
                {
                    Code = "BUILD999",
                    Message = ex.Message,
                    Severity = ErrorSeverity.Fatal
                });

                OnBuildError(new BuildErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    Exception = ex,
                    Step = "Build Execution",
                    ErrorCode = "BUILD999"
                });
            }

            return result;
        }
        /// <summary>
        /// 清理构建输出
        /// </summary>
        public async Task<CleanResult> CleanAsync(BuildConfiguration configuration)
        {
            var result = new CleanResult();
            var startTime = DateTime.UtcNow;

            try
            {
                if (Directory.Exists(configuration.OutputDirectory))
                {
                    var files = Directory.GetFiles(configuration.OutputDirectory, "*", SearchOption.AllDirectories);
                    var directories = Directory.GetDirectories(configuration.OutputDirectory, "*", SearchOption.AllDirectories);

                    long totalSize = 0;
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                        File.Delete(file);
                    }

                    foreach (var dir in directories.OrderByDescending(d => d.Length))
                    {
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                        }
                    }

                    result.DeletedFilesCount = files.Length;
                    result.DeletedDirectoriesCount = directories.Length;
                    result.FreedSpaceBytes = totalSize;
                    result.IsSuccess = true;
                }
                else
                {
                    result.IsSuccess = true; // 目录不存在也算成功
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Clean failed: {ex.Message}");
            }

            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }

        /// <summary>
        /// 打包构建输出
        /// </summary>
        public async Task<PackageResult> PackageAsync(BuildConfiguration configuration, PackageSettings packageSettings)
        {
            var result = new PackageResult();
            var startTime = DateTime.UtcNow;

            try
            {
                var outputPath = packageSettings.OutputPath ?? 
                    Path.Combine(configuration.OutputDirectory, $"{packageSettings.PackageName}_{packageSettings.Version}.zip");

                switch (packageSettings.Format)
                {
                    case PackageFormat.Zip:
                        await CreateZipPackageAsync(configuration.OutputDirectory, outputPath, packageSettings, result);
                        break;
                    case PackageFormat.Directory:
                        await CreateDirectoryPackageAsync(configuration.OutputDirectory, outputPath, packageSettings, result);
                        break;
                    default:
                        throw new NotSupportedException($"Package format {packageSettings.Format} is not supported");
                }

                result.PackageFilePath = outputPath;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add($"Packaging failed: {ex.Message}");
            }

            result.Duration = DateTime.UtcNow - startTime;
            return result;
        }

        /// <summary>
        /// 获取构建历史
        /// </summary>
        public IEnumerable<BuildRecord> GetBuildHistory(string projectPath)
        {
            return _buildHistory.Values
                .Where(record => record.Configuration?.Contains(projectPath) == true)
                .OrderByDescending(record => record.BuildTime)
                .ToList();
        }

        /// <summary>
        /// 获取构建统计信息
        /// </summary>
        public BuildStatistics GetBuildStatistics(string projectPath)
        {
            return _buildStatistics.GetOrAdd(projectPath, _ => new BuildStatistics());
        }
        #region 私有辅助方法

        private string GetAssemblyVersion(string assemblyPath)
        {
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
                return versionInfo.FileVersion ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await Task.Run(() => sha256.ComputeHash(stream));
            return Convert.ToBase64String(hash);
        }

        private async Task SimulateBuildProcessAsync(BuildConfiguration configuration, BuildResult result)
        {
            // 模拟构建过程
            await Task.Delay(1000); // 模拟编译时间

            result.Logs.Add(new BuildLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Info,
                Message = "Build process started",
                Source = "BuildSystem",
                Step = "Compilation"
            });

            // 模拟一些构建指标
            result.Metrics.CompiledFilesCount = 25;
            result.Metrics.GeneratedLinesOfCode = 5000;
            result.Metrics.PeakMemoryUsageMB = 128.5;
            result.Metrics.CpuTimeSeconds = 2.5;
            result.Metrics.ParallelismLevel = configuration.Settings.MaxParallelism;

            result.Logs.Add(new BuildLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Info,
                Message = "Build process completed successfully",
                Source = "BuildSystem",
                Step = "Compilation"
            });
        }

        private async Task GenerateBuildArtifactsAsync(BuildConfiguration configuration, BuildResult result)
        {
            // 生成主要可执行文件
            var exePath = Path.Combine(configuration.OutputDirectory, "RimWorldFramework.exe");
            await File.WriteAllTextAsync(exePath, "Mock executable content");

            result.Artifacts.Add(new BuildArtifact
            {
                FilePath = exePath,
                FileName = "RimWorldFramework.exe",
                FileSize = new FileInfo(exePath).Length,
                FileHash = await CalculateFileHashAsync(exePath),
                Type = ArtifactType.Executable,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = true
            });

            // 生成库文件
            var libPath = Path.Combine(configuration.OutputDirectory, "RimWorldFramework.Core.dll");
            await File.WriteAllTextAsync(libPath, "Mock library content");

            result.Artifacts.Add(new BuildArtifact
            {
                FilePath = libPath,
                FileName = "RimWorldFramework.Core.dll",
                FileSize = new FileInfo(libPath).Length,
                FileHash = await CalculateFileHashAsync(libPath),
                Type = ArtifactType.Library,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = false
            });
        }

        private async Task CreateZipPackageAsync(string sourceDirectory, string outputPath, PackageSettings settings, PackageResult result)
        {
            var originalSize = 0L;
            var fileCount = 0;

            using (var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var entry = archive.CreateEntry(relativePath);
                    
                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(file);
                    await fileStream.CopyToAsync(entryStream);
                    
                    originalSize += new FileInfo(file).Length;
                    fileCount++;
                }
            }

            var packageInfo = new FileInfo(outputPath);
            result.PackageSize = packageInfo.Length;
            result.FileCount = fileCount;
            result.CompressionRatio = originalSize > 0 ? (double)result.PackageSize / originalSize : 1.0;
            result.PackageHash = await CalculateFileHashAsync(outputPath);
        }

        private async Task CreateDirectoryPackageAsync(string sourceDirectory, string outputPath, PackageSettings settings, PackageResult result)
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            Directory.CreateDirectory(outputPath);

            var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
            var totalSize = 0L;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, file);
                var targetPath = Path.Combine(outputPath, relativePath);
                
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(file, targetPath);
                
                totalSize += new FileInfo(file).Length;
            }

            result.PackageSize = totalSize;
            result.FileCount = files.Length;
            result.CompressionRatio = 1.0; // 无压缩
        }

        private void RecordBuildHistory(BuildResult result)
        {
            var record = new BuildRecord
            {
                BuildId = result.BuildId,
                BuildTime = result.StartTime,
                IsSuccess = result.IsSuccess,
                Duration = result.Duration,
                Configuration = result.Configuration?.Mode.ToString(),
                Platform = result.Configuration?.Platform ?? TargetPlatform.Windows,
                ErrorCount = result.Errors.Count,
                WarningCount = result.Warnings.Count,
                OutputSize = result.Artifacts.Sum(a => a.FileSize)
            };

            _buildHistory.TryAdd(result.BuildId, record);

            // 更新统计信息
            var projectPath = result.Configuration?.ProjectPath ?? "Unknown";
            var stats = _buildStatistics.GetOrAdd(projectPath, _ => new BuildStatistics());
            
            lock (_lockObject)
            {
                stats.TotalBuilds++;
                if (result.IsSuccess)
                    stats.SuccessfulBuilds++;
                else
                    stats.FailedBuilds++;

                stats.LastBuildTime = result.StartTime;
                
                // 更新平均构建时长
                if (stats.TotalBuilds == 1)
                {
                    stats.AverageBuildDuration = result.Duration;
                    stats.FastestBuildDuration = result.Duration;
                    stats.SlowestBuildDuration = result.Duration;
                }
                else
                {
                    var totalTicks = stats.AverageBuildDuration.Ticks * (stats.TotalBuilds - 1) + result.Duration.Ticks;
                    stats.AverageBuildDuration = new TimeSpan(totalTicks / stats.TotalBuilds);
                    
                    if (result.Duration < stats.FastestBuildDuration)
                        stats.FastestBuildDuration = result.Duration;
                    
                    if (result.Duration > stats.SlowestBuildDuration)
                        stats.SlowestBuildDuration = result.Duration;
                }
            }
        }

        private void OnBuildProgress(BuildProgressEventArgs args)
        {
            BuildProgress?.Invoke(this, args);
        }

        private void OnBuildCompleted(BuildCompletedEventArgs args)
        {
            BuildCompleted?.Invoke(this, args);
        }

        private void OnBuildError(BuildErrorEventArgs args)
        {
            BuildError?.Invoke(this, args);
        }

        #endregion
    }
}