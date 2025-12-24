using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace RimWorldFramework.GUI
{
    /// <summary>
    /// 相机控制器 - 负责处理所有相机相关的操作
    /// </summary>
    public class CameraController
    {
        // 地图配置常量
        private const int MAP_SIZE = 256;
        private const int TILE_SIZE = 32;
        private const int WORLD_SIZE = MAP_SIZE * TILE_SIZE;
        private const int CANVAS_SIZE = 16384;

        // 相机控制常量
        private const double CAMERA_ZOOM_SPEED = 0.1;
        private const double MIN_ZOOM = 0.4;
        private const double MAX_ZOOM = 5.0;
        private const double CAMERA_UPDATE_INTERVAL = 16;
        private const double SMOOTH_MOVE_SPEED = 18.0;

        // UI控件引用
        private readonly ScrollViewer _scrollViewer;
        private readonly ScaleTransform _scaleTransform;

        // 相机状态
        private double _currentZoom = 1.0;
        private bool _cameraControlEnabled = true;
        private bool _followCharacter = false;

        // 键盘控制
        private readonly DispatcherTimer _cameraTimer;
        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();

        // 鼠标拖动控制
        private bool _isMouseDragging = false;
        private Point _lastMousePosition;

        // 事件
        public event Action<double>? ZoomChanged;
        public event Action? FollowModeChanged;

        public CameraController(ScrollViewer scrollViewer, ScaleTransform scaleTransform)
        {
            _scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
            _scaleTransform = scaleTransform ?? throw new ArgumentNullException(nameof(scaleTransform));

            // 初始化相机移动定时器
            _cameraTimer = new DispatcherTimer();
            _cameraTimer.Interval = TimeSpan.FromMilliseconds(CAMERA_UPDATE_INTERVAL);
            _cameraTimer.Tick += CameraTimer_Tick;
            _cameraTimer.Start();

            // 绑定鼠标事件
            _scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            _scrollViewer.MouseDown += ScrollViewer_MouseDown;
            _scrollViewer.MouseUp += ScrollViewer_MouseUp;
            _scrollViewer.MouseMove += ScrollViewer_MouseMove;
            _scrollViewer.MouseLeave += ScrollViewer_MouseLeave;
        }

        // 属性
        public double CurrentZoom => _currentZoom;
        public bool CameraControlEnabled 
        { 
            get => _cameraControlEnabled; 
            set => _cameraControlEnabled = value; 
        }
        public bool FollowCharacter 
        { 
            get => _followCharacter; 
            set 
            { 
                if (_followCharacter != value)
                {
                    _followCharacter = value;
                    FollowModeChanged?.Invoke();
                }
            } 
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        public void HandleKeyDown(Key key)
        {
            if (!_cameraControlEnabled) return;

            _pressedKeys.Add(key);

            // 处理非移动键的即时响应
            switch (key)
            {
                case Key.Space:
                    CenterView();
                    break;
                case Key.R:
                    ResetCamera();
                    break;
                case Key.F:
                    FollowCharacter = !FollowCharacter;
                    break;
            }
        }

        /// <summary>
        /// 处理键盘释放事件
        /// </summary>
        public void HandleKeyUp(Key key)
        {
            _pressedKeys.Remove(key);
        }

        /// <summary>
        /// 清空按键状态
        /// </summary>
        public void ClearKeyState()
        {
            _pressedKeys.Clear();
        }

        /// <summary>
        /// 居中视图
        /// </summary>
        public void CenterView()
        {
            // 强制更新ScrollViewer信息
            UpdateScrollInfo();
            
            var centerX = CANVAS_SIZE / 2 * _currentZoom - _scrollViewer.ViewportWidth / 2;
            var centerY = CANVAS_SIZE / 2 * _currentZoom - _scrollViewer.ViewportHeight / 2;

            var clampedPosition = ClampCameraPosition(centerX, centerY);
            _scrollViewer.ScrollToHorizontalOffset(clampedPosition.X);
            _scrollViewer.ScrollToVerticalOffset(clampedPosition.Y);
        }

        /// <summary>
        /// 跟随指定位置
        /// </summary>
        public void FollowPosition(Point gridPosition)
        {
            // 强制更新ScrollViewer信息
            UpdateScrollInfo();
            
            var mapOffsetX = (CANVAS_SIZE - WORLD_SIZE) / 2;
            var mapOffsetY = (CANVAS_SIZE - WORLD_SIZE) / 2;

            var targetPixelX = (mapOffsetX + gridPosition.X * TILE_SIZE) * _currentZoom;
            var targetPixelY = (mapOffsetY + gridPosition.Y * TILE_SIZE) * _currentZoom;

            var targetX = targetPixelX - _scrollViewer.ViewportWidth / 2;
            var targetY = targetPixelY - _scrollViewer.ViewportHeight / 2;

            var clampedPosition = ClampCameraPosition(targetX, targetY);
            _scrollViewer.ScrollToHorizontalOffset(clampedPosition.X);
            _scrollViewer.ScrollToVerticalOffset(clampedPosition.Y);
        }

        /// <summary>
        /// 重置相机
        /// </summary>
        public void ResetCamera()
        {
            _currentZoom = 1.0;
            _scaleTransform.ScaleX = _currentZoom;
            _scaleTransform.ScaleY = _currentZoom;
            
            // 强制ScrollViewer更新
            _scrollViewer.InvalidateScrollInfo();
            _scrollViewer.UpdateLayout();
            
            ZoomChanged?.Invoke(_currentZoom);
            CenterView();
        }

        /// <summary>
        /// 强制更新ScrollViewer的滚动信息
        /// </summary>
        private void UpdateScrollInfo()
        {
            _scrollViewer.InvalidateScrollInfo();
            _scrollViewer.UpdateLayout();
        }

        /// <summary>
        /// 获取相机信息
        /// </summary>
        public (int X, int Y, double Zoom, bool Following) GetCameraInfo()
        {
            var cameraX = (int)(_scrollViewer.HorizontalOffset / TILE_SIZE);
            var cameraY = (int)(_scrollViewer.VerticalOffset / TILE_SIZE);
            return (cameraX, cameraY, _currentZoom, _followCharacter);
        }

        /// <summary>
        /// 停止相机控制器
        /// </summary>
        public void Stop()
        {
            _cameraTimer?.Stop();
        }

        // 私有方法
        private void CameraTimer_Tick(object? sender, EventArgs e)
        {
            if (!_cameraControlEnabled || _pressedKeys.Count == 0) return;

            var currentHorizontalOffset = _scrollViewer.HorizontalOffset;
            var currentVerticalOffset = _scrollViewer.VerticalOffset;
            var moved = false;
            var newX = currentHorizontalOffset;
            var newY = currentVerticalOffset;

            // 检查移动键并计算新位置
            if (_pressedKeys.Contains(Key.W) || _pressedKeys.Contains(Key.Up))
            {
                newY = currentVerticalOffset - SMOOTH_MOVE_SPEED;
                moved = true;
            }

            if (_pressedKeys.Contains(Key.S) || _pressedKeys.Contains(Key.Down))
            {
                newY = currentVerticalOffset + SMOOTH_MOVE_SPEED;
                moved = true;
            }

            if (_pressedKeys.Contains(Key.A) || _pressedKeys.Contains(Key.Left))
            {
                newX = currentHorizontalOffset - SMOOTH_MOVE_SPEED;
                moved = true;
            }

            if (_pressedKeys.Contains(Key.D) || _pressedKeys.Contains(Key.Right))
            {
                newX = currentHorizontalOffset + SMOOTH_MOVE_SPEED;
                moved = true;
            }

            if (moved)
            {
                var clampedPosition = ClampCameraPosition(newX, newY);

                if (Math.Abs(clampedPosition.X - currentHorizontalOffset) > 0.1 ||
                    Math.Abs(clampedPosition.Y - currentVerticalOffset) > 0.1)
                {
                    _scrollViewer.ScrollToHorizontalOffset(clampedPosition.X);
                    _scrollViewer.ScrollToVerticalOffset(clampedPosition.Y);

                    // 如果手动移动相机，关闭跟随模式
                    FollowCharacter = false;
                }
            }
        }

        private Point ClampCameraPosition(double targetX, double targetY)
        {
            // 计算地图在Canvas中的偏移量（未缩放）
            var mapOffsetX = (CANVAS_SIZE - WORLD_SIZE) / 2;
            var mapOffsetY = (CANVAS_SIZE - WORLD_SIZE) / 2;

            // 计算缩放后的Canvas总尺寸
            var scaledCanvasWidth = CANVAS_SIZE * _currentZoom;
            var scaledCanvasHeight = CANVAS_SIZE * _currentZoom;

            // 计算缩放后的地图边界
            var scaledMapOffsetX = mapOffsetX * _currentZoom;
            var scaledMapOffsetY = mapOffsetY * _currentZoom;
            var scaledWorldSizeX = WORLD_SIZE * _currentZoom;
            var scaledWorldSizeY = WORLD_SIZE * _currentZoom;

            // 计算可滚动区域的边界
            // 最小值：地图左上角
            var minHorizontalOffset = scaledMapOffsetX;
            var minVerticalOffset = scaledMapOffsetY;
            
            // 最大值：地图右下角减去视口大小
            var maxHorizontalOffset = scaledMapOffsetX + scaledWorldSizeX - _scrollViewer.ViewportWidth;
            var maxVerticalOffset = scaledMapOffsetY + scaledWorldSizeY - _scrollViewer.ViewportHeight;

            // 确保最大值不小于最小值（当地图小于视口时）
            maxHorizontalOffset = Math.Max(maxHorizontalOffset, minHorizontalOffset);
            maxVerticalOffset = Math.Max(maxVerticalOffset, minVerticalOffset);

            // 应用边界限制
            var clampedX = Math.Max(minHorizontalOffset, Math.Min(maxHorizontalOffset, targetX));
            var clampedY = Math.Max(minVerticalOffset, Math.Min(maxVerticalOffset, targetY));

            // 如果目标位置与限制后的位置差异很小，直接使用目标位置（避免微小跳动）
            const double snapThreshold = 1.0;
            if (Math.Abs(targetX - clampedX) < snapThreshold)
                clampedX = targetX;
            if (Math.Abs(targetY - clampedY) < snapThreshold)
                clampedY = targetY;

            return new Point(clampedX, clampedY);
        }

        // 鼠标事件处理
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_cameraControlEnabled) return;

            e.Handled = true;

            var scaleFactor = e.Delta > 0 ? (1.0 + CAMERA_ZOOM_SPEED) : (1.0 - CAMERA_ZOOM_SPEED);
            var newZoom = _currentZoom * scaleFactor;

            if (newZoom >= MIN_ZOOM && newZoom <= MAX_ZOOM)
            {
                // 获取鼠标在ScrollViewer中的位置
                var mousePosition = e.GetPosition(_scrollViewer);
                
                // 计算鼠标在Canvas坐标系中的位置（缩放前）
                var mouseCanvasX = (_scrollViewer.HorizontalOffset + mousePosition.X) / _currentZoom;
                var mouseCanvasY = (_scrollViewer.VerticalOffset + mousePosition.Y) / _currentZoom;

                // 保存旧的缩放值
                var oldZoom = _currentZoom;
                
                // 应用新的缩放
                _currentZoom = newZoom;
                _scaleTransform.ScaleX = _currentZoom;
                _scaleTransform.ScaleY = _currentZoom;

                // 强制ScrollViewer更新其内容大小
                _scrollViewer.InvalidateScrollInfo();
                _scrollViewer.UpdateLayout();

                // 计算新的滚动位置，使鼠标位置保持不变
                var newHorizontalOffset = mouseCanvasX * _currentZoom - mousePosition.X;
                var newVerticalOffset = mouseCanvasY * _currentZoom - mousePosition.Y;

                // 应用边界限制
                var clampedPosition = ClampCameraPosition(newHorizontalOffset, newVerticalOffset);
                
                // 使用ScrollToHorizontalOffset和ScrollToVerticalOffset进行平滑滚动
                _scrollViewer.ScrollToHorizontalOffset(clampedPosition.X);
                _scrollViewer.ScrollToVerticalOffset(clampedPosition.Y);

                ZoomChanged?.Invoke(_currentZoom);
            }
        }

        private void ScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_cameraControlEnabled) return;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isMouseDragging = true;
                _lastMousePosition = e.GetPosition(_scrollViewer);
                _scrollViewer.CaptureMouse();
                _scrollViewer.Cursor = Cursors.SizeAll;
                e.Handled = true;
            }
        }

        private void ScrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && _isMouseDragging)
            {
                _isMouseDragging = false;
                _scrollViewer.ReleaseMouseCapture();
                _scrollViewer.Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_cameraControlEnabled || !_isMouseDragging) return;

            var currentMousePosition = e.GetPosition(_scrollViewer);
            var deltaX = currentMousePosition.X - _lastMousePosition.X;
            var deltaY = currentMousePosition.Y - _lastMousePosition.Y;

            var newHorizontalOffset = _scrollViewer.HorizontalOffset - deltaX;
            var newVerticalOffset = _scrollViewer.VerticalOffset - deltaY;

            var clampedPosition = ClampCameraPosition(newHorizontalOffset, newVerticalOffset);
            _scrollViewer.ScrollToHorizontalOffset(clampedPosition.X);
            _scrollViewer.ScrollToVerticalOffset(clampedPosition.Y);

            _lastMousePosition = currentMousePosition;
            FollowCharacter = false;

            e.Handled = true;
        }

        private void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isMouseDragging)
            {
                _isMouseDragging = false;
                _scrollViewer.ReleaseMouseCapture();
                _scrollViewer.Cursor = Cursors.Arrow;
            }
        }
    }
}