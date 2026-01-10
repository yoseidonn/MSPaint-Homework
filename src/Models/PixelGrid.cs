using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Models
{
    public class PixelGrid
    {
        public int Width { get; }
        public int Height { get; }
        public int PixelSize { get; }

        private MediaColor[,] _pixels;

        public PixelGrid(int width, int height, int pixelSize)
        {
            Width = width;
            Height = height;
            PixelSize = pixelSize;
            _pixels = new MediaColor[width, height];
        }

        public MediaColor GetPixel(int x, int y) => _pixels[x, y];
        public void SetPixel(int x, int y, MediaColor c) => _pixels[x, y] = c;
    }
}