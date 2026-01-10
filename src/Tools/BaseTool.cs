using MSPaint.Models;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Tools
{
    public abstract class BaseTool : ITool
    {
        protected PixelGrid Grid;
        
        // Pixel change collection for undo/redo
        protected List<(int x, int y, MediaColor oldColor, MediaColor newColor)>? _pixelChanges;
        protected bool _isCollectingChanges = false;

        public BaseTool(PixelGrid grid)
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
    }
}