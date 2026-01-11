using MSPaint.Models;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;


namespace MSPaint.Tools
{
    public class PencilTool : BaseTool
    {
        private MediaColor _drawColor = MediaColors.Black;
        private bool _isDrawing;
        private int _lastX, _lastY;

        public PencilTool(PixelGrid grid) : base(grid) { }

        public MediaColor DrawColor
        {
            get => _drawColor;
            set => _drawColor = value;
        }

        public override void OnMouseDown(int x, int y)
        {
            _isDrawing = true;
            _lastX = x;
            _lastY = y;
            
            // Draw the initial pixel (with change tracking)
            if (IsValidPosition(x, y))
            {
                SetPixelWithTracking(x, y, _drawColor);
            }
        }

        public override void OnMouseMove(int x, int y)
        {
            if (!_isDrawing) return;

            // Draw line from last position to current position (Bresenham-like)
            DrawLine(_lastX, _lastY, x, y, _drawColor);
            
            _lastX = x;
            _lastY = y;
        }

        public override void OnMouseUp(int x, int y)
        {
            if (!_isDrawing) return;

            // Draw final pixel if needed (with change tracking)
            if (IsValidPosition(x, y) && (x != _lastX || y != _lastY))
            {
                SetPixelWithTracking(x, y, _drawColor);
            }

            _isDrawing = false;
        }
    }
}