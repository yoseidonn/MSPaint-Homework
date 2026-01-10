using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Models;
using MSPaint.Services;
using System.Threading.Tasks;

namespace MSPaint.Controls
{
    public partial class DoubleBufferedCanvasControl : UserControl
    {
        private PixelGrid? _pixelGrid;
        private RenderService _renderService;

        public DoubleBufferedCanvasControl()
        {
            InitializeComponent();
            _renderService = new RenderService();
            this.Loaded += DoubleBufferedCanvasControl_Loaded;
        }

        private async void DoubleBufferedCanvasControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Initialize with default canvas settings if not already set
                if (_pixelGrid == null)
                {
                    // Default: 64x64 grid with 8px per pixel
                    _pixelGrid = new PixelGrid(64, 64, 8);
                    
                    // Initialize all pixels to white
                    for (int y = 0; y < _pixelGrid.Height; y++)
                    {
                        for (int x = 0; x < _pixelGrid.Width; x++)
                        {
                            _pixelGrid.SetPixel(x, y, Colors.White);
                        }
                    }
                }

                // Render the grid and set it as the back image source
                var bitmap = await _renderService.RenderAsync(_pixelGrid);
                BackImage.Source = bitmap;
            }
            catch (System.Exception ex)
            {
                // Log error and show message box for debugging
                System.Windows.MessageBox.Show(
                    $"Error initializing canvas: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Canvas Initialization Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        // Mouse events will be forwarded to drawing service / tools later
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
        }
    }
}