using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MSPaint.Core;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Tools
{
    /// <summary>
    /// Base class for all drawing tools
    /// Provides common functionality like change tracking and line drawing
    /// </summary>
    public abstract class ToolBase : ITool
    {
        protected PixelGrid Grid;
        
        // Pixel change collection for undo/redo
        protected List<(int x, int y, MediaColor oldColor, MediaColor newColor)>? _pixelChanges;
        protected bool _isCollectingChanges = false;

        public ToolBase(PixelGrid grid)
        {
            Grid = grid;
        }

        public virtual void OnMouseDown(int x, int y) { }
        public virtual void OnMouseMove(int x, int y) { }
        public virtual void OnMouseUp(int x, int y) { }
        
        public virtual bool UsesPreview => false;
        public virtual void RenderPreview(WriteableBitmap previewBitmap, int pixelSize) { }

        /// <summary>
        /// Start collecting pixel changes for command pattern
        /// </summary>
        public virtual void StartCollectingChanges()
        {
            _pixelChanges = new List<(int, int, MediaColor, MediaColor)>();
            _isCollectingChanges = true;
        }

        /// <summary>
        /// Stop collecting and return the collected changes
        /// </summary>
        public virtual List<(int x, int y, MediaColor oldColor, MediaColor newColor)>? StopCollectingChanges()
        {
            _isCollectingChanges = false;
            var changes = _pixelChanges;
            _pixelChanges = null;
            return changes;
        }

        /// <summary>
        /// Set pixel with change tracking (collects old and new color)
        /// </summary>
        protected void SetPixelWithTracking(int x, int y, MediaColor newColor)
        {
            if (x < 0 || x >= Grid.Width || y < 0 || y >= Grid.Height) return;

            MediaColor oldColor = Grid.GetPixel(x, y);
            
            // Only track if color actually changes
            if (oldColor != newColor)
            {
                if (_isCollectingChanges && _pixelChanges != null)
                {
                    // Check if this pixel was already changed in this stroke
                    var existing = _pixelChanges.FindIndex(p => p.x == x && p.y == y);
                    if (existing >= 0)
                    {
                        // Update existing entry - keep original oldColor, update newColor
                        var existingChange = _pixelChanges[existing];
                        _pixelChanges[existing] = (x, y, existingChange.oldColor, newColor);
                    }
                    else
                    {
                        // New pixel change
                        _pixelChanges.Add((x, y, oldColor, newColor));
                    }
                }

                // Apply the change
                Grid.SetPixel(x, y, newColor);
            }
        }

        /// <summary>
        /// Check if the given position is valid within the grid bounds
        /// </summary>
        protected bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }

        /// <summary>
        /// Draw a line from (x0, y0) to (x1, y1) using Bresenham's algorithm
        /// </summary>
        protected void DrawLine(int x0, int y0, int x1, int y1, MediaColor color)
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
                    SetPixelWithTracking(x, y, color);
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
    }
}
