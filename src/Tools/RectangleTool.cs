using MSPaint.Models;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    public class RectangleTool : BaseTool
    {
        private MediaColor _drawColor = MediaColors.Black;
        private bool _isDrawing;
        private int _startX, _startY;
        private int _lastX, _lastY;

        public RectangleTool(PixelGrid grid) : base(grid) { }

        public MediaColor DrawColor
        {
            get => _drawColor;
            set => _drawColor = value;
        }

        public override void OnMouseDown(int x, int y)
        {
            _isDrawing = true;
            _startX = x;
            _startY = y;
            _lastX = x;
            _lastY = y;
        }

        public override void OnMouseMove(int x, int y)
        {
            if (!_isDrawing) return;

            // Clear previous rectangle preview
            DrawRectangle(_startX, _startY, _lastX, _lastY, MediaColors.White);

            // Draw new rectangle preview
            _lastX = x;
            _lastY = y;
            DrawRectangle(_startX, _startY, _lastX, _lastY, _drawColor);
        }

        public override void OnMouseUp(int x, int y)
        {
            if (!_isDrawing) return;

            // Draw final rectangle
            DrawRectangle(_startX, _startY, x, y, _drawColor);
            _isDrawing = false;
        }

        private void DrawRectangle(int x0, int y0, int x1, int y1, MediaColor color)
        {
            // Normalize coordinates
            int minX = System.Math.Min(x0, x1);
            int maxX = System.Math.Max(x0, x1);
            int minY = System.Math.Min(y0, y1);
            int maxY = System.Math.Max(y0, y1);

            // Draw rectangle outline
            for (int x = minX; x <= maxX; x++)
            {
                if (IsValidPosition(x, minY))
                    Grid.SetPixel(x, minY, color); // Top edge
                if (IsValidPosition(x, maxY))
                    Grid.SetPixel(x, maxY, color); // Bottom edge
            }

            for (int y = minY; y <= maxY; y++)
            {
                if (IsValidPosition(minX, y))
                    Grid.SetPixel(minX, y, color); // Left edge
                if (IsValidPosition(maxX, y))
                    Grid.SetPixel(maxX, y, color); // Right edge
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }
    }
}
