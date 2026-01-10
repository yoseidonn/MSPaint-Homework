using MSPaint.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MSPaint.Services
{
    public class RenderService
    {
        public Task<WriteableBitmap> RenderAsync(PixelGrid grid)
        {
            // placeholder
            return Task.FromResult<WriteableBitmap>(null);
        }
    }
}