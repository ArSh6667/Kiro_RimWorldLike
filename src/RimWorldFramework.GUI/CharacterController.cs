using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// 人物控制器 - 负责处理人物的创建、移动和渲染
    /// </summary>
    public class CharacterController
    {
        // 地图配置常量
        private const int MAP_SIZE = 256;
        private const int TILE_SIZE = 32;
        private const int WORLD_SIZE = MAP_SIZE * TILE_SIZE;
        private const int CANVAS_SIZE = 16384;

        // 人物移动常量
        private const double CHARACTER_MOVE_SPEED = 2.0;
        private const double CHARACTER_UPDATE_INTERVAL = 16;

        // UI控件引用
        private readonly Canvas _canvas;

        // 人物状态
        private Ellipse? _character;
        private Point _characterPosition;
        private Point _characterPixelPosition;
        private Point _targetCharacterPosition;
        private bool _isCharacterMoving = false;
        private readonly Random _random;

        // 移动控制
        private readonly DispatcherTimer _characterMoveTimer;
        private readonly DispatcherTimer _gameTimer;
        private bool _isGameRunning = false;

        // 事件
        public event Action<Point>? CharacterMoved;
        public event Action<Point>? CharacterPositionChanged;

        public CharacterController(Canvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            _random = new Random();

            // 初始化人物移动定时器
            _characterMoveTimer = new DispatcherTimer();
            _characterMoveTimer.Interval = TimeSpan.FromMilliseconds(CHARACTER_UPDATE_INTERVAL);
            _characterMoveTimer.Tick += CharacterMoveTimer_Tick;
            _characterMoveTimer.Start();

            // 初始化游戏逻辑定时器
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(100); // 10 FPS 游戏逻辑
            _gameTimer.Tick += GameTimer_Tick;

            // 初始化人物位置
            ResetCharacterPosition();
        }

        // 属性
        public Point CharacterPosition => _characterPosition;
        public bool IsGameRunning 
        { 
            get => _isGameRunning; 
            set 
            { 
                _isGameRunning = value;
                if (_isGameRunning)
                    _gameTimer.Start();
                else
                    _gameTimer.Stop();
            } 
        }
        public bool IsCharacterMoving => _isCharacterMoving;

        /// <summary>
        /// 创建人物
        /// </summary>
        public void CreateCharacter()
        {
            // 移除旧的人物（如果存在）
            if (_character != null)
            {
                _canvas.Children.Remove(_character);
            }

            // 创建新的人物
            _character = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 2
            };

            UpdateCharacterPosition();
            _canvas.Children.Add(_character);
        }

        /// <summary>
        /// 重置人物位置到地图中心
        /// </summary>
        public void ResetCharacterPosition()
        {
            _characterPosition = new Point(MAP_SIZE / 2, MAP_SIZE / 2);
            _targetCharacterPosition = _characterPosition;
            _isCharacterMoving = false;

            if (_character != null)
            {
                UpdateCharacterPosition();
            }
        }

        /// <summary>
        /// 手动移动人物到指定位置
        /// </summary>
        public void MoveCharacterTo(Point gridPosition)
        {
            if (gridPosition.X >= 0 && gridPosition.X < MAP_SIZE && 
                gridPosition.Y >= 0 && gridPosition.Y < MAP_SIZE)
            {
                if (!_isCharacterMoving)
                {
                    _targetCharacterPosition = gridPosition;
                    _isCharacterMoving = true;
                }
            }
        }

        /// <summary>
        /// 获取人物信息
        /// </summary>
        public (Point Position, string Status) GetCharacterInfo()
        {
            var status = _isGameRunning ? "随机移动" : "静止";
            return (_characterPosition, status);
        }

        /// <summary>
        /// 停止人物控制器
        /// </summary>
        public void Stop()
        {
            _characterMoveTimer?.Stop();
            _gameTimer?.Stop();
        }

        // 私有方法
        private void UpdateCharacterPosition()
        {
            if (_character == null) return;

            var mapOffsetX = (CANVAS_SIZE - WORLD_SIZE) / 2;
            var mapOffsetY = (CANVAS_SIZE - WORLD_SIZE) / 2;

            // 如果不在移动中，使用格子位置计算像素位置
            if (!_isCharacterMoving)
            {
                _characterPixelPosition.X = mapOffsetX + _characterPosition.X * TILE_SIZE + TILE_SIZE / 2 - _character.Width / 2;
                _characterPixelPosition.Y = mapOffsetY + _characterPosition.Y * TILE_SIZE + TILE_SIZE / 2 - _character.Height / 2;
            }

            // 使用当前像素位置设置人物位置
            Canvas.SetLeft(_character, _characterPixelPosition.X);
            Canvas.SetTop(_character, _characterPixelPosition.Y);
        }

        private void MoveCharacterRandomly()
        {
            // 如果人物正在移动，不开始新的移动
            if (_isCharacterMoving) return;

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
                // 设置目标位置并开始平滑移动
                _targetCharacterPosition = new Point(newX, newY);
                _isCharacterMoving = true;
            }
        }

        private void CharacterMoveTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isCharacterMoving || _character == null) return;

            // 计算地图在Canvas中的偏移量
            var mapOffsetX = (CANVAS_SIZE - WORLD_SIZE) / 2;
            var mapOffsetY = (CANVAS_SIZE - WORLD_SIZE) / 2;

            // 计算目标像素位置
            var targetPixelX = mapOffsetX + _targetCharacterPosition.X * TILE_SIZE + TILE_SIZE / 2 - _character.Width / 2;
            var targetPixelY = mapOffsetY + _targetCharacterPosition.Y * TILE_SIZE + TILE_SIZE / 2 - _character.Height / 2;

            // 计算移动方向
            var deltaX = targetPixelX - _characterPixelPosition.X;
            var deltaY = targetPixelY - _characterPixelPosition.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // 如果已经到达目标位置
            if (distance <= CHARACTER_MOVE_SPEED)
            {
                // 直接设置到目标位置
                _characterPixelPosition.X = targetPixelX;
                _characterPixelPosition.Y = targetPixelY;
                _characterPosition = _targetCharacterPosition;
                _isCharacterMoving = false;

                // 触发移动完成事件
                CharacterMoved?.Invoke(_characterPosition);
            }
            else
            {
                // 按固定速度向目标移动
                var moveX = (deltaX / distance) * CHARACTER_MOVE_SPEED;
                var moveY = (deltaY / distance) * CHARACTER_MOVE_SPEED;

                _characterPixelPosition.X += moveX;
                _characterPixelPosition.Y += moveY;

                // 触发位置变化事件（用于实时跟随）
                CharacterPositionChanged?.Invoke(_characterPosition);
            }

            // 更新人物显示位置
            UpdateCharacterPosition();
        }

        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isGameRunning) return;

            // 移动人物
            MoveCharacterRandomly();
        }
    }
}