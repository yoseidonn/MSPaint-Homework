using MSPaint.Core;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    /// <summary>
    /// Rectangle tool - draws rectangle outlines
    /// </summary>
    public class RectangleTool : ToolBase
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

                    // First, restore the grid pixels in the preview area (clear previous preview)
                    // Calculate dirty region for preview (only the rectangle outline area)
                    int dirtyMinX = System.Math.Max(0, minX - 1);
                    int dirtyMaxX = System.Math.Min(Grid.Width - 1, maxX + 1);
                    int dirtyMinY = System.Math.Max(0, minY - 1);
                    int dirtyMaxY = System.Math.Min(Grid.Height - 1, maxY + 1);
                    for (int y = dirtyMinY; y <= dirtyMaxY; y++)
                    {
                        for (int x = dirtyMinX; x <= dirtyMaxX; x++)
                        {
                            if (x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height)
                            {
                                MediaColor gridColor = Grid.GetPixel(x, y);
                                int offset = y * stride + x * bytesPerPixel;
                                buffer[offset] = gridColor.B;
                                buffer[offset + 1] = gridColor.G;
                                buffer[offset + 2] = gridColor.R;
                                buffer[offset + 3] = gridColor.A;
                            }
                        }
                    }

                    // Now draw rectangle outline only (not filled)
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
                            if (maxY >= 0 && maxY < Grid.Height && maxY != minY)
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
                            if (maxX >= 0 && maxX < Grid.Width && maxX != minX)
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

                // Only mark the dirty region as changed (recalculate outside unsafe block)
                int dirtyMinX2 = System.Math.Max(0, System.Math.Min(_startX, _lastX) - 1);
                int dirtyMaxX2 = System.Math.Min(Grid.Width - 1, System.Math.Max(_startX, _lastX) + 1);
                int dirtyMinY2 = System.Math.Max(0, System.Math.Min(_startY, _lastY) - 1);
                int dirtyMaxY2 = System.Math.Min(Grid.Height - 1, System.Math.Max(_startY, _lastY) + 1);
                int dirtyWidth = dirtyMaxX2 - dirtyMinX2 + 1;
                int dirtyHeight = dirtyMaxY2 - dirtyMinY2 + 1;
                previewBitmap.AddDirtyRect(new System.Windows.Int32Rect(dirtyMinX2, dirtyMinY2, dirtyWidth, dirtyHeight));
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
    }
}
