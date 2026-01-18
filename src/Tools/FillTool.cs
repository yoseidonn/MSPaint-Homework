using MSPaint.Core;
using System.Collections.Generic;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    /// <summary>
    /// Fill tool - flood fills connected regions
    /// </summary>
    public class FillTool : ToolBase
    {
        private MediaColor _fillColor = MediaColors.Black;
        private const int MaxFillPixels = 100000; // Limit to prevent memory issues and UI freezing

        public FillTool(PixelGrid grid) : base(grid) { }

        public MediaColor FillColor
        {
            get => _fillColor;
            set => _fillColor = value;
        }

        public override void OnMouseDown(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;

            MediaColor targetColor = Grid.GetPixel(x, y);
            
            // If clicking on the same color, do nothing
            if (targetColor == _fillColor) return;

            // Flood fill using queue-based algorithm (avoids stack overflow)
            FloodFill(x, y, targetColor, _fillColor);
        }

        private void FloodFill(int startX, int startY, MediaColor targetColor, MediaColor fillColor)
        {
            var queue = new Queue<(int x, int y)>();
            var visited = new HashSet<(int x, int y)>();
            int pixelsFilled = 0;

            queue.Enqueue((startX, startY));
            visited.Add((startX, startY));

            while (queue.Count > 0 && pixelsFilled < MaxFillPixels)
            {
                var (x, y) = queue.Dequeue();

                if (!IsValidPosition(x, y)) continue;
                
                // Check if pixel still has target color (might have been changed)
                MediaColor currentColor = Grid.GetPixel(x, y);
                if (currentColor != targetColor) continue;

                SetPixelWithTracking(x, y, fillColor);
                pixelsFilled++;

                // Check neighbors
                var neighbors = new[]
                {
                    (x - 1, y),     // Left
                    (x + 1, y),     // Right
                    (x, y - 1),     // Up
                    (x, y + 1)      // Down
                };

                foreach (var (nx, ny) in neighbors)
                {
                    if (IsValidPosition(nx, ny) && !visited.Contains((nx, ny)))
                    {
                        MediaColor neighborColor = Grid.GetPixel(nx, ny);
                        if (neighborColor == targetColor)
                        {
                            queue.Enqueue((nx, ny));
                            visited.Add((nx, ny));
                        }
                    }
                }
            }

            // If we hit the limit, show a warning (optional - can be removed if not needed)
            if (pixelsFilled >= MaxFillPixels)
            {
                System.Diagnostics.Debug.WriteLine($"FillTool: Reached maximum fill limit of {MaxFillPixels} pixels");
            }
        }
    }
}
