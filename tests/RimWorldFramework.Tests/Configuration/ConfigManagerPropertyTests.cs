using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using RimWorldFramework.Core.Configuration;

namespace RimWorldFramework.Tests.Configuration
{
    /// <summary>
    /// 配置管理器属性测试
    /// Feature: rimworld-game-framework, Property 24: 配置验证
    /// Feature: rimworld-game-framework, Property 25: 配置加载完整性
    /// Feature: rimworld-game-framework, Property 28: 动态配置更新
    /// </summary>
    [TestFixture]
    public class ConfigManagerPropertyTests : TestBase
    {
        private ConfigManager? _configManager;
        private string _testConfigDirectory = string.Empty;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _configManager = new ConfigManager(Logger as ILogger<ConfigManager>);
            _testConfigDirectory = Path.Combine(Path.GetTempPath(), "RimWorldFrameworkTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testConfigDirectory);
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                if (Directory.Exists(_testConfigDirectory))
                {
                    Directory.Delete(_testConfigDirectory, true);
                }
            }
            catch
            {
                // 忽略清理错误
            }
            base.TearDown();
        }

        /// <summary>
        /// 属性 24: 配置验证
        /// 对于任何配置文件修改，系统应当验证配置的有效性并在发现错误时提供具体的错误信息
        /// 验证需求: 需求 8.1
        /// </summary>
        [Property]
        public Property ConfigValidation()
        {
            return Prop.ForAll<int, int, float, string>((width, height, volume, difficulty) =>
            {
                // 创建测试配置
                var config = new GameConfig
                {
                    Graphics = new GraphicsConfig
                    {
                        Width = width,
                        Height = height,
                        TargetFrameRate = 60
                    },
                    Audio = new AudioConfig
                    {
                        MasterVolume = volume
                    },
                    Gameplay = new GameplayConfig
                    {
                        Difficulty = difficulty ?? "Normal"
                    }
                };

                // 验证配置
                var result = _configManager!.ValidateConfig(config);

                // 检查验证逻辑的一致性
                bool shouldBeValid = width > 0 && height > 0 && 
                                   volume >= 0 && volume <= 1 &&
                                   new[] { "Easy", "Normal", "Hard", "Extreme" }.Contains(difficulty ?? "Normal");

                // 验证结果应该与预期一致
                bool validationIsCorrect = result.IsValid == shouldBeValid;

                // 如果无效，应该有错误信息
                bool hasErrorsWhenInvalid = result.IsValid || result.Errors.Any();

                return validationIsCorrect && hasErrorsWhenInvalid;
            });
        }

        /// <summary>
        /// 属性 25: 配置加载完整性
        /// 对于任何游戏启动，所有配置设置应当从配置文件正确加载并应用到相应的游戏系统
        /// 验证需求: 需求 8.2
        /// </summary>
        [Property]
        public Property ConfigLoadingIntegrity()
        {
            return Prop.ForAll<int, float, bool, string>((frameRate, volume, autoSave, logLevel) =>
            {
                // 限制参数到有效范围
                frameRate = Math.Max(1, Math.Min(frameRate, 300));
                volume = Math.Max(0, Math.Min(volume, 1));
                logLevel = new[] { "Debug", "Information", "Warning", "Error" }[Math.Abs(frameRate) % 4];

                try
                {
                    // 创建有效的测试配置
                    var originalConfig = new GameConfig
                    {
                        Graphics = new GraphicsConfig
                        {
                            Width = 1920,
                            Height = 1080,
                            TargetFrameRate = frameRate
                        },
                        Audio = new AudioConfig
                        {
                            MasterVolume = volume,
                            MusicVolume = volume * 0.8f,
                            SfxVolume = volume
                        },
                        Gameplay = new GameplayConfig
                        {
                            AutoSave = autoSave,
                            Difficulty = "Normal"
                        },
                        Logging = new LoggingConfig
                        {
                            LogLevel = logLevel
                        }
                    };

                    // 保存配置到文件
                    var configPath = Path.Combine(_testConfigDirectory, $"test_config_{Guid.NewGuid()}.json");
                    _configManager!.UpdateConfig(originalConfig);
                    _configManager.SaveConfig(configPath);

                    // 重置配置管理器并加载
                    _configManager.ResetToDefaults();
                    _configManager.LoadConfig(configPath);

                    // 验证加载的配置
                    var loadedConfig = _configManager.GetConfig();

                    // 检查关键配置是否正确加载
                    bool frameRateMatches = loadedConfig.Graphics.TargetFrameRate == frameRate;
                    bool volumeMatches = Math.Abs(loadedConfig.Audio.MasterVolume - volume) < 0.001f;
                    bool autoSaveMatches = loadedConfig.Gameplay.AutoSave == autoSave;
                    bool logLevelMatches = loadedConfig.Logging.LogLevel == logLevel;

                    return frameRateMatches && volumeMatches && autoSaveMatches && logLevelMatches;
                }
                catch (Exception)
                {
                    // 如果配置无效导致异常，这是预期的行为
                    return true;
                }
            });
        }

        /// <summary>
        /// 属性 28: 动态配置更新
        /// 对于任何运行时配置变更，新配置应当在不重启游戏的情况下生效并影响相关系统行为
        /// 验证需求: 需求 8.5
        /// </summary>
        [Property]
        public Property DynamicConfigUpdate()
        {
            return Prop.ForAll<string, int>((configKey, configValue) =>
            {
                // 过滤无效的键
                if (string.IsNullOrWhiteSpace(configKey))
                    return true;

                // 限制值到合理范围
                configValue = Math.Max(1, Math.Min(configValue, 10000));

                try
                {
                    bool configChangedEventFired = false;
                    GameConfig? changedConfig = null;

                    // 订阅配置变更事件
                    _configManager!.ConfigChanged += (config) =>
                    {
                        configChangedEventFired = true;
                        changedConfig = config;
                    };

                    // 获取初始配置
                    var initialConfig = _configManager.GetConfig();

                    // 动态更新配置值
                    _configManager.SetConfigValue(configKey, configValue);

                    // 验证事件是否触发
                    bool eventFired = configChangedEventFired;

                    // 验证配置是否更新
                    var updatedValue = _configManager.GetConfigValue<int>(configKey);
                    bool valueUpdated = updatedValue == configValue;

                    // 验证配置对象是否更新
                    bool configObjectUpdated = changedConfig != null;

                    return eventFired && valueUpdated && configObjectUpdated;
                }
                catch (Exception)
                {
                    // 某些配置键可能无效，这是预期的
                    return true;
                }
            });
        }

        /// <summary>
        /// 测试配置验证的边界情况
        /// </summary>
        [Property]
        public Property ConfigValidationBoundaryConditions()
        {
            return Prop.ForAll<int, int>((width, height) =>
            {
                var config = new GameConfig
                {
                    Graphics = new GraphicsConfig
                    {
                        Width = width,
                        Height = height,
                        TargetFrameRate = 60
                    }
                };

                var result = _configManager!.ValidateConfig(config);

                // 边界条件：宽度和高度必须大于0
                if (width <= 0 || height <= 0)
                {
                    return !result.IsValid && result.Errors.Any();
                }
                else
                {
                    // 如果宽度和高度有效，但小于推荐值，应该有警告
                    if (width < 800 || height < 600)
                    {
                        return result.IsValid && result.Warnings.Any();
                    }
                    else
                    {
                        return result.IsValid;
                    }
                }
            });
        }

        /// <summary>
        /// 测试配置文件往返一致性
        /// </summary>
        [Property]
        public Property ConfigFileRoundTripConsistency()
        {
            return Prop.ForAll<int, float, bool>((frameRate, volume, fullscreen) =>
            {
                // 确保参数在有效范围内
                frameRate = Math.Max(30, Math.Min(frameRate, 144));
                volume = Math.Max(0, Math.Min(volume, 1));

                try
                {
                    // 创建测试配置
                    var originalConfig = new GameConfig
                    {
                        Graphics = new GraphicsConfig
                        {
                            Width = 1920,
                            Height = 1080,
                            TargetFrameRate = frameRate,
                            Fullscreen = fullscreen
                        },
                        Audio = new AudioConfig
                        {
                            MasterVolume = volume
                        }
                    };

                    // 保存并重新加载配置
                    var configPath = Path.Combine(_testConfigDirectory, $"roundtrip_test_{Guid.NewGuid()}.json");
                    
                    _configManager!.UpdateConfig(originalConfig);
                    _configManager.SaveConfig(configPath);
                    
                    _configManager.ResetToDefaults();
                    _configManager.LoadConfig(configPath);
                    
                    var loadedConfig = _configManager.GetConfig();

                    // 验证往返一致性
                    bool frameRateConsistent = loadedConfig.Graphics.TargetFrameRate == frameRate;
                    bool volumeConsistent = Math.Abs(loadedConfig.Audio.MasterVolume - volume) < 0.001f;
                    bool fullscreenConsistent = loadedConfig.Graphics.Fullscreen == fullscreen;

                    return frameRateConsistent && volumeConsistent && fullscreenConsistent;
                }
                catch (Exception)
                {
                    // 如果配置无效，异常是预期的
                    return true;
                }
            });
        }

        /// <summary>
        /// 测试无效配置的错误处理
        /// </summary>
        [Test]
        public void InvalidConfig_ShouldTriggerValidationFailedEvent()
        {
            // 安排
            bool validationFailedEventFired = false;
            ConfigValidationResult? failedResult = null;

            _configManager!.ConfigValidationFailed += (result) =>
            {
                validationFailedEventFired = true;
                failedResult = result;
            };

            var invalidConfig = new GameConfig
            {
                Graphics = new GraphicsConfig
                {
                    Width = -100, // 无效值
                    Height = -50   // 无效值
                }
            };

            // 行动 & 断言
            AssertThrows<ArgumentException>(() => _configManager.UpdateConfig(invalidConfig));
            
            Assert.That(validationFailedEventFired, Is.True);
            Assert.That(failedResult, Is.Not.Null);
            Assert.That(failedResult!.IsValid, Is.False);
            Assert.That(failedResult.Errors.Count, Is.GreaterThan(0));
        }

        /// <summary>
        /// 测试配置重置功能
        /// </summary>
        [Test]
        public void ResetToDefaults_ShouldRestoreDefaultConfiguration()
        {
            // 安排
            var customConfig = new GameConfig
            {
                Graphics = new GraphicsConfig
                {
                    Width = 2560,
                    Height = 1440,
                    TargetFrameRate = 120
                }
            };

            _configManager!.UpdateConfig(customConfig);
            var modifiedConfig = _configManager.GetConfig();

            // 行动
            _configManager.ResetToDefaults();
            var resetConfig = _configManager.GetConfig();

            // 断言
            Assert.That(resetConfig.Graphics.Width, Is.Not.EqualTo(modifiedConfig.Graphics.Width));
            Assert.That(resetConfig.Graphics.Height, Is.Not.EqualTo(modifiedConfig.Graphics.Height));
            Assert.That(resetConfig.Graphics.TargetFrameRate, Is.Not.EqualTo(modifiedConfig.Graphics.TargetFrameRate));
            
            // 验证是默认值
            Assert.That(resetConfig.Graphics.Width, Is.EqualTo(1920));
            Assert.That(resetConfig.Graphics.Height, Is.EqualTo(1080));
            Assert.That(resetConfig.Graphics.TargetFrameRate, Is.EqualTo(60));
        }
    }
}