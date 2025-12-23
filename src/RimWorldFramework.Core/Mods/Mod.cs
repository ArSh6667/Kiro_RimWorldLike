using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 模组实现
    /// </summary>
    public class Mod : IMod
    {
        private readonly ModManifest _manifest;
        private ModStatus _status;

        public string Id => _manifest.Id;
        public string Name => _manifest.Name;
        public Version Version { get; }
        public string Description => _manifest.Description;
        public string Author => _manifest.Author;
        public string Path { get; }
        public ModStatus Status => _status;

        public IEnumerable<ModDependency> Dependencies { get; }
        public List<Assembly> LoadedAssemblies { get; }
        public ModManifest Manifest => _manifest;

        public Mod(ModManifest manifest, string path)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            
            Version = System.Version.Parse(manifest.Version);
            Dependencies = manifest.Dependencies?.Select(d => new ModDependency
            {
                ModId = d.ModId,
                MinVersion = !string.IsNullOrEmpty(d.MinVersion) ? System.Version.Parse(d.MinVersion) : null,
                MaxVersion = !string.IsNullOrEmpty(d.MaxVersion) ? System.Version.Parse(d.MaxVersion) : null,
                IsOptional = d.Optional
            }).ToList() ?? new List<ModDependency>();

            LoadedAssemblies = new List<Assembly>();
            _status = ModStatus.NotLoaded;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _status = ModStatus.Loading;

                // 执行初始化入口点
                await ExecuteEntryPointsAsync("Initialize");

                _status = ModStatus.Loaded;
            }
            catch (Exception ex)
            {
                _status = ModStatus.Error;
                throw new ModInitializationException($"Failed to initialize mod {Id}: {ex.Message}", ex);
            }
        }

        public async Task StartAsync()
        {
            try
            {
                if (_status != ModStatus.Loaded)
                {
                    throw new InvalidOperationException($"Cannot start mod {Id} in status {_status}");
                }

                // 执行启动入口点
                await ExecuteEntryPointsAsync("Start");

                _status = ModStatus.Running;
            }
            catch (Exception ex)
            {
                _status = ModStatus.Error;
                throw new ModStartException($"Failed to start mod {Id}: {ex.Message}", ex);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_status != ModStatus.Running)
                    return;

                // 执行停止入口点
                await ExecuteEntryPointsAsync("Stop");

                _status = ModStatus.Stopped;
            }
            catch (Exception ex)
            {
                _status = ModStatus.Error;
                throw new ModStopException($"Failed to stop mod {Id}: {ex.Message}", ex);
            }
        }

        public async Task CleanupAsync()
        {
            try
            {
                // 执行清理入口点
                await ExecuteEntryPointsAsync("Cleanup");

                // 清理加载的程序集
                LoadedAssemblies.Clear();

                _status = ModStatus.NotLoaded;
            }
            catch (Exception ex)
            {
                _status = ModStatus.Error;
                throw new ModCleanupException($"Failed to cleanup mod {Id}: {ex.Message}", ex);
            }
        }
        private async Task ExecuteEntryPointsAsync(string entryPointType)
        {
            var entryPoints = _manifest.EntryPoints
                .Where(ep => ep.Type.Equals(entryPointType, StringComparison.OrdinalIgnoreCase))
                .OrderBy(ep => ep.LoadOrder);

            foreach (var entryPoint in entryPoints)
            {
                await ExecuteEntryPointAsync(entryPoint);
            }
        }

        private async Task ExecuteEntryPointAsync(ModEntryPoint entryPoint)
        {
            try
            {
                // 查找对应的程序集
                var assembly = LoadedAssemblies.FirstOrDefault(a => 
                    a.Location.EndsWith(entryPoint.Assembly, StringComparison.OrdinalIgnoreCase));

                if (assembly == null)
                {
                    throw new ModExecutionException($"Assembly not found: {entryPoint.Assembly}");
                }

                // 查找类型
                var type = assembly.GetType(entryPoint.ClassName);
                if (type == null)
                {
                    throw new ModExecutionException($"Class not found: {entryPoint.ClassName}");
                }

                // 查找方法
                var method = type.GetMethod(entryPoint.MethodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    throw new ModExecutionException($"Method not found: {entryPoint.MethodName}");
                }

                // 执行方法
                var result = method.Invoke(null, null);

                // 如果返回Task，等待完成
                if (result is Task task)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                throw new ModExecutionException($"Failed to execute entry point {entryPoint.ClassName}.{entryPoint.MethodName}: {ex.Message}", ex);
            }
        }

        public override string ToString()
        {
            return $"{Name} v{Version} ({Id})";
        }
    }

    /// <summary>
    /// 模组初始化异常
    /// </summary>
    public class ModInitializationException : Exception
    {
        public ModInitializationException(string message) : base(message) { }
        public ModInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 模组启动异常
    /// </summary>
    public class ModStartException : Exception
    {
        public ModStartException(string message) : base(message) { }
        public ModStartException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 模组停止异常
    /// </summary>
    public class ModStopException : Exception
    {
        public ModStopException(string message) : base(message) { }
        public ModStopException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 模组清理异常
    /// </summary>
    public class ModCleanupException : Exception
    {
        public ModCleanupException(string message) : base(message) { }
        public ModCleanupException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// 模组执行异常
    /// </summary>
    public class ModExecutionException : Exception
    {
        public ModExecutionException(string message) : base(message) { }
        public ModExecutionException(string message, Exception innerException) : base(message, innerException) { }
    }
}