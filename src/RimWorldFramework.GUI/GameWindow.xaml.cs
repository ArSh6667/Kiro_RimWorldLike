using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// GameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameWindow : Window
    {
        private DispatcherTimer _gameTimer = null!;
        private DateTime _gameStartTime;
        private Process? _demoProcess;
        private bool _isDemoRunning = false;

        public GameWindow()
        {
            InitializeComponent();
            InitializeGameTimer();
            _gameStartTime = DateTime.Now;
            
            // 初始化游戏输出
            AppendGameOutput("=== RimWorld 游戏框架 ===");
            AppendGameOutput("欢迎来到RimWorld游戏框架演示！");
            AppendGameOutput("点击'启动演示'开始体验游戏功能。");
            AppendGameOutput("");
        }

        private void InitializeGameTimer()
        {
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _gameStartTime;
            GameTimeText.Text = $"运行时间: {elapsed:hh\\:mm\\:ss}";
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要返回主菜单吗？正在运行的演示将被停止。", 
                "确认返回", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                StopDemo();
                this.Close();
            }
        }

        private async void StartDemoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDemoRunning)
                return;

            try
            {
                StartDemoButton.IsEnabled = false;
                StopDemoButton.IsEnabled = true;
                _isDemoRunning = true;
                
                UpdateGameStatus("正在启动演示...");
                AppendGameOutput("正在启动RimWorld框架演示...");
                
                // 启动独立演示程序
                await StartStandaloneDemo();
            }
            catch (Exception ex)
            {
                AppendGameOutput($"启动演示失败: {ex.Message}");
                UpdateGameStatus("演示启动失败");
                StartDemoButton.IsEnabled = true;
                StopDemoButton.IsEnabled = false;
                _isDemoRunning = false;
            }
        }

        private void StopDemoButton_Click(object sender, RoutedEventArgs e)
        {
            StopDemo();
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            GameOutputText.Text = "";
            AppendGameOutput("输出已清空");
        }

        private async Task StartStandaloneDemo()
        {
            try
            {
                // 查找项目根目录
                var currentDir = Directory.GetCurrentDirectory();
                var projectRoot = FindProjectRoot(currentDir);
                
                if (projectRoot == null)
                {
                    // 如果找不到项目根目录，尝试相对路径
                    var possiblePaths = new[]
                    {
                        Path.Combine(currentDir, "src", "RimWorldFramework.StandaloneDemo"),
                        Path.Combine(currentDir, "..", "src", "RimWorldFramework.StandaloneDemo"),
                        Path.Combine(currentDir, "..", "..", "src", "RimWorldFramework.StandaloneDemo")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (Directory.Exists(path))
                        {
                            projectRoot = Path.GetDirectoryName(Path.GetDirectoryName(path));
                            break;
                        }
                    }
                }

                if (projectRoot == null)
                {
                    AppendGameOutput("无法找到项目根目录，启动模拟演示...");
                    await StartSimulatedDemo();
                    return;
                }

                var demoProjectPath = Path.Combine(projectRoot, "src", "RimWorldFramework.StandaloneDemo");
                
                if (!Directory.Exists(demoProjectPath))
                {
                    AppendGameOutput($"演示项目不存在: {demoProjectPath}");
                    AppendGameOutput("启动模拟演示...");
                    await StartSimulatedDemo();
                    return;
                }

                AppendGameOutput($"找到演示项目: {demoProjectPath}");
                AppendGameOutput("正在构建演示项目...");

                // 检查dotnet是否可用
                if (!IsDotNetAvailable())
                {
                    AppendGameOutput("未找到dotnet命令，启动模拟演示...");
                    await StartSimulatedDemo();
                    return;
                }

                // 构建项目
                var buildProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{demoProjectPath}\" --verbosity quiet",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = projectRoot
                    }
                };

                buildProcess.Start();
                var buildOutput = await buildProcess.StandardOutput.ReadToEndAsync();
                var buildError = await buildProcess.StandardError.ReadToEndAsync();
                await buildProcess.WaitForExitAsync();

                if (buildProcess.ExitCode != 0)
                {
                    AppendGameOutput("构建失败，启动模拟演示...");
                    AppendGameOutput($"构建错误: {buildError}");
                    await StartSimulatedDemo();
                    return;
                }

                AppendGameOutput("构建成功！正在启动演示...");

                // 运行演示
                _demoProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{demoProjectPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = projectRoot
                    }
                };

                _demoProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() => AppendGameOutput(e.Data));
                    }
                };

                _demoProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.Invoke(() => AppendGameOutput($"错误: {e.Data}"));
                    }
                };

                _demoProcess.Exited += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        AppendGameOutput("演示程序已退出");
                        UpdateGameStatus("演示已停止");
                        StartDemoButton.IsEnabled = true;
                        StopDemoButton.IsEnabled = false;
                        _isDemoRunning = false;
                    });
                };

                _demoProcess.EnableRaisingEvents = true;
                _demoProcess.Start();
                _demoProcess.BeginOutputReadLine();
                _demoProcess.BeginErrorReadLine();

                UpdateGameStatus("演示运行中");
                AppendGameOutput("演示程序已启动！");
            }
            catch (Exception ex)
            {
                AppendGameOutput($"启动演示时发生错误: {ex.Message}");
                AppendGameOutput("启动模拟演示...");
                await StartSimulatedDemo();
            }
        }

        private async Task StartSimulatedDemo()
        {
            try
            {
                UpdateGameStatus("模拟演示运行中");
                AppendGameOutput("=== 模拟演示模式 ===");
                AppendGameOutput("正在模拟RimWorld游戏框架运行...");
                
                await Task.Delay(1000);
                AppendGameOutput("初始化ECS系统...");
                
                await Task.Delay(800);
                AppendGameOutput("创建游戏世界...");
                
                await Task.Delay(600);
                AppendGameOutput("生成角色: 张三 (建筑师)");
                AppendGameOutput("生成角色: 李四 (矿工)");
                AppendGameOutput("生成角色: 王五 (研究员)");
                
                await Task.Delay(1000);
                AppendGameOutput("分配任务: 建造房屋");
                AppendGameOutput("分配任务: 采集资源");
                AppendGameOutput("分配任务: 研究科技");
                
                // 模拟游戏循环
                for (int i = 0; i < 10 && _isDemoRunning; i++)
                {
                    await Task.Delay(2000);
                    if (!_isDemoRunning) break;
                    
                    var actions = new[]
                    {
                        "张三正在建造墙壁...",
                        "李四发现了铁矿石！",
                        "王五完成了工具研究",
                        "系统: 资源收集 +10",
                        "系统: 建筑进度 +15%",
                        "系统: 科技点数 +5"
                    };
                    
                    var random = new Random();
                    AppendGameOutput(actions[random.Next(actions.Length)]);
                }
                
                if (_isDemoRunning)
                {
                    AppendGameOutput("=== 演示完成 ===");
                    AppendGameOutput("这是RimWorld游戏框架的模拟演示");
                    AppendGameOutput("实际框架包含完整的ECS系统、AI行为树、任务管理等功能");
                }
            }
            catch (Exception ex)
            {
                AppendGameOutput($"模拟演示错误: {ex.Message}");
            }
            finally
            {
                if (_isDemoRunning)
                {
                    UpdateGameStatus("模拟演示完成");
                    StartDemoButton.IsEnabled = true;
                    StopDemoButton.IsEnabled = false;
                    _isDemoRunning = false;
                }
            }
        }

        private bool IsDotNetAvailable()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private string? FindProjectRoot(string startPath)
        {
            var current = new DirectoryInfo(startPath);
            
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "RimWorldFramework.sln")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            
            return null;
        }

        private void StopDemo()
        {
            try
            {
                _isDemoRunning = false;
                
                if (_demoProcess != null && !_demoProcess.HasExited)
                {
                    _demoProcess.Kill();
                    _demoProcess.Dispose();
                    _demoProcess = null;
                }
                
                AppendGameOutput("演示已停止");
                UpdateGameStatus("演示已停止");
                StartDemoButton.IsEnabled = true;
                StopDemoButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                AppendGameOutput($"停止演示时发生错误: {ex.Message}");
            }
        }

        private void AppendGameOutput(string text)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            GameOutputText.Text += $"[{timestamp}] {text}\n";
            
            // 自动滚动到底部
            GameOutputScrollViewer.ScrollToEnd();
        }

        private void UpdateGameStatus(string status)
        {
            GameStatusText.Text = $"游戏状态: {status}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();
            StopDemo();
            base.OnClosed(e);
        }
    }
}