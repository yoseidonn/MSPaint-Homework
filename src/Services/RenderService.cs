using MSPaint.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MSPaint.Services
{
    public class RenderService
    {
        // Minimal renderer that returns a non-null WriteableBitmap sized according to the grid.
        public Task<WriteableBitmap> RenderAsync(PixelGrid? grid)
        {
            int width = 1;
            int height = 1;

            if (grid != null)
            {
                width = grid.Width * grid.PixelSize;
                height = grid.Height * grid.PixelSize;
            }

            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            return Task.FromResult(wb);
        }
    }
}