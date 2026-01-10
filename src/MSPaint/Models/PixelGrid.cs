using System.Windows.Media;

namespace MSPaint.Models
{
    public class PixelGrid
    {
        public int Width { get; }
        public int Height { get; }
        public int PixelSize { get; }

        private Color[,] _pixels;

        public PixelGrid(int width, int height, int pixelSize)
        {
            Width = width;
            Height = height;
            PixelSize = pixelSize;
            _pixels = new Color[width, height];
        }

        public Color GetPixel(int x, int y) => _pixels[x, y];
        public void SetPixel(int x, int y, Color c) => _pixels[x, y] = c;
    }
}