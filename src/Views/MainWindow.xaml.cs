using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Core;
using MSPaint.ViewModels;

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

        private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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
            if (sender is Button button && button.Tag is string toolName)
            {
                _viewModel.SelectTool(toolName);
            }
        }

        private void CanvasControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel == null || _viewModel.CurrentTool is not TextTool) return;
            
            var position = e.GetPosition(CanvasControl);
            int gridX = (int)position.X / _viewModel.Canvas.PixelSize;
            int gridY = (int)position.Y / _viewModel.Canvas.PixelSize;
            _viewModel.HandleTextToolClick(gridX, gridY);
        }

        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            if (sender is Button button && button.Tag is string colorName)
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
                var mediaColor = Color.FromArgb(color.A, color.R, color.G, color.B);
                _viewModel.SelectColor(mediaColor, isPrimary);
            }
        }
    }
}
