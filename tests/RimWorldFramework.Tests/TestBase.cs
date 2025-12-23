using Microsoft.Extensions.Logging;
using System;

namespace RimWorldFramework.Tests
{
    /// <summary>
    /// 测试基类，提供通用的测试设置和工具
    /// </summary>
    public abstract class TestBase
    {
        protected ILogger Logger { get; private set; } = null!;

        [SetUp]
        public virtual void SetUp()
        {
            // 创建测试用的日志记录器
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            });
            Logger = loggerFactory.CreateLogger(GetType().Name);
        }

        [TearDown]
        public virtual void TearDown()
        {
            // 清理资源
        }

        /// <summary>
        /// 创建默认的游戏配置用于测试
        /// </summary>
        protected GameConfig CreateTestConfig()
        {
            return new GameConfig
            {
                Graphics = new GraphicsConfig
                {
                    Width = 800,
                    Height = 600,
                    Fullscreen = false,
                    TargetFrameRate = 60
                },
                Audio = new AudioConfig
                {
                    MasterVolume = 0.5f,
                    MusicVolume = 0.3f,
                    SfxVolume = 0.7f,
                    Muted = false
                },
                Gameplay = new GameplayConfig
                {
                    Difficulty = "Easy",
                    AutoSave = false, // 测试时禁用自动保存
                    AutoSaveInterval = 60,
                    PauseOnFocusLost = false
                },
                Mods = new ModConfig
                {
                    EnableMods = false, // 测试时禁用模组
                    EnabledMods = new(),
                    ModsDirectory = "TestMods",
                    AllowUnsafeMods = false
                },
                Logging = new LoggingConfig
                {
                    LogLevel = "Debug",
                    LogToFile = false, // 测试时不写入文件
                    LogDirectory = "TestLogs",
                    MaxLogFiles = 5,
                    MaxLogFileSize = 1024 * 1024 // 1MB
                }
            };
        }

        /// <summary>
        /// 断言操作不会抛出异常
        /// </summary>
        protected void AssertDoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 断言操作会抛出指定类型的异常
        /// </summary>
        protected void AssertThrows<T>(Action action) where T : Exception
        {
            Assert.Throws<T>(() => action());
        }
    }
}