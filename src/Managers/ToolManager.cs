using System.Windows;
using MSPaint.Controls;
using MSPaint.Tools;
using WpfButton = System.Windows.Controls.Button;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Managers
{
    /// <summary>
    /// Manages tool selection, highlighting, and color updates
    /// </summary>
    public class ToolManager
    {
        private readonly MainWindow _mainWindow;
        private WpfButton? _selectedToolButton;

        public ToolManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void SetTool(ITool tool)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                canvas.SetTool(tool);
            }
        }

        public void HighlightToolButton(WpfButton button)
        {
            // Reset all tool buttons
            _mainWindow.EraserButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);
            _mainWindow.FillButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);
            _mainWindow.PencilButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);
            _mainWindow.RectangleButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);
            _mainWindow.EllipseButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);
            _mainWindow.TextButton.Background = new System.Windows.Media.SolidColorBrush(MediaColors.Transparent);

            // Highlight selected button
            if (button != null)
            {
                button.Background = new System.Windows.Media.SolidColorBrush(MediaColors.LightBlue);
                _selectedToolButton = button;
            }
        }

        public void UpdateToolColor(MediaColor color)
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
            else if (currentTool is TextTool textTool)
                textTool.DrawColor = color;
        }

        // Tool selection handlers
        public void SelectPencilTool(MediaColor primaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new PencilTool(canvas.PixelGrid);
                tool.DrawColor = primaryColor;
                SetTool(tool);
                HighlightToolButton(_mainWindow.PencilButton);
            }
        }

        public void SelectEraserTool(MediaColor secondaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new EraserTool(canvas.PixelGrid);
                tool.EraseColor = secondaryColor;
                SetTool(tool);
                HighlightToolButton(_mainWindow.EraserButton);
            }
        }

        public void SelectFillTool(MediaColor primaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new FillTool(canvas.PixelGrid);
                tool.FillColor = primaryColor;
                SetTool(tool);
                HighlightToolButton(_mainWindow.FillButton);
            }
        }

        public void SelectRectangleTool(MediaColor primaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new RectangleTool(canvas.PixelGrid);
                tool.DrawColor = primaryColor;
                SetTool(tool);
                HighlightToolButton(_mainWindow.RectangleButton);
            }
        }

        public void SelectEllipseTool(MediaColor primaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new EllipseTool(canvas.PixelGrid);
                tool.DrawColor = primaryColor;
                SetTool(tool);
                HighlightToolButton(_mainWindow.EllipseButton);
            }
        }

        public void SelectTextTool(MediaColor primaryColor)
        {
            var canvas = GetCanvasControl();
            if (canvas?.PixelGrid != null)
            {
                var tool = new TextTool(canvas.PixelGrid);
                tool.DrawColor = primaryColor;
                tool.FontSize = 12; // Default font size
                SetTool(tool);
                HighlightToolButton(_mainWindow.TextButton);
            }
        }

        private DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return _mainWindow.GetDrawingPage()?.GetCanvasControl();
        }
    }
}
