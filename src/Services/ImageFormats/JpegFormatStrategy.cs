using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MSPaint.Models;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Services.ImageFormats
{
    /// <summary>
    /// JPEG format strategy - lossy compression, no alpha channel
    /// Flattens alpha channel to white background
    /// Stateless singleton-safe implementation
    /// </summary>
    public class JpegFormatStrategy : IImageFormatStrategy
    {
        private const int DefaultQuality = 90; // 0-100, higher = better quality

        public string[] SupportedExtensions => new[] { ".jpg", ".jpeg" };
        public string FormatName => "JPEG";
        public bool SupportsCustomMetadata => false; // JPEG doesn't support custom metadata

        public async Task SaveAsync(WriteableBitmap bitmap, string filePath)
        {
            await SaveAsync(bitmap, filePath, DefaultQuality);
        }

        /// <summary>
        /// Save with custom quality (0-100)
        /// </summary>
        public async Task SaveAsync(WriteableBitmap bitmap, string filePath, int quality)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (quality < 0 || quality > 100) throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100");

            // WriteableBitmap must be accessed on UI thread
            // Flatten alpha channel and create frozen copy on UI thread
            // IMPORTANT: Ensure original bitmap is NOT frozen - only the copy should be frozen
            BitmapSource? rgbBitmap = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Check if bitmap is frozen (should never be for WriteableBitmap, but safety check)
                if (bitmap.IsFrozen)
                {
                    throw new InvalidOperationException("Cannot save frozen WriteableBitmap. Bitmap must be editable.");
                }

                // JPEG doesn't support alpha channel, so we need to flatten it
                // Convert to RGB format (flatten alpha to white background)
                // FlattenAlphaChannel creates a NEW WriteableBitmap and freezes it, original remains unfrozen and editable
                var flattened = FlattenAlphaChannel(bitmap);
                rgbBitmap = flattened; // Already frozen in FlattenAlphaChannel, original bitmap remains editable
            });

            if (rgbBitmap == null) throw new InvalidOperationException("Failed to create flattened bitmap");

            // Now we can use the frozen bitmap on background thread for file I/O
            await Task.Run(() =>
            {
                var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder();
                encoder.QualityLevel = quality;
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rgbBitmap));

                using (var stream = File.Create(filePath))
                {
                    encoder.Save(stream);
                }
            });
        }

        public async Task<PixelGrid?> LoadAsync(string filePath, CanvasSettings? settings = null)
        {
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (!File.Exists(filePath)) return null;

            return await Task.Run(() =>
            {
                try
                {
                    BitmapSource? bitmapSource = null;

                    using (var stream = File.OpenRead(filePath))
                    {
                        var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                            stream,
                            System.Windows.Media.Imaging.BitmapCreateOptions.None,
                            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);

                        bitmapSource = decoder.Frames[0];
                    }

                    if (bitmapSource == null) return null;

                    // Convert to Pbgra32 format (JPEG has no alpha, will be 255)
                    var convertedBitmap = new FormatConvertedBitmap(bitmapSource, System.Windows.Media.PixelFormats.Pbgra32, null, 0);
                    convertedBitmap.Freeze();

                    // Determine PixelSize from settings or use default
                    int pixelSize = settings?.PixelSize ?? 1;

                    // Create PixelGrid from bitmap
                    var width = convertedBitmap.PixelWidth;
                    var height = convertedBitmap.PixelHeight;

                    var pixelGrid = new PixelGrid(width, height, pixelSize);

                    // Copy pixels from bitmap to PixelGrid
                    var stride = (width * convertedBitmap.Format.BitsPerPixel + 7) / 8;
                    var pixelData = new byte[stride * height];
                    convertedBitmap.CopyPixels(pixelData, stride, 0);

                    // Convert byte array to PixelGrid
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int offset = y * stride + x * 4; // 4 bytes per pixel (BGRA)
                            byte b = pixelData[offset];
                            byte g = pixelData[offset + 1];
                            byte r = pixelData[offset + 2];
                            byte a = pixelData[offset + 3]; // Will be 255 for JPEG

                            var color = MediaColor.FromArgb(a, r, g, b);
                            pixelGrid.SetPixel(x, y, color);
                        }
                    }

                    return pixelGrid;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Flatten alpha channel to white background (for JPEG export)
        /// Copies pixel data first to avoid locked bitmap issues
        /// </summary>
        private BitmapSource FlattenAlphaChannel(WriteableBitmap bitmap)
        {
            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;
            var stride = bitmap.BackBufferStride;
            var bytesPerPixel = 4; // BGRA
            int totalBytes = stride * height;

            // First, copy pixel data from source bitmap (lock/unlock to avoid conflicts)
            byte[] sourcePixelData = new byte[totalBytes];
            bitmap.Lock();
            try
            {
                unsafe
                {
                    byte* sourceBuffer = (byte*)bitmap.BackBuffer;
                    System.Runtime.InteropServices.Marshal.Copy((System.IntPtr)sourceBuffer, sourcePixelData, 0, totalBytes);
                }
            }
            finally
            {
                bitmap.Unlock();
            }

            // Create new RGB bitmap (no alpha)
            var rgbBitmap = new WriteableBitmap(
                width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgr24, null);

            int destStride = rgbBitmap.BackBufferStride;
            int destTotalBytes = destStride * height;
            byte[] destPixelData = new byte[destTotalBytes];

            // Process pixel data (flatten alpha to white background)
            unsafe
            {
                fixed (byte* sourcePtr = sourcePixelData)
                fixed (byte* destPtr = destPixelData)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sourceOffset = y * stride + x * bytesPerPixel;
                            int destOffset = y * destStride + x * 3; // 3 bytes per pixel (BGR)

                            byte b = sourcePtr[sourceOffset];
                            byte g = sourcePtr[sourceOffset + 1];
                            byte r = sourcePtr[sourceOffset + 2];
                            byte a = sourcePtr[sourceOffset + 3];

                            // Alpha compositing: blend with white background
                            // result = source * (alpha/255) + white * (1 - alpha/255)
                            double alphaFactor = a / 255.0;
                            double invAlphaFactor = 1.0 - alphaFactor;

                            byte finalR = (byte)(r * alphaFactor + 255 * invAlphaFactor);
                            byte finalG = (byte)(g * alphaFactor + 255 * invAlphaFactor);
                            byte finalB = (byte)(b * alphaFactor + 255 * invAlphaFactor);

                            destPtr[destOffset] = finalB;
                            destPtr[destOffset + 1] = finalG;
                            destPtr[destOffset + 2] = finalR;
                        }
                    }
                }
            }

            // Copy processed data to destination bitmap
            rgbBitmap.Lock();
            try
            {
                unsafe
                {
                    byte* destBuffer = (byte*)rgbBitmap.BackBuffer;
                    System.Runtime.InteropServices.Marshal.Copy(destPixelData, 0, (System.IntPtr)destBuffer, destTotalBytes);
                }
                rgbBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            }
            finally
            {
                rgbBitmap.Unlock();
            }

            rgbBitmap.Freeze();
            return rgbBitmap;
        }
    }
}
