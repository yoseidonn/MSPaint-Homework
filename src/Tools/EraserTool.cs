using MSPaint.Models;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    public class EraserTool : BaseTool
    {
        private bool _isDrawing;
        private int _lastX, _lastY;
        private MediaColor _eraseColor = MediaColors.White; // Default erase to white

        public EraserTool(PixelGrid grid) : base(grid) { }

        public MediaColor EraseColor
        {
            get => _eraseColor;
            set => _eraseColor = value;
        }

        public override void OnMouseDown(int x, int y)
        {
            _isDrawing = true;
            _lastX = x;
            _lastY = y;
            
            // Erase the initial pixel (with change tracking)
            if (IsValidPosition(x, y))
            {
                SetPixelWithTracking(x, y, _eraseColor);
            }
        }

        public override void OnMouseMove(int x, int y)
        {
            if (!_isDrawing) return;

            // Erase line from last position to current position
            DrawLine(_lastX, _lastY, x, y, _eraseColor);
            
            _lastX = x;
            _lastY = y;
        }

        public override void OnMouseUp(int x, int y)
        {
            if (!_isDrawing) return;

            // Erase final pixel if needed (with change tracking)
            if (IsValidPosition(x, y) && (x != _lastX || y != _lastY))
            {
                SetPixelWithTracking(x, y, _eraseColor);
            }

            _isDrawing = false;
        }
    }
}
