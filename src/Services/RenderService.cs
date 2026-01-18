using MSPaint.Core;
using System;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Services
{
    /// <summary>
    /// Service for rendering PixelGrid to WriteableBitmap
    /// Uses dirty region tracking for efficient rendering
    /// </summary>
    public class RenderService
    {
        private const int LargeCanvasThreshold = 1000000; // 1M pixels threshold for background thread processing
        private const int BytesPerPixel = 4; // PBGRA32 = 4 bytes per pixel

        /// <summary>
        /// Creates a new WriteableBitmap from PixelGrid
        /// </summary>
        public WriteableBitmap CreateBitmap(PixelGrid grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            int width = Math.Max(1, grid.Width);
            int height = Math.Max(1, grid.Height);

            var bitmap = new WriteableBitmap(
                width,
                height,
                96, 96,
                PixelFormats.Pbgra32,
                null);

            return bitmap;
        }

        /// <summary>
        /// Updates an existing WriteableBitmap with new pixel data from PixelGrid (1:1 mapping)
        /// Uses dirty region tracking - only renders changed pixels
        /// </summary>
        public async Task<(bool rendered, int dirtyWidth, int dirtyHeight)> UpdateBitmapAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) 
                return (false, 0, 0);

            int width = Math.Max(1, grid.Width);
            int height = Math.Max(1, grid.Height);

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
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(grid.Width - 1, maxX);
            maxY = Math.Min(grid.Height - 1, maxY);

            int dirtyWidth = maxX - minX + 1;
            int dirtyHeight = maxY - minY + 1;

            if (dirtyWidth <= 0 || dirtyHeight <= 0)
                return (false, 0, 0);

            // Lock bitmap before accessing BackBuffer
            bitmap.Lock();
            try
            {
                // Render only dirty region
                RenderPixelRegion(bitmap, grid, minX, minY, maxX, maxY);
                
                // Mark only dirty region as dirty (not entire bitmap)
                bitmap.AddDirtyRect(new System.Windows.Int32Rect(minX, minY, dirtyWidth, dirtyHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
            
            return (true, dirtyWidth, dirtyHeight);
        }
        
        /// <summary>
        /// Full update method - renders entire bitmap (used for initial render or when needed)
        /// Uses 1:1 mapping - no PixelSize scaling
        /// </summary>
        public async Task UpdateBitmapFullAsync(WriteableBitmap bitmap, PixelGrid? grid)
        {
            if (bitmap == null || grid == null) return;

            int width = Math.Max(1, grid.Width);
            int height = Math.Max(1, grid.Height);

            // Check if bitmap size matches - if not, caller should create new bitmap
            if (bitmap.PixelWidth != width || bitmap.PixelHeight != height)
                return;

            int stride = bitmap.BackBufferStride;
            int totalBytes = stride * height;

            // For large canvases, prepare pixel data on background thread
            bool useBackgroundThread = (width * height) > LargeCanvasThreshold;
            byte[]? pixelData = null;

            if (useBackgroundThread)
            {
                // Prepare pixel data on background thread
                pixelData = await Task.Run(() => PreparePixelData(grid, width, height, stride, BytesPerPixel));
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
                        RenderPixelRegion(bitmap, grid, 0, 0, width - 1, height - 1);
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

        /// <summary>
        /// Render a rectangular region of pixels from grid to bitmap buffer
        /// </summary>
        private void RenderPixelRegion(WriteableBitmap bitmap, PixelGrid grid, int minX, int minY, int maxX, int maxY)
        {
            int stride = bitmap.BackBufferStride;
            
            unsafe
            {
                byte* buffer = (byte*)bitmap.BackBuffer;

                // Render specified region (1:1 mapping)
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        MediaColor pixelColor = grid.GetPixel(x, y);
                        int offset = y * stride + x * BytesPerPixel;
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
