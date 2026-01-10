using MSPaint.Models;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Services
{
    public class RenderService
    {
        // Renders a PixelGrid to a WriteableBitmap, scaling each pixel by PixelSize
        // For large canvases (>1000x1000 pixels), pixel data is prepared on background thread
        // then copied to bitmap on UI thread for better performance
        public async Task<WriteableBitmap> RenderAsync(PixelGrid? grid)
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
                int stride = wb.BackBufferStride;
                int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
                int totalBytes = stride * height;
                int pixelSize = grid.PixelSize;

                // For large canvases, prepare pixel data on background thread
                bool useBackgroundThread = (width * height) > 1000000; // 1M pixels threshold

                byte[]? pixelData = null;
                if (useBackgroundThread)
                {
                    // Prepare pixel data on background thread
                    pixelData = await Task.Run(() => PreparePixelData(grid, width, height, stride, bytesPerPixel, pixelSize));
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
                            // Small canvas: render directly on UI thread
                            for (int gridY = 0; gridY < grid.Height; gridY++)
                            {
                                for (int gridX = 0; gridX < grid.Width; gridX++)
                                {
                                    MediaColor pixelColor = grid.GetPixel(gridX, gridY);

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

        private byte[] PreparePixelData(PixelGrid grid, int width, int height, int stride, int bytesPerPixel, int pixelSize)
        {
            byte[] pixelData = new byte[stride * height];

            // Prepare pixel data on background thread
            for (int gridY = 0; gridY < grid.Height; gridY++)
            {
                for (int gridX = 0; gridX < grid.Width; gridX++)
                {
                    MediaColor pixelColor = grid.GetPixel(gridX, gridY);

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
                                pixelData[offset] = pixelColor.B;     // Blue
                                pixelData[offset + 1] = pixelColor.G; // Green
                                pixelData[offset + 2] = pixelColor.R; // Red
                                pixelData[offset + 3] = pixelColor.A; // Alpha
                            }
                        }
                    }
                }
            }

            return pixelData;
        }

        // Updates an existing WriteableBitmap with new pixel data from PixelGrid
        // This method reuses the bitmap instead of creating a new one, preventing memory leaks
        // Now uses dirty region tracking - only renders changed pixels
        // Returns dirty region size for logging purposes
        public async Task<(bool rendered, int dirtyWidth, int dirtyHeight)> UpdateBitmapAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) 
                return (false, 0, 0);

            int width = grid.Width * grid.PixelSize;
            int height = grid.Height * grid.PixelSize;
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

            // Calculate bitmap coordinates for dirty region
            int bitmapMinX = minX * grid.PixelSize;
            int bitmapMinY = minY * grid.PixelSize;
            int bitmapMaxX = (maxX + 1) * grid.PixelSize;
            int bitmapMaxY = (maxY + 1) * grid.PixelSize;

            // Clamp to bitmap bounds
            bitmapMinX = System.Math.Max(0, bitmapMinX);
            bitmapMinY = System.Math.Max(0, bitmapMinY);
            bitmapMaxX = System.Math.Min(width, bitmapMaxX);
            bitmapMaxY = System.Math.Min(height, bitmapMaxY);

            int dirtyWidth = bitmapMaxX - bitmapMinX;
            int dirtyHeight = bitmapMaxY - bitmapMinY;

            if (dirtyWidth <= 0 || dirtyHeight <= 0)
                return (false, 0, 0);

            int stride = bitmap.BackBufferStride;
            int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
            int pixelSize = grid.PixelSize;

            // Update only dirty region on UI thread (direct update is fast for small regions)
            bitmap.Lock();
            try
            {
                unsafe
                {
                    byte* buffer = (byte*)bitmap.BackBuffer;

                    // Render only dirty region
                    for (int gridY = minY; gridY <= maxY; gridY++)
                    {
                        for (int gridX = minX; gridX <= maxX; gridX++)
                        {
                            MediaColor pixelColor = grid.GetPixel(gridX, gridY);

                            // Draw this pixel scaled by PixelSize
                            for (int py = 0; py < pixelSize; py++)
                            {
                                for (int px = 0; px < pixelSize; px++)
                                {
                                    int x = gridX * pixelSize + px;
                                    int y = gridY * pixelSize + py;

                                    if (x >= bitmapMinX && x < bitmapMaxX && y >= bitmapMinY && y < bitmapMaxY)
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

                // Mark only dirty region as dirty (not entire bitmap)
                bitmap.AddDirtyRect(new System.Windows.Int32Rect(bitmapMinX, bitmapMinY, dirtyWidth, dirtyHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
            
            return (true, dirtyWidth, dirtyHeight);
        }
        
        // Full update method - renders entire bitmap (used for initial render or when needed)
        public async Task UpdateBitmapFullAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) return;

            int width = grid.Width * grid.PixelSize;
            int height = grid.Height * grid.PixelSize;
            width = System.Math.Max(1, width);
            height = System.Math.Max(1, height);

            // Check if bitmap size matches - if not, caller should create new bitmap
            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
                return;

            int stride = bitmap.BackBufferStride;
            int bytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel
            int totalBytes = stride * height;
            int pixelSize = grid.PixelSize;

            // For large canvases, prepare pixel data on background thread
            bool useBackgroundThread = (width * height) > 1000000; // 1M pixels threshold
            byte[]? pixelData = null;

            if (useBackgroundThread)
            {
                // Prepare pixel data on background thread
                pixelData = await Task.Run(() => PreparePixelData(grid, width, height, stride, bytesPerPixel, pixelSize));
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
                        // Small canvas: render directly on UI thread
                        for (int gridY = 0; gridY < grid.Height; gridY++)
                        {
                            for (int gridX = 0; gridX < grid.Width; gridX++)
                            {
                                MediaColor pixelColor = grid.GetPixel(gridX, gridY);

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
