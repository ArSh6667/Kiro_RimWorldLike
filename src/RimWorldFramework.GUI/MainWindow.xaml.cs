using System.Windows;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateStatus("就绪");
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("正在启动游戏...");
                
                // 禁用按钮防止重复点击
                StartGameButton.IsEnabled = false;
                
                // 创建并显示游戏窗口
                var gameWindow = new GameWindow();
                gameWindow.Owner = this;
                gameWindow.Show();
                
                // 隐藏主窗口
                this.Hide();
                
                // 当游戏窗口关闭时，重新显示主窗口
                gameWindow.Closed += (s, args) =>
                {
                    this.Show();
                    StartGameButton.IsEnabled = true;
                    UpdateStatus("游戏已结束，就绪");
                };
                
                UpdateStatus("游戏运行中");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动游戏时发生错误: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StartGameButton.IsEnabled = true;
                UpdateStatus("启动失败");
            }
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要退出游戏吗？", "确认退出", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                UpdateStatus("正在退出...");
                Application.Current.Shutdown();
            }
        }

        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"状态: {status}";
            });
        }
    }
}