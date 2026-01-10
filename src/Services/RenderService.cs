using MSPaint.Models;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSPaint.Services
{
    public class RenderService
    {
        // Renders a PixelGrid to a WriteableBitmap, scaling each pixel by PixelSize
        // Note: WriteableBitmap operations must be on UI thread for thread safety
        // For now, we render synchronously. For large canvases, we can optimize later
        // by preparing pixel data on background thread, then updating bitmap on UI thread
        public Task<WriteableBitmap> RenderAsync(PixelGrid? grid)
        {
            // Calculate dimensions
            int width = 1;
            int height = 1;

            if (grid != null)
            {
                width = grid.Width * grid.PixelSize;
                height = grid.Height * grid.PixelSize;
            }

            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            // Create bitmap on UI thread (must be on UI thread)
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            
            if (grid != null)
            {
                // Update bitmap on UI thread
                wb.Lock();
                try
                {
                    int stride = wb.BackBufferStride;
                    int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
                    int pixelSize = grid.PixelSize;

                    unsafe
                    {
                        byte* buffer = (byte*)wb.BackBuffer;

                        // Iterate through each pixel in the grid
                        for (int gridY = 0; gridY < grid.Height; gridY++)
                        {
                            for (int gridX = 0; gridX < grid.Width; gridX++)
                            {
                                Color pixelColor = grid.GetPixel(gridX, gridY);

                                // Draw this pixel scaled by PixelSize
                                for (int py = 0; py < pixelSize; py++)
                                {
                                    for (int px = 0; px < pixelSize; px++)
                                    {
                                        int x = gridX * pixelSize + px;
                                        int y = gridY * pixelSize + py;

                                        if (x < width && y < height)
                                        {
                                            int offset = y * stride + x * bytesPerPixel;
                                            buffer[offset] = pixelColor.B;     // Blue
                                            buffer[offset + 1] = pixelColor.G; // Green
                                            buffer[offset + 2] = pixelColor.R; // Red
                                            buffer[offset + 3] = pixelColor.A; // Alpha
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Mark the entire bitmap as dirty
                    wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                }
                finally
                {
                    wb.Unlock();
                }
            }
            else
            {
                // Fill with white if grid is null
                wb.Lock();
                try
                {
                    unsafe
                    {
                        byte* buffer = (byte*)wb.BackBuffer;
                        int stride = wb.BackBufferStride;
                        int bytesPerPixel = 4;

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int offset = y * stride + x * bytesPerPixel;
                                buffer[offset] = 255;     // Blue
                                buffer[offset + 1] = 255; // Green
                                buffer[offset + 2] = 255; // Red
                                buffer[offset + 3] = 255; // Alpha
                            }
                        }
                    }
                    wb.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                }
                finally
                {
                    wb.Unlock();
                }
            }

            return Task.FromResult(wb);
        }
    }
}
