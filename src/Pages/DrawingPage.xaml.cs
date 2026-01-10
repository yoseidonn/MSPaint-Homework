using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MSPaint.Controls;
using MSPaint.Models;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace MSPaint.Pages
{
    public partial class DrawingPage : WpfUserControl
    {
        public DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return CanvasControl; // Reference from XAML
        }

        public DrawingPage()
        {
            InitializeComponent();
            
            // Subscribe to canvas size changes
            if (CanvasControl != null)
            {
                CanvasControl.CanvasSizeChanged += CanvasControl_CanvasSizeChanged;
            }
        }

        private void CanvasControl_CanvasSizeChanged(object? sender, EventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                int width = canvas.PixelGrid.Width * canvas.PixelGrid.PixelSize;
                int height = canvas.PixelGrid.Height * canvas.PixelGrid.PixelSize;
                CanvasBorder.Width = width;
                CanvasBorder.Height = height;
            }
        }

        public async Task InitializeCanvas(CanvasSettings settings)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                await canvas.InitializeCanvas(settings);
                UpdateCanvasBorderSize(settings);
            }
        }

        public async Task InitializeCanvas(PixelGrid grid)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                await canvas.InitializeCanvas(grid);
                // Calculate size from grid
                int width = grid.Width * grid.PixelSize;
                int height = grid.Height * grid.PixelSize;
                CanvasBorder.Width = width;
                CanvasBorder.Height = height;
            }
        }

        private void UpdateCanvasBorderSize(CanvasSettings settings)
        {
            int width = settings.Width * settings.PixelSize;
            int height = settings.Height * settings.PixelSize;
            CanvasBorder.Width = width;
            CanvasBorder.Height = height;
        }
    }
}