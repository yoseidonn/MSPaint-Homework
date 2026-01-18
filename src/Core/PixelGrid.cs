using System;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Core
{
    /// <summary>
    /// Pixel grid - stores pixel data as a 2D array of colors
    /// PixelSize is NOT stored here - it's only used in rendering/view layer
    /// </summary>
    public class PixelGrid
    {
        public int Width { get; }
        public int Height { get; }

        private MediaColor[,] _pixels;
        
        // Dirty region tracking - only render changed pixels
        private int _dirtyMinX = int.MaxValue;
        private int _dirtyMinY = int.MaxValue;
        private int _dirtyMaxX = int.MinValue;
        private int _dirtyMaxY = int.MinValue;
        private bool _isDirty = false;
        private readonly object _dirtyLock = new object();

        public PixelGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be greater than 0");
            
            Width = width;
            Height = height;
            _pixels = new MediaColor[width, height];
            MarkAllDirty(); // Initial state: everything needs rendering
        }

        public MediaColor GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return MediaColor.FromArgb(0, 0, 0, 0); // Transparent for out-of-bounds
            return _pixels[x, y];
        }
        
        public void SetPixel(int x, int y, MediaColor c)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            
            _pixels[x, y] = c;
            
            // Update dirty region
            lock (_dirtyLock)
            {
                _isDirty = true;
                if (x < _dirtyMinX) _dirtyMinX = x;
                if (x > _dirtyMaxX) _dirtyMaxX = x;
                if (y < _dirtyMinY) _dirtyMinY = y;
                if (y > _dirtyMaxY) _dirtyMaxY = y;
            }
        }
        
        // Get dirty region and clear it (atomic operation)
        public bool GetAndClearDirtyRegion(out int minX, out int minY, out int maxX, out int maxY)
        {
            lock (_dirtyLock)
            {
                if (!_isDirty)
                {
                    minX = minY = maxX = maxY = 0;
                    return false;
                }
                
                minX = _dirtyMinX;
                minY = _dirtyMinY;
                maxX = _dirtyMaxX;
                maxY = _dirtyMaxY;
                
                // Clear dirty region
                _dirtyMinX = int.MaxValue;
                _dirtyMinY = int.MaxValue;
                _dirtyMaxX = int.MinValue;
                _dirtyMaxY = int.MinValue;
                _isDirty = false;
                
                return true;
            }
        }
        
        // Mark entire grid as dirty (e.g., after initialization or full redraw)
        public void MarkAllDirty()
        {
            lock (_dirtyLock)
            {
                _isDirty = true;
                _dirtyMinX = 0;
                _dirtyMinY = 0;
                _dirtyMaxX = Width - 1;
                _dirtyMaxY = Height - 1;
            }
        }
        
        // Check if there's a dirty region
        public bool HasDirtyRegion()
        {
            lock (_dirtyLock)
            {
                return _isDirty;
            }
        }
    }
}
