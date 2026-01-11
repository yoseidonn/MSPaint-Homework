using MSPaint.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        public override bool UsesPreview => true;

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
            // Just update coordinates - preview will be rendered in RenderPreview
            _lastX = x;
            _lastY = y;
        }

        public override void OnMouseUp(int x, int y)
        {
            if (!_isDrawing) return;

            // Draw final ellipse to actual grid
            DrawEllipseToGrid(_startX, _startY, x, y, _drawColor);
            _isDrawing = false;
        }

        public override void RenderPreview(WriteableBitmap previewBitmap, int pixelSize)
        {
            if (!_isDrawing) return;

            previewBitmap.Lock();
            try
            {
                unsafe
                {
                    byte* buffer = (byte*)previewBitmap.BackBuffer;
                    int stride = previewBitmap.BackBufferStride;
                    int bytesPerPixel = 4;

                    // Calculate center and radii
                    int centerX = (_startX + _lastX) / 2;
                    int centerY = (_startY + _lastY) / 2;
                    int radiusX = System.Math.Abs(_lastX - _startX) / 2;
                    int radiusY = System.Math.Abs(_lastY - _startY) / 2;

                    if (radiusX == 0 && radiusY == 0)
                    {
                        // Single point (1:1 mapping)
                        if (IsValidPosition(centerX, centerY))
                        {
                            int offset = centerY * stride + centerX * bytesPerPixel;
                            buffer[offset] = _drawColor.B;
                            buffer[offset + 1] = _drawColor.G;
                            buffer[offset + 2] = _drawColor.R;
                            buffer[offset + 3] = _drawColor.A;
                        }
                    }
                    else
                    {
                        // Draw ellipse using midpoint algorithm (1:1 mapping)
                        for (int angle = 0; angle < 360; angle++)
                        {
                            double radians = angle * System.Math.PI / 180.0;
                            int x = centerX + (int)(radiusX * System.Math.Cos(radians));
                            int y = centerY + (int)(radiusY * System.Math.Sin(radians));

                            if (IsValidPosition(x, y))
                            {
                                int offset = y * stride + x * bytesPerPixel;
                                buffer[offset] = _drawColor.B;
                                buffer[offset + 1] = _drawColor.G;
                                buffer[offset + 2] = _drawColor.R;
                                buffer[offset + 3] = _drawColor.A;
                            }
                        }
                    }
                }

                previewBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, Grid.Width, Grid.Height));
            }
            finally
            {
                previewBitmap.Unlock();
            }
        }

        private void DrawEllipseToGrid(int x0, int y0, int x1, int y1, MediaColor color)
        {
            // Calculate center and radii
            int centerX = (x0 + x1) / 2;
            int centerY = (y0 + y1) / 2;
            int radiusX = System.Math.Abs(x1 - x0) / 2;
            int radiusY = System.Math.Abs(y1 - y0) / 2;

            if (radiusX == 0 && radiusY == 0)
            {
                // Single point (with change tracking)
                if (IsValidPosition(centerX, centerY))
                    SetPixelWithTracking(centerX, centerY, color);
                return;
            }

            // Draw ellipse using midpoint algorithm (with change tracking)
            for (int angle = 0; angle < 360; angle++)
            {
                double radians = angle * System.Math.PI / 180.0;
                int x = centerX + (int)(radiusX * System.Math.Cos(radians));
                int y = centerY + (int)(radiusY * System.Math.Sin(radians));

                if (IsValidPosition(x, y))
                    SetPixelWithTracking(x, y, color);
            }
        }
    }
}
