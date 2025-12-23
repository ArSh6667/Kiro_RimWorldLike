using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace RimWorldFramework.Core.Mods
{
    /// <summary>
    /// 默认模组安全管理器实现
    /// </summary>
    public class DefaultModSecurityManager : IModSecurityManager
    {
        private readonly Dictionary<string, AppDomain> _modAppDomains;

        public DefaultModSecurityManager()
        {
            _modAppDomains = new Dictionary<string, AppDomain>();
        }

        public async Task<Assembly> LoadAssemblySecurelyAsync(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
            }

            // 首先检查程序集安全性
            var securityResult = await CheckAssemblySecurityAsync(assemblyPath);
            if (!securityResult.IsSecure)
            {
                throw new SecurityException($"Assembly {assemblyPath} failed security check: {securityResult.Details}");
            }

            try
            {
                // 在当前应用程序域中加载程序集
                // 注意：在实际生产环境中，应该考虑使用独立的应用程序域
                var assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }
            catch (Exception ex)
            {
                throw new ModLoadException($"Failed to load assembly {assemblyPath}: {ex.Message}", ex);
            }
        }

        public async Task<SecurityCheckResult> CheckAssemblySecurityAsync(string assemblyPath)
        {
            var result = new SecurityCheckResult
            {
                IsSecure = true,
                SecurityLevel = SecurityLevel.Safe
            };

            try
            {
                // 加载程序集进行分析（不执行）
                var assemblyBytes = await File.ReadAllBytesAsync(assemblyPath);
                var assembly = Assembly.Load(assemblyBytes);

                // 检查程序集中的类型
                var types = assembly.GetTypes();
                
                foreach (var type in types)
                {
                    await AnalyzeTypeSecurityAsync(type, result);
                }

                // 根据发现的威胁确定安全级别
                DetermineSecurityLevel(result);
            }
            catch (ReflectionTypeLoadException ex)
            {
                result.IsSecure = false;
                result.SecurityLevel = SecurityLevel.Dangerous;
                result.Details = $"Failed to load types from assembly: {ex.Message}";
                
                result.Threats.Add(new SecurityThreat
                {
                    Type = ThreatType.MaliciousPattern,
                    Level = ThreatLevel.High,
                    Description = "Assembly contains unloadable types",
                    Location = assemblyPath
                });
            }
            catch (Exception ex)
            {
                result.IsSecure = false;
                result.SecurityLevel = SecurityLevel.Dangerous;
                result.Details = $"Security analysis failed: {ex.Message}";
            }

            return result;
        }

        public AppDomain CreateSecureAppDomain(string modId)
        {
            if (_modAppDomains.ContainsKey(modId))
            {
                return _modAppDomains[modId];
            }

            try
            {
                // 创建安全的应用程序域设置
                var setup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    DisallowBindingRedirects = true,
                    DisallowCodeDownload = true,
                    DisallowPublisherPolicy = true
                };

                // 创建权限集（受限权限）
                var permissionSet = new PermissionSet(PermissionState.None);
                
                // 添加基本权限
                permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                permissionSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Read, AppDomain.CurrentDomain.BaseDirectory));

                // 创建应用程序域
                var appDomain = AppDomain.CreateDomain(
                    $"ModDomain_{modId}",
                    null,
                    setup,
                    permissionSet);

                _modAppDomains[modId] = appDomain;
                return appDomain;
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Failed to create secure app domain for mod {modId}: {ex.Message}", ex);
            }
        }
        private async Task AnalyzeTypeSecurityAsync(Type type, SecurityCheckResult result)
        {
            // 检查类型名称中的可疑模式
            if (ContainsSuspiciousPatterns(type.Name))
            {
                result.Threats.Add(new SecurityThreat
                {
                    Type = ThreatType.MaliciousPattern,
                    Level = ThreatLevel.Medium,
                    Description = "Suspicious type name detected",
                    Location = type.FullName
                });
            }

            // 检查方法
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                await AnalyzeMethodSecurityAsync(method, result);
            }

            // 检查字段和属性
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var field in fields)
            {
                AnalyzeFieldSecurity(field, result);
            }
        }

        private async Task AnalyzeMethodSecurityAsync(MethodInfo method, SecurityCheckResult result)
        {
            // 检查方法名称中的可疑模式
            if (ContainsSuspiciousPatterns(method.Name))
            {
                result.Threats.Add(new SecurityThreat
                {
                    Type = ThreatType.MaliciousPattern,
                    Level = ThreatLevel.Medium,
                    Description = "Suspicious method name detected",
                    Location = $"{method.DeclaringType?.FullName}.{method.Name}"
                });
            }

            // 检查参数类型
            var parameters = method.GetParameters();
            foreach (var param in parameters)
            {
                if (IsDangerousType(param.ParameterType))
                {
                    result.Threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.UnsafeCode,
                        Level = ThreatLevel.High,
                        Description = "Method uses dangerous parameter types",
                        Location = $"{method.DeclaringType?.FullName}.{method.Name}"
                    });
                }
            }

            // 检查返回类型
            if (IsDangerousType(method.ReturnType))
            {
                result.Threats.Add(new SecurityThreat
                {
                    Type = ThreatType.UnsafeCode,
                    Level = ThreatLevel.High,
                    Description = "Method returns dangerous type",
                    Location = $"{method.DeclaringType?.FullName}.{method.Name}"
                });
            }
        }

        private void AnalyzeFieldSecurity(FieldInfo field, SecurityCheckResult result)
        {
            if (IsDangerousType(field.FieldType))
            {
                result.Threats.Add(new SecurityThreat
                {
                    Type = ThreatType.UnsafeCode,
                    Level = ThreatLevel.Medium,
                    Description = "Field uses dangerous type",
                    Location = $"{field.DeclaringType?.FullName}.{field.Name}"
                });
            }
        }

        private bool ContainsSuspiciousPatterns(string name)
        {
            var suspiciousPatterns = new[]
            {
                "hack", "crack", "exploit", "malware", "virus", "trojan",
                "keylog", "backdoor", "rootkit", "inject", "hijack"
            };

            return suspiciousPatterns.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsDangerousType(Type type)
        {
            if (type == null) return false;

            var dangerousTypes = new[]
            {
                typeof(System.Diagnostics.Process),
                typeof(System.IO.File),
                typeof(System.IO.Directory),
                typeof(System.Net.WebClient),
                typeof(System.Net.Http.HttpClient),
                typeof(Microsoft.Win32.Registry)
            };

            return dangerousTypes.Contains(type) || 
                   type.Namespace?.StartsWith("System.Runtime.InteropServices") == true ||
                   type.Name.Contains("Unsafe");
        }

        private void DetermineSecurityLevel(SecurityCheckResult result)
        {
            var criticalThreats = result.Threats.Count(t => t.Level == ThreatLevel.Critical);
            var highThreats = result.Threats.Count(t => t.Level == ThreatLevel.High);
            var mediumThreats = result.Threats.Count(t => t.Level == ThreatLevel.Medium);

            if (criticalThreats > 0)
            {
                result.IsSecure = false;
                result.SecurityLevel = SecurityLevel.Dangerous;
            }
            else if (highThreats > 2)
            {
                result.IsSecure = false;
                result.SecurityLevel = SecurityLevel.HighRisk;
            }
            else if (highThreats > 0 || mediumThreats > 5)
            {
                result.SecurityLevel = SecurityLevel.MediumRisk;
            }
            else if (mediumThreats > 0)
            {
                result.SecurityLevel = SecurityLevel.LowRisk;
            }
        }

        public void Dispose()
        {
            // 清理应用程序域
            foreach (var appDomain in _modAppDomains.Values)
            {
                try
                {
                    AppDomain.Unload(appDomain);
                }
                catch (Exception)
                {
                    // 忽略卸载错误
                }
            }
            _modAppDomains.Clear();
        }
    }
}