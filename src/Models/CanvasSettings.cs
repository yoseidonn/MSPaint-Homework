using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Models
{
    public class CanvasSettings
    {
        public int Width { get; set; } = 512;
        public int Height { get; set; } = 512;
        public int PixelSize { get; set; } = 4;
        public MediaColor Background { get; set; } = Colors.White;
        public bool Transparent { get; set; } = false;
    }
}