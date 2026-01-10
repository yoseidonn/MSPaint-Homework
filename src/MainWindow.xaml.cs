using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Controls;
using MSPaint.Tools;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Pages.DrawingPage? _drawingPage;
        private MediaColor _primaryColor = MediaColors.Black;
        private MediaColor _secondaryColor = MediaColors.White;

        public MainWindow()
        {
            InitializeComponent();

            // At startup we mount the drawing page control into the CanvasHost
            _drawingPage = new Pages.DrawingPage();
            CanvasHost.Content = _drawingPage;
        }

        public Pages.DrawingPage? GetDrawingPage()
        {
            return _drawingPage;
        }

        private MSPaint.Controls.DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return _drawingPage?.GetCanvasControl();
        }

        private void SetTool(ITool tool)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                canvas.SetTool(tool);
            }
        }

        private void UpdateToolColor(MediaColor color)
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return;

            var currentTool = canvas.GetCurrentTool();
            
            // Update tool color if it has DrawColor or FillColor property
            if (currentTool is PencilTool pencilTool)
                pencilTool.DrawColor = color;
            else if (currentTool is RectangleTool rectTool)
                rectTool.DrawColor = color;
            else if (currentTool is EllipseTool ellipseTool)
                ellipseTool.DrawColor = color;
            else if (currentTool is FillTool fillTool)
                fillTool.FillColor = color;
        }

        // Tool selection handlers
        private void PencilButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new PencilTool(canvas.PixelGrid);
                tool.DrawColor = _primaryColor;
                SetTool(tool);
            }
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new EraserTool(canvas.PixelGrid);
                tool.EraseColor = _secondaryColor;
                SetTool(tool);
            }
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new FillTool(canvas.PixelGrid);
                tool.FillColor = _primaryColor;
                SetTool(tool);
            }
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new RectangleTool(canvas.PixelGrid);
                tool.DrawColor = _primaryColor;
                SetTool(tool);
            }
        }

        private void EllipseButton_Click(object sender, RoutedEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new EllipseTool(canvas.PixelGrid);
                tool.DrawColor = _primaryColor;
                SetTool(tool);
            }
        }

        // Color picker handlers
        private void PrimaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowColorPicker(true);
        }

        private void SecondaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShowColorPicker(false);
        }

        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string colorName)
            {
                MediaColor color = GetColorByName(colorName);
                _primaryColor = color;
                UpdateColorPreview();
                UpdateToolColor(color);
            }
        }

        private void ShowColorPicker(bool isPrimary)
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(
                    isPrimary ? _primaryColor.A : _secondaryColor.A,
                    isPrimary ? _primaryColor.R : _secondaryColor.R,
                    isPrimary ? _primaryColor.G : _secondaryColor.G,
                    isPrimary ? _primaryColor.B : _secondaryColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = colorDialog.Color;
                var mediaColor = MediaColor.FromArgb(color.A, color.R, color.G, color.B);
                
                if (isPrimary)
                {
                    _primaryColor = mediaColor;
                }
                else
                {
                    _secondaryColor = mediaColor;
                }

                UpdateColorPreview();
                if (isPrimary)
                {
                    UpdateToolColor(_primaryColor);
                }
            }
        }

        private void UpdateColorPreview()
        {
            PrimaryColorPreview.Background = new SolidColorBrush(_primaryColor);
            SecondaryColorPreview.Background = new SolidColorBrush(_secondaryColor);
        }

        private MediaColor GetColorByName(string name)
        {
            return name switch
            {
                "Black" => MediaColors.Black,
                "Gray" => MediaColors.Gray,
                "White" => MediaColors.White,
                "Red" => MediaColors.Red,
                "Lime" => MediaColors.Lime,
                "Cyan" => MediaColors.Cyan,
                "Blue" => MediaColors.Blue,
                _ => MediaColors.Black
            };
        }
    }
}