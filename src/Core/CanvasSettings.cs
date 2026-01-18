using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Core
{
    /// <summary>
    /// Canvas settings - used for initializing a new canvas
    /// PixelSize is used only in rendering/view layer, not stored in PixelGrid
    /// </summary>
    public class CanvasSettings
    {
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public int PixelSize { get; set; } = 4; // Display scaling factor (not stored in PixelGrid)
        public MediaColor Background { get; set; } = Colors.White;
        public bool Transparent { get; set; } = false;
    }
}
