using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RimWorldFramework.Tests.Core
{
    /// <summary>
    /// 游戏框架属性测试（更新版）
    /// Feature: rimworld-game-framework, Property 1: 错误处理和日志记录
    /// </summary>
    [TestFixture]
    public class GameFrameworkPropertyTestsUpdated : TestBase
    {
        private GameFramework? _framework;
        private TestLoggerProvider? _testLoggerProvider;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _testLoggerProvider = new TestLoggerProvider();
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(_testLoggerProvider);
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            
            var logger = loggerFactory.CreateLogger<GameFramework>();
            _framework = new GameFramework(logger);
        }

        [TearDown]
        public override void TearDown()
        {
            _framework?.Dispose();
            base.TearDown();
        }

        /// <summary>
        /// 属性 1: 错误处理和日志记录
        /// 对于任何系统错误情况，游戏框架应当记录错误信息并继续运行而不崩溃
        /// 验证需求: 需求 1.4
        /// </summary>
        [Property]
        public Property ErrorHandlingAndLogging()
        {
            return Prop.ForAll<string, int>((errorMessage, errorCode) =>
            {
                // 安排：创建一个会抛出异常的系统
                var faultySystem = new FaultyTestSystem(errorMessage, errorCode);
                
                // 行动：注册系统并尝试初始化
                _framework!.RegisterSystem(faultySystem);
                
                bool frameworkStillRunning = false;
                bool errorWasLogged = false;
                
                try
                {
                    _framework.Initialize(CreateTestConfig());
                    _framework.Update(0.016f); // 模拟一帧更新
                    frameworkStillRunning = _framework.IsRunning;
                    errorWasLogged = _testLoggerProvider!.HasErrorsLogged();
                }
                catch (Exception)
                {
                    // 框架不应该因为系统错误而崩溃
                    frameworkStillRunning = false;
                }
                
                // 断言：框架应该继续运行并记录错误
                return frameworkStillRunning && errorWasLogged;
            });
        }

        /// <summary>
        /// 测试框架在系统初始化失败时的行为
        /// </summary>
        [Property]
        public Property SystemInitializationFailureHandling()
        {
            return Prop.ForAll<string>((systemName) =>
            {
                // 过滤掉空字符串
                if (string.IsNullOrWhiteSpace(systemName))
                    return true;

                // 安排：创建一个初始化时会失败的系统
                var failingSystem = new InitializationFailureSystem(systemName);
                
                // 行动：注册系统并尝试初始化框架
                _framework!.RegisterSystem(failingSystem);
                
                bool initializationCompleted = false;
                bool errorWasLogged = false;
                
                try
                {
                    _framework.Initialize(CreateTestConfig());
                    initializationCompleted = _framework.IsInitialized;
                    errorWasLogged = _testLoggerProvider!.HasErrorsLogged();
                }
                catch (Exception)
                {
                    // 框架初始化不应该因为单个系统失败而完全失败
                    initializationCompleted = false;
                }
                
                // 断言：框架应该完成初始化并记录错误
                return initializationCompleted && errorWasLogged;
            });
        }

        /// <summary>
        /// 测试框架在系统更新时的错误处理
        /// </summary>
        [Property]
        public Property SystemUpdateErrorHandling()
        {
            return Prop.ForAll<float>((deltaTime) =>
            {
                // 限制deltaTime在合理范围内
                if (deltaTime < 0 || deltaTime > 1.0f)
                    return true;

                // 安排：创建一个更新时会出错的系统
                var errorSystem = new UpdateErrorSystem();
                _framework!.RegisterSystem(errorSystem);
                _framework.Initialize(CreateTestConfig());
                
                // 行动：多次更新框架
                bool frameworkStillRunning = true;
                bool errorWasLogged = false;
                
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        _framework.Update(deltaTime);
                        if (!_framework.IsRunning)
                        {
                            frameworkStillRunning = false;
                            break;
                        }
                    }
                    errorWasLogged = _testLoggerProvider!.HasErrorsLogged();
                }
                catch (Exception)
                {
                    frameworkStillRunning = false;
                }
                
                // 断言：框架应该继续运行并记录错误
                return frameworkStillRunning && errorWasLogged;
            });
        }

        /// <summary>
        /// 测试框架事件系统的错误处理
        /// </summary>
        [Property]
        public Property EventSystemErrorHandling()
        {
            return Prop.ForAll<string>((eventData) =>
            {
                // 安排：初始化框架
                _framework!.Initialize(CreateTestConfig());
                var eventBus = _framework.GetEventBus();
                
                // 订阅一个会抛出异常的事件处理器
                eventBus.Subscribe<TestEvent>(evt => 
                {
                    throw new InvalidOperationException($"Event handler error: {eventData}");
                });
                
                bool frameworkStillRunning = true;
                bool errorWasLogged = false;
                
                try
                {
                    // 发布事件
                    eventBus.Publish(new TestEvent(eventData ?? "test"));
                    
                    // 继续更新框架
                    _framework.Update(0.016f);
                    frameworkStillRunning = _framework.IsRunning;
                    errorWasLogged = _testLoggerProvider!.HasErrorsLogged();
                }
                catch (Exception)
                {
                    frameworkStillRunning = false;
                }
                
                // 断言：框架应该继续运行并记录错误
                return frameworkStillRunning && errorWasLogged;
            });
        }
    }

    /// <summary>
    /// 测试事件
    /// </summary>
    public class TestEvent : GameEvent
    {
        public string Data { get; }

        public TestEvent(string data)
        {
            Data = data ?? string.Empty;
        }
    }

    /// <summary>
    /// 测试日志提供程序
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<LogEntry> _logs = new();
        private readonly object _lock = new();

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(this, categoryName);
        }

        public void AddLog(LogLevel level, string message, Exception? exception = null)
        {
            lock (_lock)
            {
                _logs.Add(new LogEntry(level, message, exception));
            }
        }

        public bool HasErrorsLogged()
        {
            lock (_lock)
            {
                return _logs.Any(log => log.Level >= LogLevel.Error);
            }
        }

        public IReadOnlyList<LogEntry> GetLogs()
        {
            lock (_lock)
            {
                return _logs.ToList();
            }
        }

        public void Dispose()
        {
            // 清理资源
        }

        private class TestLogger : ILogger
        {
            private readonly TestLoggerProvider _provider;
            private readonly string _categoryName;

            public TestLogger(TestLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                _provider.AddLog(logLevel, $"[{_categoryName}] {message}", exception);
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance { get; } = new();
                public void Dispose() { }
            }
        }
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public LogLevel Level { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; }

        public LogEntry(LogLevel level, string message, Exception? exception = null)
        {
            Level = level;
            Message = message;
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }
    }
}