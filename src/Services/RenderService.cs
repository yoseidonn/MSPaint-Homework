using MSPaint.Models;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Services
{
    public class RenderService
    {
        // Renders a PixelGrid to a WriteableBitmap (1:1 mapping - no scaling)
        // Visual scaling is done in XAML with NearestNeighbor to save memory
        // For large canvases (>1000x1000 pixels), pixel data is prepared on background thread
        public async Task<WriteableBitmap> RenderAsync(PixelGrid? grid)
        {
            // Calculate dimensions - 1:1 with grid (NO PixelSize multiplication)
            int width = 1;
            int height = 1;

            if (grid != null)
            {
                width = grid.Width;
                height = grid.Height;
            }

            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            // Create bitmap on UI thread (must be on UI thread)
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
            
            if (grid != null)
            {
                int stride = wb.BackBufferStride;
                int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
                int totalBytes = stride * height;

                // For large canvases, prepare pixel data on background thread
                bool useBackgroundThread = (width * height) > 1000000; // 1M pixels threshold

                byte[]? pixelData = null;
                if (useBackgroundThread)
                {
                    // Prepare pixel data on background thread
                    pixelData = await Task.Run(() => PreparePixelData(grid, width, height, stride, bytesPerPixel));
                }

                // Update bitmap on UI thread
                wb.Lock();
                try
                {
                    unsafe
                    {
                        byte* buffer = (byte*)wb.BackBuffer;

                        if (useBackgroundThread && pixelData != null)
                        {
                            // Copy prepared data to bitmap buffer
                            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, (System.IntPtr)buffer, totalBytes);
                        }
                        else
                        {
                            // Small canvas: render directly on UI thread (1:1 mapping)
                            for (int y = 0; y < grid.Height; y++)
                            {
                                for (int x = 0; x < grid.Width; x++)
                                {
                                    MediaColor pixelColor = grid.GetPixel(x, y);
                                    int offset = y * stride + x * bytesPerPixel;
                                    buffer[offset] = pixelColor.B;     // Blue
                                    buffer[offset + 1] = pixelColor.G; // Green
                                    buffer[offset + 2] = pixelColor.R; // Red
                                    buffer[offset + 3] = pixelColor.A; // Alpha
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

            return wb;
        }

        private byte[] PreparePixelData(PixelGrid grid, int width, int height, int stride, int bytesPerPixel)
        {
            byte[] pixelData = new byte[stride * height];

            // Prepare pixel data on background thread (1:1 mapping)
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    MediaColor pixelColor = grid.GetPixel(x, y);
                    int offset = y * stride + x * bytesPerPixel;
                    pixelData[offset] = pixelColor.B;     // Blue
                    pixelData[offset + 1] = pixelColor.G; // Green
                    pixelData[offset + 2] = pixelColor.R; // Red
                    pixelData[offset + 3] = pixelColor.A; // Alpha
                }
            }

            return pixelData;
        }

        // Updates an existing WriteableBitmap with new pixel data from PixelGrid (1:1 mapping)
        // This method reuses the bitmap instead of creating a new one, preventing memory leaks
        // Now uses dirty region tracking - only renders changed pixels
        // Returns dirty region size for logging purposes
        public async Task<(bool rendered, int dirtyWidth, int dirtyHeight)> UpdateBitmapAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) 
                return (false, 0, 0);

            int width = grid.Width;
            int height = grid.Height;
            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            // Check if bitmap size matches - if not, caller should create new bitmap
            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
                return (false, 0, 0);

            // Get dirty region from grid
            if (!grid.GetAndClearDirtyRegion(out int minX, out int minY, out int maxX, out int maxY))
            {
                // No dirty region - nothing to render
                return (false, 0, 0);
            }

            // Clamp dirty region to grid bounds
            minX = System.Math.Max(0, minX);
            minY = System.Math.Max(0, minY);
            maxX = System.Math.Min(grid.Width - 1, maxX);
            maxY = System.Math.Min(grid.Height - 1, maxY);

            int dirtyWidth = maxX - minX + 1;
            int dirtyHeight = maxY - minY + 1;

            if (dirtyWidth <= 0 || dirtyHeight <= 0)
                return (false, 0, 0);

            int stride = bitmap.BackBufferStride;
            int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel

            // Update only dirty region on UI thread (1:1 mapping - no scaling)
            bitmap.Lock();
            try
            {
                unsafe
                {
                    byte* buffer = (byte*)bitmap.BackBuffer;

                    // Render only dirty region (1:1 mapping)
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            MediaColor pixelColor = grid.GetPixel(x, y);
                            int offset = y * stride + x * bytesPerPixel;
                            buffer[offset] = pixelColor.B;     // Blue
                            buffer[offset + 1] = pixelColor.G; // Green
                            buffer[offset + 2] = pixelColor.R; // Red
                            buffer[offset + 3] = pixelColor.A; // Alpha
                        }
                    }
                }

                // Mark only dirty region as dirty (not entire bitmap)
                bitmap.AddDirtyRect(new System.Windows.Int32Rect(minX, minY, dirtyWidth, dirtyHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
            
            return (true, dirtyWidth, dirtyHeight);
        }
        
        // Full update method - renders entire bitmap (used for initial render or when needed)
        // Uses 1:1 mapping - no PixelSize scaling
        public async Task UpdateBitmapFullAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) return;

            int width = grid.Width;
            int height = grid.Height;
            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            // Check if bitmap size matches - if not, caller should create new bitmap
            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
                return;

            int stride = bitmap.BackBufferStride;
            int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
            int totalBytes = stride * height;

            // For large canvases, prepare pixel data on background thread
            bool useBackgroundThread = (width * height) > 1000000; // 1M pixels threshold
            byte[]? pixelData = null;

            if (useBackgroundThread)
            {
                // Prepare pixel data on background thread
                pixelData = await Task.Run(() => PreparePixelData(grid, width, height, stride, bytesPerPixel));
            }

            // Update bitmap on UI thread
            bitmap.Lock();
            try
            {
                unsafe
                {
                    byte* buffer = (byte*)bitmap.BackBuffer;

                    if (useBackgroundThread && pixelData != null)
                    {
                        // Copy prepared data to bitmap buffer
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, (System.IntPtr)buffer, totalBytes);
                    }
                    else
                    {
                        // Small canvas: render directly on UI thread (1:1 mapping)
                        for (int y = 0; y < grid.Height; y++)
                        {
                            for (int x = 0; x < grid.Width; x++)
                            {
                                MediaColor pixelColor = grid.GetPixel(x, y);
                                int offset = y * stride + x * bytesPerPixel;
                                buffer[offset] = pixelColor.B;     // Blue
                                buffer[offset + 1] = pixelColor.G; // Green
                                buffer[offset + 2] = pixelColor.R; // Red
                                buffer[offset + 3] = pixelColor.A; // Alpha
                            }
                        }
                    }
                }

                // Mark the entire bitmap as dirty
                bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
    }
}
