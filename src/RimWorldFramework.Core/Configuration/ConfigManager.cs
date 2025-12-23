using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RimWorldFramework.Core.Configuration
{
    /// <summary>
    /// 配置管理器实现
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        private readonly ILogger<ConfigManager>? _logger;
        private readonly object _lock = new();
        private GameConfig _currentConfig;
        private readonly Dictionary<string, object> _configValues = new();

        public event Action<GameConfig>? ConfigChanged;
        public event Action<ConfigValidationResult>? ConfigValidationFailed;

        public ConfigManager(ILogger<ConfigManager>? logger = null)
        {
            _logger = logger;
            _currentConfig = CreateDefaultConfig();
            PopulateConfigValues(_currentConfig);
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("Config path cannot be null or empty", nameof(configPath));

            lock (_lock)
            {
                try
                {
                    _logger?.LogInformation("Loading configuration from {ConfigPath}", configPath);

                    if (!File.Exists(configPath))
                    {
                        _logger?.LogWarning("Configuration file not found at {ConfigPath}, using defaults", configPath);
                        return;
                    }

                    var json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<GameConfig>(json);

                    if (config == null)
                    {
                        _logger?.LogError("Failed to deserialize configuration from {ConfigPath}", configPath);
                        throw new InvalidOperationException($"Failed to load configuration from {configPath}");
                    }

                    var validationResult = ValidateConfig(config);
                    if (!validationResult.IsValid)
                    {
                        _logger?.LogError("Configuration validation failed: {Errors}", 
                            string.Join(", ", validationResult.Errors));
                        
                        ConfigValidationFailed?.Invoke(validationResult);
                        throw new InvalidOperationException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
                    }

                    if (validationResult.Warnings.Any())
                    {
                        _logger?.LogWarning("Configuration warnings: {Warnings}", 
                            string.Join(", ", validationResult.Warnings));
                    }

                    _currentConfig = config;
                    PopulateConfigValues(_currentConfig);

                    _logger?.LogInformation("Configuration loaded successfully from {ConfigPath}", configPath);
                    ConfigChanged?.Invoke(_currentConfig);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error loading configuration from {ConfigPath}", configPath);
                    throw;
                }
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("Config path cannot be null or empty", nameof(configPath));

            lock (_lock)
            {
                try
                {
                    _logger?.LogInformation("Saving configuration to {ConfigPath}", configPath);

                    var validationResult = ValidateConfig(_currentConfig);
                    if (!validationResult.IsValid)
                    {
                        _logger?.LogError("Cannot save invalid configuration: {Errors}", 
                            string.Join(", ", validationResult.Errors));
                        throw new InvalidOperationException($"Cannot save invalid configuration: {string.Join(", ", validationResult.Errors)}");
                    }

                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var json = JsonConvert.SerializeObject(_currentConfig, Formatting.Indented);
                    File.WriteAllText(configPath, json);

                    _logger?.LogInformation("Configuration saved successfully to {ConfigPath}", configPath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error saving configuration to {ConfigPath}", configPath);
                    throw;
                }
            }
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public GameConfig GetConfig()
        {
            lock (_lock)
            {
                // 返回深拷贝以防止外部修改
                var json = JsonConvert.SerializeObject(_currentConfig);
                return JsonConvert.DeserializeObject<GameConfig>(json)!;
            }
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(GameConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_lock)
            {
                var validationResult = ValidateConfig(config);
                if (!validationResult.IsValid)
                {
                    _logger?.LogError("Configuration update failed validation: {Errors}", 
                        string.Join(", ", validationResult.Errors));
                    
                    ConfigValidationFailed?.Invoke(validationResult);
                    throw new ArgumentException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
                }

                if (validationResult.Warnings.Any())
                {
                    _logger?.LogWarning("Configuration update warnings: {Warnings}", 
                        string.Join(", ", validationResult.Warnings));
                }

                _currentConfig = config;
                PopulateConfigValues(_currentConfig);

                _logger?.LogInformation("Configuration updated successfully");
                ConfigChanged?.Invoke(_currentConfig);
            }
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public ConfigValidationResult ValidateConfig(GameConfig config)
        {
            if (config == null)
                return ConfigValidationResult.Failure("Configuration cannot be null");

            var errors = new List<string>();
            var warnings = new List<string>();

            // 验证图形配置
            if (config.Graphics != null)
            {
                if (config.Graphics.Width <= 0)
                    errors.Add("Graphics width must be greater than 0");
                
                if (config.Graphics.Height <= 0)
                    errors.Add("Graphics height must be greater than 0");
                
                if (config.Graphics.TargetFrameRate <= 0)
                    errors.Add("Target frame rate must be greater than 0");
                
                if (config.Graphics.Width < 800 || config.Graphics.Height < 600)
                    warnings.Add("Resolution below 800x600 may cause display issues");
            }

            // 验证音频配置
            if (config.Audio != null)
            {
                if (config.Audio.MasterVolume < 0 || config.Audio.MasterVolume > 1)
                    errors.Add("Master volume must be between 0 and 1");
                
                if (config.Audio.MusicVolume < 0 || config.Audio.MusicVolume > 1)
                    errors.Add("Music volume must be between 0 and 1");
                
                if (config.Audio.SfxVolume < 0 || config.Audio.SfxVolume > 1)
                    errors.Add("SFX volume must be between 0 and 1");
            }

            // 验证游戏玩法配置
            if (config.Gameplay != null)
            {
                if (config.Gameplay.AutoSaveInterval <= 0)
                    errors.Add("Auto save interval must be greater than 0");
                
                var validDifficulties = new[] { "Easy", "Normal", "Hard", "Extreme" };
                if (!validDifficulties.Contains(config.Gameplay.Difficulty))
                    errors.Add($"Difficulty must be one of: {string.Join(", ", validDifficulties)}");
            }

            // 验证模组配置
            if (config.Mods != null)
            {
                if (string.IsNullOrWhiteSpace(config.Mods.ModsDirectory))
                    errors.Add("Mods directory cannot be empty");
            }

            // 验证日志配置
            if (config.Logging != null)
            {
                var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
                if (!validLogLevels.Contains(config.Logging.LogLevel))
                    errors.Add($"Log level must be one of: {string.Join(", ", validLogLevels)}");
                
                if (config.Logging.MaxLogFiles <= 0)
                    errors.Add("Max log files must be greater than 0");
                
                if (config.Logging.MaxLogFileSize <= 0)
                    errors.Add("Max log file size must be greater than 0");
            }

            return errors.Any() 
                ? new ConfigValidationResult(false, errors, warnings)
                : new ConfigValidationResult(true, null, warnings);
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public T? GetConfigValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lock)
            {
                if (_configValues.TryGetValue(key, out var value))
                {
                    if (value is T typedValue)
                        return typedValue;
                    
                    // 尝试转换类型
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        _logger?.LogWarning("Failed to convert config value {Key} to type {Type}", key, typeof(T).Name);
                        return default;
                    }
                }

                return default;
            }
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public void SetConfigValue<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lock)
            {
                _configValues[key] = value!;
                
                // 尝试更新配置对象中的对应属性
                UpdateConfigProperty(key, value);
                
                _logger?.LogDebug("Config value {Key} set to {Value}", key, value);
                ConfigChanged?.Invoke(_currentConfig);
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public void ResetToDefaults()
        {
            lock (_lock)
            {
                _currentConfig = CreateDefaultConfig();
                PopulateConfigValues(_currentConfig);
                
                _logger?.LogInformation("Configuration reset to defaults");
                ConfigChanged?.Invoke(_currentConfig);
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private static GameConfig CreateDefaultConfig()
        {
            return new GameConfig
            {
                Graphics = new GraphicsConfig(),
                Audio = new AudioConfig(),
                Gameplay = new GameplayConfig(),
                Mods = new ModConfig(),
                Logging = new LoggingConfig()
            };
        }

        /// <summary>
        /// 填充配置值字典
        /// </summary>
        private void PopulateConfigValues(GameConfig config)
        {
            _configValues.Clear();
            
            var configJson = JsonConvert.SerializeObject(config);
            var configJObject = JObject.Parse(configJson);
            
            FlattenJObject(configJObject, "", _configValues);
        }

        /// <summary>
        /// 扁平化JObject
        /// </summary>
        private static void FlattenJObject(JObject jObject, string prefix, Dictionary<string, object> result)
        {
            foreach (var property in jObject.Properties())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                
                if (property.Value is JObject nestedObject)
                {
                    FlattenJObject(nestedObject, key, result);
                }
                else if (property.Value is JValue jValue && jValue.Value != null)
                {
                    result[key] = jValue.Value;
                }
            }
        }

        /// <summary>
        /// 更新配置对象属性
        /// </summary>
        private void UpdateConfigProperty<T>(string key, T value)
        {
            try
            {
                var parts = key.Split('.');
                if (parts.Length < 2) return;

                var configProperty = typeof(GameConfig).GetProperty(parts[0]);
                if (configProperty == null) return;

                var configSection = configProperty.GetValue(_currentConfig);
                if (configSection == null) return;

                var targetProperty = configSection.GetType().GetProperty(parts[1]);
                if (targetProperty != null && targetProperty.CanWrite)
                {
                    targetProperty.SetValue(configSection, value);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update config property {Key}", key);
            }
        }
    }
}