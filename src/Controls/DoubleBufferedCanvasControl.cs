using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Models;
using MSPaint.Services;
using MSPaint.Tools;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MSPaint.Controls
{
    public partial class DoubleBufferedCanvasControl : WpfUserControl
    {
        private PixelGrid? _pixelGrid;
        private RenderService _renderService;
        private ITool? _currentTool;
        private bool _isDrawing;
        private System.Windows.Point _lastMousePosition;

        public PixelGrid? PixelGrid => _pixelGrid;

        public DoubleBufferedCanvasControl()
        {
            InitializeComponent();
            _renderService = new RenderService();
            this.Loaded += DoubleBufferedCanvasControl_Loaded;
            
            // Enable mouse capture for smooth drawing
            this.MouseLeftButtonDown += DoubleBufferedCanvasControl_MouseLeftButtonDown;
            this.MouseMove += DoubleBufferedCanvasControl_MouseMove;
            this.MouseLeftButtonUp += DoubleBufferedCanvasControl_MouseLeftButtonUp;
        }

        public void SetTool(ITool tool)
        {
            _currentTool = tool;
        }

        public async Task InitializeCanvas(CanvasSettings settings)
        {
            _pixelGrid = new PixelGrid(settings.Width, settings.Height, settings.PixelSize);
            
            // Initialize all pixels with background color
            for (int y = 0; y < _pixelGrid.Height; y++)
            {
                for (int x = 0; x < _pixelGrid.Width; x++)
                {
                    _pixelGrid.SetPixel(x, y, settings.Background);
                }
            }

            // Initialize default tool
            _currentTool = new PencilTool(_pixelGrid);

            // Render the grid
            await RenderAsync();
        }

        public async Task InitializeCanvas(PixelGrid grid)
        {
            _pixelGrid = grid;
            _currentTool = new PencilTool(_pixelGrid);
            await RenderAsync();
        }

        private void DoubleBufferedCanvasControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Canvas will be initialized from setup window or MainWindow
            // This event handler is kept for compatibility but won't auto-initialize
        }

        private async void DoubleBufferedCanvasControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pixelGrid == null || _currentTool == null) return;

            CaptureMouse();
            _isDrawing = true;
            
            var position = e.GetPosition(this);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                _lastMousePosition = gridPosition.Value;
                _currentTool.OnMouseDown((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
                await RenderAsync();
            }
        }

        private async void DoubleBufferedCanvasControl_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_isDrawing || _pixelGrid == null || _currentTool == null) return;

            var position = e.GetPosition(this);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                var currentPos = gridPosition.Value;
                
                // Only process if position changed (avoid duplicate calls)
                if (currentPos != _lastMousePosition)
                {
                    _currentTool.OnMouseMove((int)currentPos.X, (int)currentPos.Y);
                    _lastMousePosition = currentPos;
                    await RenderAsync();
                }
            }
        }

        private async void DoubleBufferedCanvasControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing || _pixelGrid == null || _currentTool == null) return;

            ReleaseMouseCapture();
            _isDrawing = false;

            var position = e.GetPosition(this);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                _currentTool.OnMouseUp((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
                await RenderAsync();
            }
        }

        private System.Windows.Point? ScreenToGrid(System.Windows.Point screenPoint)
        {
            if (_pixelGrid == null) return null;

            // Convert screen coordinates to grid coordinates
            int gridX = (int)(screenPoint.X / _pixelGrid.PixelSize);
            int gridY = (int)(screenPoint.Y / _pixelGrid.PixelSize);

            // Clamp to grid bounds
            if (gridX < 0 || gridX >= _pixelGrid.Width || gridY < 0 || gridY >= _pixelGrid.Height)
                return null;

            return new System.Windows.Point(gridX, gridY);
        }

        private async Task RenderAsync()
        {
            if (_pixelGrid == null) return;

            try
            {
                // Render on background thread, then update UI on UI thread
                var bitmap = await _renderService.RenderAsync(_pixelGrid);
                
                // Update UI on UI thread
                await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                {
                    BackImage.Source = bitmap;
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error rendering canvas: {ex.Message}",
                    "Rendering Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}