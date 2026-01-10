using System.Windows.Media;

namespace MSPaint.Models
{
    public class CanvasSettings
    {
        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;
        public int PixelSize { get; set; } = 8;
        public Color Background { get; set; } = Colors.White;
        public bool Transparent { get; set; } = false;
    }
}