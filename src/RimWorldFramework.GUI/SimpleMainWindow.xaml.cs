using System.Windows;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// SimpleMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SimpleMainWindow : Window
    {
        public SimpleMainWindow()
        {
            InitializeComponent();
        }

        private void GameWorldButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "状态: 正在加载游戏世界...";
                
                // 创建并显示游戏世界窗口
                var gameWorldWindow = new GameWorldWindow();
                gameWorldWindow.Owner = this;
                gameWorldWindow.Show();
                
                // 隐藏主窗口
                this.Hide();
                
                // 当游戏世界窗口关闭时，重新显示主窗口
                gameWorldWindow.Closed += (s, args) =>
                {
                    this.Show();
                    StatusText.Text = "状态: 就绪";
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动游戏世界时发生错误: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "状态: 启动失败";
            }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "状态: 正在启动演示...";
                
                // 创建并显示游戏窗口
                var gameWindow = new SimpleGameWindow();
                gameWindow.Owner = this;
                gameWindow.Show();
                
                // 隐藏主窗口
                this.Hide();
                
                // 当游戏窗口关闭时，重新显示主窗口
                gameWindow.Closed += (s, args) =>
                {
                    this.Show();
                    StatusText.Text = "状态: 就绪";
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动演示时发生错误: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "状态: 启动失败";
            }
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要退出游戏吗？", "确认退出", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}