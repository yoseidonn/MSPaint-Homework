using MSPaint.Models;
using System.Collections.Generic;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    public class FillTool : BaseTool
    {
        private MediaColor _fillColor = MediaColors.Black;

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

            queue.Enqueue((startX, startY));
            visited.Add((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                if (!IsValidPosition(x, y)) continue;
                if (Grid.GetPixel(x, y) != targetColor) continue;

                Grid.SetPixel(x, y, fillColor);

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
                        if (Grid.GetPixel(nx, ny) == targetColor)
                        {
                            queue.Enqueue((nx, ny));
                            visited.Add((nx, ny));
                        }
                    }
                }
            }
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Grid.Width && y >= 0 && y < Grid.Height;
        }
    }
}
