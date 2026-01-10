using System;
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
        private System.Windows.Controls.Button? _selectedToolButton;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // At startup we mount the drawing page control into the CanvasHost
                _drawingPage = new Pages.DrawingPage();
                CanvasHost.Content = _drawingPage;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error initializing MainWindow: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw; // Re-throw to let App.xaml.cs handle it
            }
        }

        public Pages.DrawingPage? GetDrawingPage()
        {
            return _drawingPage;
        }

        public async System.Threading.Tasks.Task InitializeCanvasAsync(Pages.CanvasSetupWindow setupWindow)
        {
            // Wait for window to be fully rendered
            await System.Threading.Tasks.Task.Delay(100);
            
            if (setupWindow.LoadedGrid != null)
            {
                // Load from file
                var page = GetDrawingPage();
                if (page != null)
                {
                    await page.InitializeCanvas(setupWindow.LoadedGrid);
                }
            }
            else if (setupWindow.Result != null)
            {
                // Create new canvas with settings
                var page = GetDrawingPage();
                if (page != null)
                {
                    await page.InitializeCanvas(setupWindow.Result);
                }
            }
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

        private void HighlightToolButton(System.Windows.Controls.Button button)
        {
            // Reset all tool buttons
            EraserButton.Background = new SolidColorBrush(MediaColors.Transparent);
            FillButton.Background = new SolidColorBrush(MediaColors.Transparent);
            PencilButton.Background = new SolidColorBrush(MediaColors.Transparent);
            RectangleButton.Background = new SolidColorBrush(MediaColors.Transparent);
            EllipseButton.Background = new SolidColorBrush(MediaColors.Transparent);

            // Highlight selected button
            if (button != null)
            {
                button.Background = new SolidColorBrush(MediaColors.LightBlue);
                _selectedToolButton = button;
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
                HighlightToolButton(PencilButton);
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
                HighlightToolButton(EraserButton);
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
                HighlightToolButton(FillButton);
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
                HighlightToolButton(RectangleButton);
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
                HighlightToolButton(EllipseButton);
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

        // Keyboard shortcut handling
        private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return;

            // Ctrl+Z: Undo
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await canvas.Undo();
                e.Handled = true;
            }
            // Ctrl+Shift+Z or Ctrl+Y: Redo
            else if ((e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ||
                     (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control))
            {
                await canvas.Redo();
                e.Handled = true;
            }
        }
    }
}