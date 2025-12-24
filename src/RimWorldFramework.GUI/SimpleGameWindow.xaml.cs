using System.Windows;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// SimpleGameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SimpleGameWindow : Window
    {
        private DispatcherTimer? _demoTimer;
        private bool _isDemoRunning = false;
        private int _demoStep = 0;

        public SimpleGameWindow()
        {
            InitializeComponent();
            AppendOutput("=== RimWorld 游戏框架演示 ===");
            AppendOutput("欢迎来到RimWorld游戏框架！");
            AppendOutput("点击'启动演示'开始体验游戏功能。");
            AppendOutput("");
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StopDemo();
            this.Close();
        }

        private void StartDemoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDemoRunning) return;

            _isDemoRunning = true;
            _demoStep = 0;
            StartDemoButton.IsEnabled = false;
            StopDemoButton.IsEnabled = true;

            AppendOutput("=== 开始演示 ===");
            AppendOutput("正在初始化RimWorld游戏框架...");

            _demoTimer = new DispatcherTimer();
            _demoTimer.Interval = TimeSpan.FromSeconds(2);
            _demoTimer.Tick += DemoTimer_Tick;
            _demoTimer.Start();
        }

        private void StopDemoButton_Click(object sender, RoutedEventArgs e)
        {
            StopDemo();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            GameOutput.Text = "";
            AppendOutput("输出已清空");
        }

        private void DemoTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDemoRunning) return;

            _demoStep++;

            switch (_demoStep)
            {
                case 1:
                    AppendOutput("✓ ECS系统初始化完成");
                    AppendOutput("✓ 事件系统启动");
                    break;
                case 2:
                    AppendOutput("✓ 创建游戏世界...");
                    AppendOutput("✓ 生成地形: 草原、森林、山脉");
                    break;
                case 3:
                    AppendOutput("✓ 生成角色:");
                    AppendOutput("  - 张三 (建筑师) - 技能: 建造 8, 采矿 5");
                    AppendOutput("  - 李四 (矿工) - 技能: 采矿 9, 建造 4");
                    AppendOutput("  - 王五 (研究员) - 技能: 研究 10, 医疗 6");
                    break;
                case 4:
                    AppendOutput("✓ 任务系统启动");
                    AppendOutput("✓ 分配任务:");
                    AppendOutput("  - 建造房屋 (优先级: 高)");
                    AppendOutput("  - 采集资源 (优先级: 中)");
                    AppendOutput("  - 研究科技 (优先级: 低)");
                    break;
                case 5:
                    AppendOutput("✓ AI行为树激活");
                    AppendOutput("张三: 开始建造墙壁...");
                    AppendOutput("李四: 前往矿区采集铁矿...");
                    AppendOutput("王五: 在研究台研究工具制作...");
                    break;
                case 6:
                    AppendOutput("📊 资源更新:");
                    AppendOutput("  木材: +15, 石材: +8, 铁矿: +12");
                    AppendOutput("张三: 墙壁建造进度 25%");
                    AppendOutput("李四: 发现了优质铁矿脉！");
                    break;
                case 7:
                    AppendOutput("🔬 研究完成: 工具制作技术");
                    AppendOutput("王五: 开始研究高级建筑技术...");
                    AppendOutput("张三: 墙壁建造进度 50%");
                    AppendOutput("李四: 继续采矿作业...");
                    break;
                case 8:
                    AppendOutput("🏠 建筑完成: 基础房屋");
                    AppendOutput("张三: 开始建造屋顶...");
                    AppendOutput("📊 资源更新:");
                    AppendOutput("  木材: +20, 石材: +15, 铁矿: +18");
                    break;
                case 9:
                    AppendOutput("⚡ 随机事件: 商队到访");
                    AppendOutput("商队带来了稀有材料和工具");
                    AppendOutput("🔄 任务重新分配:");
                    AppendOutput("  - 与商队交易 (新任务)");
                    break;
                case 10:
                    AppendOutput("✅ 交易完成: 获得高级工具");
                    AppendOutput("🏆 成就解锁: 第一个定居点");
                    AppendOutput("📈 殖民地发展等级: 1 → 2");
                    AppendOutput("");
                    AppendOutput("=== 演示完成 ===");
                    AppendOutput("这展示了RimWorld框架的核心功能:");
                    AppendOutput("• ECS架构 • AI行为树 • 任务系统");
                    AppendOutput("• 资源管理 • 事件系统 • 程序化内容");
                    StopDemo();
                    break;
            }
        }

        private void StopDemo()
        {
            _isDemoRunning = false;
            _demoTimer?.Stop();
            _demoTimer = null;
            StartDemoButton.IsEnabled = true;
            StopDemoButton.IsEnabled = false;

            if (_demoStep > 0)
            {
                AppendOutput("演示已停止");
            }
        }

        private void AppendOutput(string text)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            GameOutput.Text += $"[{timestamp}] {text}\n";
        }

        protected override void OnClosed(EventArgs e)
        {
            StopDemo();
            base.OnClosed(e);
        }
    }
}