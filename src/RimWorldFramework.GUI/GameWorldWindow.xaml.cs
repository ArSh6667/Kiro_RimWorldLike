using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// GameWorldWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameWorldWindow : Window
    {
        // 地图配置
        private const int MAP_SIZE = 256;           // 地图格子数量 256x256
        private const int TILE_SIZE = 32;           // 每格像素大小 32x32
        private const int WORLD_SIZE = MAP_SIZE * TILE_SIZE; // 总像素大小 8192x8192

        // 游戏状态
        private DispatcherTimer _gameTimer;
        private DispatcherTimer _fpsTimer;
        private DateTime _gameStartTime;
        private bool _isGameRunning = false;
        private bool _followCharacter = false;
        private int _frameCount = 0;
        private DateTime _lastFpsUpdate = DateTime.Now;

        // 地图数据
        private Rectangle[,] _mapTiles;
        private float[,] _noiseMap;

        // 人物
        private Ellipse _character;
        private Point _characterPosition;
        private Random _random;

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
            _random = new Random();
            InitializeGame();
        }

        private void InitializeGame()
        {
            _gameStartTime = DateTime.Now;
            _characterPosition = new Point(MAP_SIZE / 2, MAP_SIZE / 2); // 起始位置在地图中心

            // 初始化定时器
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(100); // 10 FPS 游戏逻辑
            _gameTimer.Tick += GameTimer_Tick;

            _fpsTimer = new DispatcherTimer();
            _fpsTimer.Interval = TimeSpan.FromSeconds(1);
            _fpsTimer.Tick += FpsTimer_Tick;
            _fpsTimer.Start();

            // 生成地图
            GenerateMap();
            
            // 创建人物
            CreateCharacter();
            
            // 居中视图
            CenterView();

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
                    
                    Canvas.SetLeft(tile, x * TILE_SIZE);
                    Canvas.SetTop(tile, y * TILE_SIZE);
                    
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

        private void CreateCharacter()
        {
            _character = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };
            
            UpdateCharacterPosition();
            GameCanvas.Children.Add(_character);
        }

        private void UpdateCharacterPosition()
        {
            double pixelX = _characterPosition.X * TILE_SIZE + TILE_SIZE / 2 - _character.Width / 2;
            double pixelY = _characterPosition.Y * TILE_SIZE + TILE_SIZE / 2 - _character.Height / 2;
            
            Canvas.SetLeft(_character, pixelX);
            Canvas.SetTop(_character, pixelY);
        }

        private void MoveCharacterRandomly()
        {
            // 随机选择移动方向
            var directions = new Point[]
            {
                new Point(0, -1),  // 上
                new Point(1, 0),   // 右
                new Point(0, 1),   // 下
                new Point(-1, 0),  // 左
                new Point(1, -1),  // 右上
                new Point(1, 1),   // 右下
                new Point(-1, 1),  // 左下
                new Point(-1, -1), // 左上
                new Point(0, 0)    // 停留
            };
            
            var direction = directions[_random.Next(directions.Length)];
            var newX = _characterPosition.X + direction.X;
            var newY = _characterPosition.Y + direction.Y;
            
            // 边界检查
            if (newX >= 0 && newX < MAP_SIZE && newY >= 0 && newY < MAP_SIZE)
            {
                _characterPosition.X = newX;
                _characterPosition.Y = newY;
                UpdateCharacterPosition();
                
                // 如果启用跟随模式，移动视图
                if (_followCharacter)
                {
                    FollowCharacter();
                }
            }
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isGameRunning) return;
            
            // 移动人物
            MoveCharacterRandomly();
            
            // 更新UI
            UpdateUI();
            
            // 计算FPS
            _frameCount++;
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
            
            CharacterInfoText.Text = $"数量: 1个\n位置: ({(int)_characterPosition.X}, {(int)_characterPosition.Y})\n状态: {(_isGameRunning ? "随机移动" : "静止")}";
        }

        private void CenterView()
        {
            // 将视图居中到地图中心
            var centerX = WORLD_SIZE / 2 - MapScrollViewer.ViewportWidth / 2;
            var centerY = WORLD_SIZE / 2 - MapScrollViewer.ViewportHeight / 2;
            
            MapScrollViewer.ScrollToHorizontalOffset(Math.Max(0, centerX));
            MapScrollViewer.ScrollToVerticalOffset(Math.Max(0, centerY));
        }

        private void FollowCharacter()
        {
            // 将视图跟随人物
            var characterPixelX = _characterPosition.X * TILE_SIZE;
            var characterPixelY = _characterPosition.Y * TILE_SIZE;
            
            var targetX = characterPixelX - MapScrollViewer.ViewportWidth / 2;
            var targetY = characterPixelY - MapScrollViewer.ViewportHeight / 2;
            
            MapScrollViewer.ScrollToHorizontalOffset(Math.Max(0, targetX));
            MapScrollViewer.ScrollToVerticalOffset(Math.Max(0, targetY));
        }

        // 事件处理器
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _gameTimer?.Stop();
            _fpsTimer?.Stop();
            this.Close();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _isGameRunning = true;
            _gameTimer.Start();
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            StatusText.Text = "状态: 游戏运行中";
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _isGameRunning = false;
            _gameTimer.Stop();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            StatusText.Text = "状态: 游戏已暂停";
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _isGameRunning = false;
            _gameTimer.Stop();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            
            // 重新生成地图和重置人物位置
            _characterPosition = new Point(MAP_SIZE / 2, MAP_SIZE / 2);
            GenerateMap();
            CreateCharacter();
            CenterView();
            
            StatusText.Text = "状态: 游戏已重置";
        }

        private void CenterViewButton_Click(object sender, RoutedEventArgs e)
        {
            _followCharacter = false;
            CenterView();
        }

        private void FollowCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            _followCharacter = !_followCharacter;
            FollowCharacterButton.Content = _followCharacter ? "🔓 取消跟随" : "👤 跟随人物";
            
            if (_followCharacter)
            {
                FollowCharacter();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();
            _fpsTimer?.Stop();
            base.OnClosed(e);
        }
    }
}