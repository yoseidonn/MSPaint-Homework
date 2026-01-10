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
            DrawLine(_lastX, _lastY, x, y);
            
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

        private void DrawLine(int x0, int y0, int x1, int y1)
        {
            // Simple line drawing using Bresenham's algorithm
            int dx = System.Math.Abs(x1 - x0);
            int dy = System.Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                if (IsValidPosition(x, y))
                {
                    SetPixelWithTracking(x, y, _eraseColor);
                }

                if (x == x1 && y == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }
    }
}
