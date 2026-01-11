using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Managers;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Managers
{
    /// <summary>
    /// Manages color selection, preview, and color picker dialogs
    /// </summary>
    public class ColorManager
    {
        private readonly MainWindow _mainWindow;
        private readonly ToolManager _toolManager;
        private MediaColor _primaryColor = MediaColors.Black;
        private MediaColor _secondaryColor = MediaColors.White;

        public MediaColor PrimaryColor
        {
            get => _primaryColor;
            set
            {
                _primaryColor = value;
                UpdateColorPreview();
            }
        }

        public MediaColor SecondaryColor
        {
            get => _secondaryColor;
            set
            {
                _secondaryColor = value;
                UpdateColorPreview();
            }
        }

        public ColorManager(MainWindow mainWindow, ToolManager toolManager)
        {
            _mainWindow = mainWindow;
            _toolManager = toolManager;
        }

        public void ShowColorPicker(bool isPrimary)
        {
            var colorDialog = new ColorDialog
            {
                Color = System.Drawing.Color.FromArgb(
                    isPrimary ? _primaryColor.A : _secondaryColor.A,
                    isPrimary ? _primaryColor.R : _secondaryColor.R,
                    isPrimary ? _primaryColor.G : _secondaryColor.G,
                    isPrimary ? _primaryColor.B : _secondaryColor.B),
                FullOpen = true
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
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
                    _toolManager.UpdateToolColor(_primaryColor);
                }
            }
        }

        public void UpdateColorPreview()
        {
            _mainWindow.PrimaryColorPreview.Background = new SolidColorBrush(_primaryColor);
            _mainWindow.SecondaryColorPreview.Background = new SolidColorBrush(_secondaryColor);
        }

        public void HandleColorSwatchClick(string colorName)
        {
            MediaColor color = GetColorByName(colorName);
            _primaryColor = color;
            UpdateColorPreview();
            _toolManager.UpdateToolColor(color);
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
