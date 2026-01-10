using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using MSPaint.Models;
using MSPaint.Services;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Pages
{
    public partial class CanvasSetupWindow : Window
    {
        public CanvasSettings? Result { get; private set; }
        public PixelGrid? LoadedGrid { get; private set; }
        private MediaColor _selectedColor = Colors.White;

        public CanvasSetupWindow()
        {
            InitializeComponent();
            ColorPreview.Background = new SolidColorBrush(_selectedColor);
            UpdateColorPickerEnabled();
        }

        private void UpdateColorPickerEnabled()
        {
            ColorPickerPanel.IsEnabled = TransparentCheckBox.IsChecked != true;
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*",
                Title = "Load Canvas from File"
            };

            if (openDialog.ShowDialog() == true)
            {
                var ioService = new ProjectIOService();
                LoadedGrid = ioService.Load(openDialog.FileName);
                
                if (LoadedGrid != null)
                {
                    // Close dialog and return loaded grid
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to load file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TransparentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _selectedColor = Colors.Transparent;
            ColorPreview.Background = new SolidColorBrush(Colors.Transparent);
            UpdateColorPickerEnabled();
        }

        private void TransparentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_selectedColor == Colors.Transparent)
            {
                _selectedColor = Colors.White;
            }
            ColorPreview.Background = new SolidColorBrush(_selectedColor);
            UpdateColorPickerEnabled();
        }

        private void ColorPreview_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ChooseColor();
        }

        private void ChooseColorButton_Click(object sender, RoutedEventArgs e)
        {
            ChooseColor();
        }

        private void ChooseColor()
        {
            var colorDialog = new System.Windows.Forms.ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(_selectedColor.A, _selectedColor.R, _selectedColor.G, _selectedColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = colorDialog.Color;
                _selectedColor = MediaColor.FromArgb(color.A, color.R, color.G, color.B);
                ColorPreview.Background = new SolidColorBrush(_selectedColor);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(WidthBox.Text, out int width) && width > 0 &&
                int.TryParse(HeightBox.Text, out int height) && height > 0 &&
                int.TryParse(PixelSizeBox.Text, out int pixelSize) && pixelSize > 0)
            {
                Result = new CanvasSettings
                {
                    Width = width,
                    Height = height,
                    PixelSize = pixelSize,
                    Background = TransparentCheckBox.IsChecked == true ? Colors.Transparent : _selectedColor,
                    Transparent = TransparentCheckBox.IsChecked == true
                };

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for width, height, and pixel size.", 
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}