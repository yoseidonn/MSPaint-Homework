using MSPaint.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

            // Draw final rectangle to actual grid
            DrawRectangleToGrid(_startX, _startY, x, y, _drawColor);
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

                    // Normalize coordinates
                    int minX = System.Math.Min(_startX, _lastX);
                    int maxX = System.Math.Max(_startX, _lastX);
                    int minY = System.Math.Min(_startY, _lastY);
                    int maxY = System.Math.Max(_startY, _lastY);

                    // Draw rectangle outline to preview bitmap (1:1 mapping)
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (x >= 0 && x < Grid.Width)
                        {
                            if (minY >= 0 && minY < Grid.Height)
                            {
                                int offset = minY * stride + x * bytesPerPixel;
                                buffer[offset] = _drawColor.B;
                                buffer[offset + 1] = _drawColor.G;
                                buffer[offset + 2] = _drawColor.R;
                                buffer[offset + 3] = _drawColor.A;
                            }
                            if (maxY >= 0 && maxY < Grid.Height)
                            {
                                int offset = maxY * stride + x * bytesPerPixel;
                                buffer[offset] = _drawColor.B;
                                buffer[offset + 1] = _drawColor.G;
                                buffer[offset + 2] = _drawColor.R;
                                buffer[offset + 3] = _drawColor.A;
                            }
                        }
                    }

                    for (int y = minY; y <= maxY; y++)
                    {
                        if (y >= 0 && y < Grid.Height)
                        {
                            if (minX >= 0 && minX < Grid.Width)
                            {
                                int offset = y * stride + minX * bytesPerPixel;
                                buffer[offset] = _drawColor.B;
                                buffer[offset + 1] = _drawColor.G;
                                buffer[offset + 2] = _drawColor.R;
                                buffer[offset + 3] = _drawColor.A;
                            }
                            if (maxX >= 0 && maxX < Grid.Width)
                            {
                                int offset = y * stride + maxX * bytesPerPixel;
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

        private void DrawRectangleToGrid(int x0, int y0, int x1, int y1, MediaColor color)
        {
            // Normalize coordinates
            int minX = System.Math.Min(x0, x1);
            int maxX = System.Math.Max(x0, x1);
            int minY = System.Math.Min(y0, y1);
            int maxY = System.Math.Max(y0, y1);

            // Draw rectangle outline (with change tracking)
            for (int x = minX; x <= maxX; x++)
            {
                if (IsValidPosition(x, minY))
                    SetPixelWithTracking(x, minY, color); // Top edge
                if (IsValidPosition(x, maxY))
                    SetPixelWithTracking(x, maxY, color); // Bottom edge
            }

            for (int y = minY; y <= maxY; y++)
            {
                if (IsValidPosition(minX, y))
                    SetPixelWithTracking(minX, y, color); // Left edge
                if (IsValidPosition(maxX, y))
                    SetPixelWithTracking(maxX, y, color); // Right edge
            }
        }


        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }
    }
}
