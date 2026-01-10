using MSPaint.Models;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    public class EllipseTool : BaseTool
    {
        private MediaColor _drawColor = MediaColors.Black;
        private bool _isDrawing;
        private int _startX, _startY;
        private int _lastX, _lastY;

        public EllipseTool(PixelGrid grid) : base(grid) { }

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

            // Clear previous ellipse preview
            DrawEllipse(_startX, _startY, _lastX, _lastY, MediaColors.White);

            // Draw new ellipse preview
            _lastX = x;
            _lastY = y;
            DrawEllipse(_startX, _startY, _lastX, _lastY, _drawColor);
        }

        public override void OnMouseUp(int x, int y)
        {
            if (!_isDrawing) return;

            // Draw final ellipse
            DrawEllipse(_startX, _startY, x, y, _drawColor);
            _isDrawing = false;
        }

        private void DrawEllipse(int x0, int y0, int x1, int y1, MediaColor color)
        {
            // Calculate center and radii
            int centerX = (x0 + x1) / 2;
            int centerY = (y0 + y1) / 2;
            int radiusX = System.Math.Abs(x1 - x0) / 2;
            int radiusY = System.Math.Abs(y1 - y0) / 2;

            if (radiusX == 0 && radiusY == 0)
            {
                // Single point
                if (IsValidPosition(centerX, centerY))
                    Grid.SetPixel(centerX, centerY, color);
                return;
            }

            // Draw ellipse using midpoint algorithm
            for (int angle = 0; angle < 360; angle++)
            {
                double radians = angle * System.Math.PI / 180.0;
                int x = centerX + (int)(radiusX * System.Math.Cos(radians));
                int y = centerY + (int)(radiusY * System.Math.Sin(radians));

                if (IsValidPosition(x, y))
                    Grid.SetPixel(x, y, color);
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }
    }
}
