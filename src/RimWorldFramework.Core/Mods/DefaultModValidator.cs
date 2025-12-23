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
    /// 默认模组验证器实现
    /// </summary>
    public class DefaultModValidator : IModValidator
    {
        private readonly IModSecurityManager _securityManager;

        public DefaultModValidator(IModSecurityManager securityManager = null)
        {
            _securityManager = securityManager ?? new DefaultModSecurityManager();
        }

        public async Task<ModValidationResult> ValidateAsync(string modPath)
        {
            var result = new ModValidationResult
            {
                IsValid = true,
                SecurityLevel = SecurityLevel.Safe
            };

            try
            {
                // 验证模组目录结构
                await ValidateModStructureAsync(modPath, result);

                // 验证模组清单
                await ValidateManifestAsync(modPath, result);

                // 验证程序集安全性
                await ValidateAssemblySecurityAsync(modPath, result);

                // 验证资源文件
                await ValidateResourcesAsync(modPath, result);

                // 根据发现的问题确定最终状态
                if (result.Errors.Any(e => e.Type == ValidationErrorType.MaliciousCode))
                {
                    result.IsValid = false;
                    result.SecurityLevel = SecurityLevel.Dangerous;
                }
                else if (result.Warnings.Any())
                {
                    result.SecurityLevel = SecurityLevel.LowRisk;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError
                {
                    Type = ValidationErrorType.InvalidManifest,
                    Message = "Validation failed",
                    Details = ex.Message
                });
            }

            return result;
        }

        private async Task ValidateModStructureAsync(string modPath, ModValidationResult result)
        {
            // 检查必需文件
            var manifestPath = Path.Combine(modPath, "mod.json");
            if (!File.Exists(manifestPath))
            {
                result.Errors.Add(new ValidationError
                {
                    Type = ValidationErrorType.FileNotFound,
                    Message = "Mod manifest file not found",
                    FilePath = manifestPath
                });
                result.IsValid = false;
                return;
            }

            // 检查目录权限
            try
            {
                var testFile = Path.Combine(modPath, "test_write_permission.tmp");
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException)
            {
                result.Errors.Add(new ValidationError
                {
                    Type = ValidationErrorType.InsufficientPermissions,
                    Message = "Insufficient permissions to access mod directory",
                    FilePath = modPath
                });
                result.IsValid = false;
            }
        }

        private async Task ValidateManifestAsync(string modPath, ModValidationResult result)
        {
            var manifestPath = Path.Combine(modPath, "mod.json");
            
            try
            {
                var manifestJson = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<ModManifest>(manifestJson);

                // 验证必需字段
                if (string.IsNullOrEmpty(manifest.Id))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidManifest,
                        Message = "Mod ID is required",
                        FilePath = manifestPath
                    });
                    result.IsValid = false;
                }

                if (string.IsNullOrEmpty(manifest.Name))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidManifest,
                        Message = "Mod name is required",
                        FilePath = manifestPath
                    });
                    result.IsValid = false;
                }

                if (string.IsNullOrEmpty(manifest.Version))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidManifest,
                        Message = "Mod version is required",
                        FilePath = manifestPath
                    });
                    result.IsValid = false;
                }
                else
                {
                    // 验证版本格式
                    if (!Version.TryParse(manifest.Version, out _))
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Type = ValidationErrorType.InvalidManifest,
                            Message = "Invalid version format",
                            Details = $"Version '{manifest.Version}' is not in valid format",
                            FilePath = manifestPath
                        });
                        result.IsValid = false;
                    }
                }

                // 验证入口点
                foreach (var entryPoint in manifest.EntryPoints)
                {
                    if (!string.IsNullOrEmpty(entryPoint.Assembly))
                    {
                        var assemblyPath = Path.Combine(modPath, entryPoint.Assembly);
                        if (!File.Exists(assemblyPath))
                        {
                            result.Errors.Add(new ValidationError
                            {
                                Type = ValidationErrorType.FileNotFound,
                                Message = "Entry point assembly not found",
                                FilePath = assemblyPath
                            });
                            result.IsValid = false;
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                result.Errors.Add(new ValidationError
                {
                    Type = ValidationErrorType.InvalidManifest,
                    Message = "Invalid JSON format in manifest",
                    Details = ex.Message,
                    FilePath = manifestPath
                });
                result.IsValid = false;
            }
        }
        private async Task ValidateAssemblySecurityAsync(string modPath, ModValidationResult result)
        {
            var assemblyFiles = Directory.GetFiles(modPath, "*.dll", SearchOption.AllDirectories);

            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    var securityResult = await _securityManager.CheckAssemblySecurityAsync(assemblyFile);
                    
                    if (!securityResult.IsSecure)
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Type = ValidationErrorType.MaliciousCode,
                            Message = "Assembly contains potentially malicious code",
                            Details = securityResult.Details,
                            FilePath = assemblyFile
                        });
                        result.IsValid = false;
                    }

                    // 根据威胁级别添加警告
                    foreach (var threat in securityResult.Threats)
                    {
                        if (threat.Level >= ThreatLevel.Medium)
                        {
                            result.Warnings.Add(new ValidationWarning
                            {
                                Type = ValidationWarningType.CompatibilityIssue,
                                Message = $"Security concern: {threat.Description}",
                                Details = threat.Location
                            });
                        }
                    }

                    // 更新安全级别
                    if (securityResult.SecurityLevel > result.SecurityLevel)
                    {
                        result.SecurityLevel = securityResult.SecurityLevel;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Type = ValidationErrorType.InvalidAssembly,
                        Message = "Failed to validate assembly",
                        Details = ex.Message,
                        FilePath = assemblyFile
                    });
                    result.IsValid = false;
                }
            }
        }

        private async Task ValidateResourcesAsync(string modPath, ModValidationResult result)
        {
            // 检查资源文件大小
            var allFiles = Directory.GetFiles(modPath, "*", SearchOption.AllDirectories);
            long totalSize = 0;

            foreach (var file in allFiles)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;

                // 检查单个文件大小（100MB限制）
                if (fileInfo.Length > 100 * 1024 * 1024)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Type = ValidationWarningType.ExcessiveResourceUsage,
                        Message = "Large file detected",
                        Details = $"File {file} is {fileInfo.Length / (1024 * 1024)}MB"
                    });
                }
            }

            // 检查总大小（1GB限制）
            if (totalSize > 1024 * 1024 * 1024)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.ExcessiveResourceUsage,
                    Message = "Mod size is very large",
                    Details = $"Total size: {totalSize / (1024 * 1024)}MB"
                });
            }

            // 检查可疑文件扩展名
            var suspiciousExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1", ".vbs", ".scr" };
            var suspiciousFiles = allFiles.Where(f => 
                suspiciousExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();

            foreach (var suspiciousFile in suspiciousFiles)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Type = ValidationWarningType.CompatibilityIssue,
                    Message = "Suspicious file type detected",
                    Details = suspiciousFile
                });
            }
        }
    }
}