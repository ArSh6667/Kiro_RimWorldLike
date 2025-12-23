using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RimWorldFramework.Tests.Core
{
    /// <summary>
    /// 游戏框架属性测试
    /// Feature: rimworld-game-framework, Property 1: 错误处理和日志记录
    /// </summary>
    [TestFixture]
    public class GameFrameworkPropertyTests : TestBase
    {
        private TestGameFramework? _framework;
        private TestLogger? _testLogger;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _testLogger = new TestLogger();
            _framework = new TestGameFramework(_testLogger);
        }

        [TearDown]
        public override void TearDown()
        {
            _framework?.Shutdown();
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
                    errorWasLogged = _testLogger!.HasErrorsLogged();
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
                    errorWasLogged = _testLogger!.HasErrorsLogged();
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
                    errorWasLogged = _testLogger!.HasErrorsLogged();
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
    /// 测试用的游戏框架实现
    /// </summary>
    internal class TestGameFramework : IGameFramework
    {
        private readonly Dictionary<Type, IGameSystem> _systems = new();
        private readonly TestLogger _logger;
        private bool _isInitialized;
        private bool _isRunning;

        public bool IsInitialized => _isInitialized;
        public bool IsRunning => _isRunning;

        public TestGameFramework(TestLogger logger)
        {
            _logger = logger;
        }

        public void Initialize(GameConfig config)
        {
            try
            {
                foreach (var system in _systems.Values)
                {
                    try
                    {
                        system.Initialize();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"System {system.Name} initialization failed: {ex.Message}");
                        // 继续初始化其他系统
                    }
                }
                _isInitialized = true;
                _isRunning = true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Framework initialization failed: {ex.Message}");
                throw;
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isInitialized || !_isRunning)
                return;

            foreach (var system in _systems.Values)
            {
                try
                {
                    system.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"System {system.Name} update failed: {ex.Message}");
                    // 继续更新其他系统，不让单个系统的错误影响整个框架
                }
            }
        }

        public void Shutdown()
        {
            try
            {
                foreach (var system in _systems.Values)
                {
                    try
                    {
                        system.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"System {system.Name} shutdown failed: {ex.Message}");
                    }
                }
                _isRunning = false;
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Framework shutdown failed: {ex.Message}");
            }
        }

        public T? GetSystem<T>() where T : class, IGameSystem
        {
            return _systems.TryGetValue(typeof(T), out var system) ? system as T : null;
        }

        public void RegisterSystem<T>(T system) where T : class, IGameSystem
        {
            _systems[typeof(T)] = system;
        }

        public void UnregisterSystem<T>() where T : class, IGameSystem
        {
            _systems.Remove(typeof(T));
        }

        public bool HasSystem<T>() where T : class, IGameSystem
        {
            return _systems.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// 测试用的日志记录器
    /// </summary>
    internal class TestLogger
    {
        private readonly List<string> _errorLogs = new();
        private readonly List<string> _infoLogs = new();

        public void LogError(string message)
        {
            _errorLogs.Add(message);
        }

        public void LogInfo(string message)
        {
            _infoLogs.Add(message);
        }

        public bool HasErrorsLogged() => _errorLogs.Count > 0;
        public IReadOnlyList<string> GetErrorLogs() => _errorLogs.AsReadOnly();
        public IReadOnlyList<string> GetInfoLogs() => _infoLogs.AsReadOnly();
    }

    /// <summary>
    /// 会抛出异常的测试系统
    /// </summary>
    internal class FaultyTestSystem : GameSystem
    {
        private readonly string _errorMessage;
        private readonly int _errorCode;

        public override int Priority => 100;
        public override string Name => "FaultyTestSystem";

        public FaultyTestSystem(string errorMessage, int errorCode)
        {
            _errorMessage = errorMessage ?? "Test error";
            _errorCode = errorCode;
        }

        protected override void OnInitialize()
        {
            // 初始化成功
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 在更新时抛出异常
            throw new InvalidOperationException($"{_errorMessage} (Code: {_errorCode})");
        }

        protected override void OnShutdown()
        {
            // 关闭成功
        }
    }

    /// <summary>
    /// 初始化时失败的测试系统
    /// </summary>
    internal class InitializationFailureSystem : GameSystem
    {
        public override int Priority => 50;
        public override string Name { get; }

        public InitializationFailureSystem(string name)
        {
            Name = name;
        }

        protected override void OnInitialize()
        {
            throw new InvalidOperationException($"System {Name} failed to initialize");
        }

        protected override void OnUpdate(float deltaTime)
        {
            // 正常更新
        }

        protected override void OnShutdown()
        {
            // 正常关闭
        }
    }

    /// <summary>
    /// 更新时出错的测试系统
    /// </summary>
    internal class UpdateErrorSystem : GameSystem
    {
        private int _updateCount = 0;

        public override int Priority => 75;
        public override string Name => "UpdateErrorSystem";

        protected override void OnInitialize()
        {
            // 初始化成功
        }

        protected override void OnUpdate(float deltaTime)
        {
            _updateCount++;
            if (_updateCount % 3 == 0) // 每三次更新抛出一次异常
            {
                throw new InvalidOperationException($"Update error on iteration {_updateCount}");
            }
        }

        protected override void OnShutdown()
        {
            // 关闭成功
        }
    }
}