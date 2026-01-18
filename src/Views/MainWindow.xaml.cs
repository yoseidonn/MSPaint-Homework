using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Core;
using MSPaint.Tools;
using MSPaint.ViewModels;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;

namespace MSPaint.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = DataContext as MainViewModel;
            
            if (_viewModel != null)
            {
                CanvasControl.SetViewModel(_viewModel.Canvas);
            }

            // Setup keyboard shortcuts
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private async void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_viewModel == null) return;

            var modifiers = Keyboard.Modifiers;
            
            // Ctrl+Z: Undo
            if (e.Key == Key.Z && modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                await _viewModel.Canvas.Undo();
                return;
            }
            
            // Ctrl+Shift+Z or Ctrl+Y: Redo
            if ((e.Key == Key.Z && modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ||
                (e.Key == Key.Y && modifiers == ModifierKeys.Control))
            {
                e.Handled = true;
                await _viewModel.Canvas.Redo();
                return;
            }
        }

        private void ToolButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is WpfButton button && button.Tag is string toolName)
            {
                _viewModel.SelectTool(toolName);
            }
        }

        private void CanvasControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null || _viewModel.CurrentTool is not TextTool) return;
            
            // Get position relative to the CanvasControl
            var canvasControl = sender as Canvas.CanvasControl;
            if (canvasControl == null) return;
            
            // Get position relative to the CanvasControl itself
            var position = e.GetPosition(canvasControl);
            
            // Convert screen coordinates to pixel grid coordinates
            // The Image is scaled by PixelSize, so we need to divide by PixelSize
            int gridX = (int)(position.X / _viewModel.Canvas.PixelSize);
            int gridY = (int)(position.Y / _viewModel.Canvas.PixelSize);
            
            // Clamp to grid bounds
            if (_viewModel.Canvas.PixelGrid != null)
            {
                gridX = Math.Max(0, Math.Min(gridX, _viewModel.Canvas.PixelGrid.Width - 1));
                gridY = Math.Max(0, Math.Min(gridY, _viewModel.Canvas.PixelGrid.Height - 1));
            }
            
            _viewModel.HandleTextToolClick(gridX, gridY);
        }

        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is WpfButton button && button.Tag is string colorName)
            {
                var color = ColorPalette.GetColorByName(colorName);
                _viewModel.SelectColor(color, true);
            }
        }

        private void PrimaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;
            ShowColorPicker(true);
        }

        private void SecondaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null) return;
            ShowColorPicker(false);
        }

        private void ShowColorPicker(bool isPrimary)
        {
            if (_viewModel == null) return;

            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(
                    isPrimary ? _viewModel.PrimaryColor.A : _viewModel.SecondaryColor.A,
                    isPrimary ? _viewModel.PrimaryColor.R : _viewModel.SecondaryColor.R,
                    isPrimary ? _viewModel.PrimaryColor.G : _viewModel.SecondaryColor.G,
                    isPrimary ? _viewModel.PrimaryColor.B : _viewModel.SecondaryColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = colorDialog.Color;
                var mediaColor = WpfColor.FromArgb(color.A, color.R, color.G, color.B);
                _viewModel.SelectColor(mediaColor, isPrimary);
            }
        }
    }
}
