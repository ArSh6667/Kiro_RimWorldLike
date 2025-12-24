using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// GameWorldWindow.xaml 的交互逻辑 - 重构版本
    /// </summary>
    public partial class GameWorldWindow : Window
    {
        // 地图配置
        private const int MAP_SIZE = 256;           // 地图格子数量 256x256
        private const int TILE_SIZE = 32;           // 每格像素大小 32x32
        private const int WORLD_SIZE = MAP_SIZE * TILE_SIZE; // 总像素大小 8192x8192
        private const int CANVAS_SIZE = 16384;      // Canvas尺寸 (2倍世界大小，防止缩放黑幕)

        // 控制器
        private CameraController? _cameraController;
        private CharacterController? _characterController;

        // 游戏状态
        private DispatcherTimer _fpsTimer = null!;
        private DateTime _gameStartTime;
        private int _frameCount = 0;

        // 地图数据
        private Rectangle[,] _mapTiles = null!;
        private float[,] _noiseMap = null!;

        // ESC菜单状态
        private bool _isEscMenuVisible = false;

        // 地形颜色
        private readonly Brush[] _terrainColors = new Brush[]
        {
            Brushes.Black,              // 深水/岩石
            new SolidColorBrush(Color.FromRgb(255, 234, 167)), // 浅黄色 - 沙地/沙漠
            new SolidColorBrush(Color.FromRgb(144, 238, 144)), // 浅绿色 - 草地/森林
            Brushes.White               // 雪地/高山
        };

        public GameWorldWindow()
        {
            InitializeComponent();
            InitializeControllers();
            InitializeGame();
        }

        private void InitializeControllers()
        {
            // 初始化相机控制器
            _cameraController = new CameraController(MapScrollViewer, MapScaleTransform);
            _cameraController.ZoomChanged += OnZoomChanged;
            _cameraController.FollowModeChanged += OnFollowModeChanged;

            // 初始化人物控制器
            _characterController = new CharacterController(GameCanvas);
            _characterController.CharacterMoved += OnCharacterMoved;
            _characterController.CharacterPositionChanged += OnCharacterPositionChanged;

            // 添加键盘事件支持
            this.KeyDown += GameWorldWindow_KeyDown;
            this.KeyUp += GameWorldWindow_KeyUp;
            this.Focusable = true;
            this.Focus();
        }

        private void InitializeGame()
        {
            _gameStartTime = DateTime.Now;

            // 初始化FPS定时器
            _fpsTimer = new DispatcherTimer();
            _fpsTimer.Interval = TimeSpan.FromSeconds(1);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            // 生成地图
            GenerateMap();

            // 创建人物
            _characterController?.CreateCharacter();

            // 居中视图
            _cameraController?.CenterView();

            UpdateUI();
        }

        private void GenerateMap()
        {
            StatusText.Text = "状态: 正在生成地图...";

            // 生成噪声地图
            _noiseMap = GenerateNoiseMap(MAP_SIZE, MAP_SIZE);

            // 初始化地图瓦片数组
            _mapTiles = new Rectangle[MAP_SIZE, MAP_SIZE];

            // 清空画布
            GameCanvas.Children.Clear();

            // 计算地图在Canvas中的偏移量（居中显示）
            var mapOffsetX = (CANVAS_SIZE - WORLD_SIZE) / 2;
            var mapOffsetY = (CANVAS_SIZE - WORLD_SIZE) / 2;

            // 生成地图瓦片
            for (int x = 0; x < MAP_SIZE; x++)
            {
                for (int y = 0; y < MAP_SIZE; y++)
                {
                    var tile = new Rectangle
                    {
                        Width = TILE_SIZE,
                        Height = TILE_SIZE,
                        Fill = GetTerrainColor(_noiseMap[x, y]),
                        Stroke = null // 不显示边框以提高性能
                    };

                    Canvas.SetLeft(tile, mapOffsetX + x * TILE_SIZE);
                    Canvas.SetTop(tile, mapOffsetY + y * TILE_SIZE);

                    GameCanvas.Children.Add(tile);
                    _mapTiles[x, y] = tile;
                }
            }

            StatusText.Text = "状态: 地图生成完成";
        }

        private float[,] GenerateNoiseMap(int width, int height)
        {
            var noiseMap = new float[width, height];
            var random = new Random();

            // 简单的柏林噪声实现
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noise = 0f;
                    float amplitude = 1f;
                    float frequency = 0.01f;

                    // 多层噪声
                    for (int octave = 0; octave < 4; octave++)
                    {
                        float sampleX = x * frequency;
                        float sampleY = y * frequency;

                        // 简化的噪声函数
                        float noiseValue = (float)(Math.Sin(sampleX) * Math.Cos(sampleY) +
                                                  Math.Sin(sampleX * 2) * Math.Cos(sampleY * 2) * 0.5f +
                                                  random.NextDouble() * 0.1f);

                        noise += noiseValue * amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2f;
                    }

                    // 标准化到 0-1 范围
                    noiseMap[x, y] = (noise + 1f) / 2f;
                }
            }

            return noiseMap;
        }

        private Brush GetTerrainColor(float noiseValue)
        {
            // 将噪声值映射到四种地形类型
            if (noiseValue < 0.25f)
                return _terrainColors[0]; // 黑色 - 深水/岩石
            else if (noiseValue < 0.5f)
                return _terrainColors[1]; // 浅黄色 - 沙地/沙漠
            else if (noiseValue < 0.75f)
                return _terrainColors[2]; // 浅绿色 - 草地/森林
            else
                return _terrainColors[3]; // 白色 - 雪地/高山
        }

        private void FpsTimer_Tick(object? sender, EventArgs e)
        {
            FpsText.Text = $"FPS: {_frameCount}";
            _frameCount = 0;
        }

        private void UpdateUI()
        {
            var elapsed = DateTime.Now - _gameStartTime;
            TimeText.Text = $"游戏时间: {elapsed:hh\\:mm\\:ss}";

            if (_characterController != null)
            {
                var (position, status) = _characterController.GetCharacterInfo();
                CharacterInfoText.Text = $"数量: 1个\n位置: ({(int)position.X}, {(int)position.Y})\n状态: {status}";
            }

            // 更新相机信息
            UpdateCameraInfo();
        }

        private void UpdateCameraInfo()
        {
            if (_cameraController != null)
            {
                var (x, y, zoom, following) = _cameraController.GetCameraInfo();
                var runningStatus = _characterController?.IsGameRunning == true ? "运行中" : "暂停";
                StatusText.Text = $"状态: {runningStatus} | 缩放: {zoom:F1}x | 视角: ({x}, {y}) | 跟随: {(following ? "开" : "关")}";
            }
        }

        // 控制器事件处理
        private void OnZoomChanged(double newZoom)
        {
            UpdateCameraInfo();
            _frameCount++;
        }

        private void OnFollowModeChanged()
        {
            UpdateCameraInfo();
            UpdateFollowButtonText();
        }

        private void OnCharacterMoved(Point position)
        {
            // 如果启用跟随模式，移动相机
            if (_cameraController?.FollowCharacter == true)
            {
                _cameraController.FollowPosition(position);
            }
            UpdateUI();
        }

        private void OnCharacterPositionChanged(Point position)
        {
            // 实时位置更新（用于平滑跟随）
            _frameCount++;
        }

        private void UpdateFollowButtonText()
        {
            if (_cameraController != null)
            {
                FollowCharacterButton.Content = _cameraController.FollowCharacter ? "🔓 取消跟随" : "👤 跟随人物";
            }
        }

        // 键盘事件处理
        private void GameWorldWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // ESC键优先处理，无论相机控制是否启用
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                ToggleEscMenu();
                e.Handled = true;
                return;
            }

            if (!_isEscMenuVisible)
            {
                _cameraController?.HandleKeyDown(e.Key);
                e.Handled = true;
            }
        }

        private void GameWorldWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isEscMenuVisible)
            {
                _cameraController?.HandleKeyUp(e.Key);
            }
        }

        private void ToggleEscMenu()
        {
            _isEscMenuVisible = !_isEscMenuVisible;
            EscMenuPanel.Visibility = _isEscMenuVisible ? Visibility.Visible : Visibility.Collapsed;

            // 当显示菜单时，清空按键状态以停止移动
            if (_isEscMenuVisible)
            {
                _cameraController?.ClearKeyState();
            }

            // 禁用/启用相机控制
            if (_cameraController != null)
            {
                _cameraController.CameraControlEnabled = !_isEscMenuVisible;
            }
        }

        // 按钮事件处理器
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _cameraController?.Stop();
            _characterController?.Stop();
            _fpsTimer?.Stop();
            this.Close();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_characterController != null)
            {
                _characterController.IsGameRunning = true;
                StartButton.IsEnabled = false;
                PauseButton.IsEnabled = true;
                StatusText.Text = "状态: 游戏运行中";
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_characterController != null)
            {
                _characterController.IsGameRunning = false;
                StartButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
                StatusText.Text = "状态: 游戏已暂停";
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (_characterController != null)
            {
                _characterController.IsGameRunning = false;
                StartButton.IsEnabled = true;
                PauseButton.IsEnabled = false;

                // 重新生成地图和重置人物位置
                _characterController.ResetCharacterPosition();
                GenerateMap();
                _characterController.CreateCharacter();
                _cameraController?.CenterView();

                StatusText.Text = "状态: 游戏已重置";
            }
        }

        private void CenterViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cameraController != null)
            {
                _cameraController.FollowCharacter = false;
                _cameraController.CenterView();
            }
        }

        private void FollowCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_cameraController != null)
            {
                _cameraController.FollowCharacter = !_cameraController.FollowCharacter;

                if (_cameraController.FollowCharacter && _characterController != null)
                {
                    _cameraController.FollowPosition(_characterController.CharacterPosition);
                }
            }
        }

        private void ExitFullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cameraController?.Stop();
            _characterController?.Stop();
            _fpsTimer?.Stop();
            base.OnClosed(e);
        }
    }
}