using System;
using System.Collections.Generic;

namespace RimWorldFramework.Core.Configuration
{
    /// <summary>
    /// 配置管理器接口
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// 加载配置
        /// </summary>
        void LoadConfig(string configPath);

        /// <summary>
        /// 保存配置
        /// </summary>
        void SaveConfig(string configPath);

        /// <summary>
        /// 获取当前配置
        /// </summary>
        GameConfig GetConfig();

        /// <summary>
        /// 更新配置
        /// </summary>
        void UpdateConfig(GameConfig config);

        /// <summary>
        /// 验证配置
        /// </summary>
        ConfigValidationResult ValidateConfig(GameConfig config);

        /// <summary>
        /// 获取配置值
        /// </summary>
        T? GetConfigValue<T>(string key);

        /// <summary>
        /// 设置配置值
        /// </summary>
        void SetConfigValue<T>(string key, T value);

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// 配置变更事件
        /// </summary>
        event Action<GameConfig>? ConfigChanged;

        /// <summary>
        /// 配置验证失败事件
        /// </summary>
        event Action<ConfigValidationResult>? ConfigValidationFailed;
    }

    /// <summary>
    /// 配置验证结果
    /// </summary>
    public class ConfigValidationResult
    {
        public bool IsValid { get; }
        public List<string> Errors { get; }
        public List<string> Warnings { get; }

        public ConfigValidationResult(bool isValid, List<string>? errors = null, List<string>? warnings = null)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public static ConfigValidationResult Success() => new(true);
        
        public static ConfigValidationResult Failure(params string[] errors) => 
            new(false, new List<string>(errors));
        
        public static ConfigValidationResult WithWarnings(params string[] warnings) => 
            new(true, null, new List<string>(warnings));
    }
}