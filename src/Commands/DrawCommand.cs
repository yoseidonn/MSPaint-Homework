using System.Collections.Generic;
using System.Linq;
using MSPaint.Models;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Commands
{
    /// <summary>
    /// Command that tracks pixel changes for a drawing stroke
    /// Stores only changed pixels (x, y, oldColor, newColor) for memory efficiency
    /// </summary>
    public class DrawCommand : ICommand
    {
        private readonly PixelGrid _grid;
        private readonly List<(int x, int y, MediaColor oldColor, MediaColor newColor)> _pixelChanges;
        private bool _isExecuted;

        public DrawCommand(PixelGrid grid, List<(int x, int y, MediaColor oldColor, MediaColor newColor)> pixelChanges)
        {
            _grid = grid;
            _pixelChanges = pixelChanges ?? new List<(int, int, MediaColor, MediaColor)>();
            // Command is already executed (pixels were changed by tools via SetPixelWithTracking)
            _isExecuted = true;
        }

        public void Execute()
        {
            if (_isExecuted) return;

            // Apply all pixel changes
            foreach (var (x, y, _, newColor) in _pixelChanges)
            {
                if (x >= 0 && x < _grid.Width && y >= 0 && y < _grid.Height)
                {
                    _grid.SetPixel(x, y, newColor);
                }
            }

            _isExecuted = true;
        }

        public void Undo()
        {
            if (!_isExecuted) return;

            // Restore old colors
            foreach (var (x, y, oldColor, _) in _pixelChanges)
            {
                if (x >= 0 && x < _grid.Width && y >= 0 && y < _grid.Height)
                {
                    _grid.SetPixel(x, y, oldColor);
                }
            }

            _isExecuted = false;
        }

        /// <summary>
        /// Check if command has any pixel changes
        /// </summary>
        public bool HasChanges => _pixelChanges.Count > 0;
    }
}
