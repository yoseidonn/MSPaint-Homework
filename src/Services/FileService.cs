using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MSPaint.Core;

namespace MSPaint.Services
{
    /// <summary>
    /// Service for saving and loading image files
    /// Supports PNG and BMP formats
    /// </summary>
    public class FileService
    {
        /// <summary>
        /// Save PixelGrid to file
        /// </summary>
        public async Task SaveAsync(string path, PixelGrid grid, int pixelSize = 1)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Create bitmap from PixelGrid
            var renderService = new RenderService();
            var bitmap = renderService.CreateBitmap(grid);
            await renderService.UpdateBitmapFullAsync(bitmap, grid);

            // Scale bitmap if pixelSize > 1
            WriteableBitmap finalBitmap = bitmap;
            if (pixelSize > 1)
            {
                finalBitmap = ScaleBitmap(bitmap, pixelSize);
            }

            // Save based on file extension
            var extension = Path.GetExtension(path).ToLower();
            await SaveBitmapAsync(finalBitmap, path, extension);

            // Clean up scaled bitmap if created
            if (finalBitmap != bitmap)
            {
                finalBitmap = null;
            }
        }

        /// <summary>
        /// Load image from file and return as PixelGrid
        /// </summary>
        public async Task<PixelGrid?> LoadAsync(string path, int pixelSize = 1)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));
            if (!File.Exists(path)) return null;

            return await Task.Run(() =>
            {
                try
                {
                    BitmapSource? bitmapSource = null;

                    using (var stream = File.OpenRead(path))
                    {
                        var decoder = BitmapDecoder.Create(
                            stream,
                            BitmapCreateOptions.None,
                            BitmapCacheOption.OnLoad);

                        bitmapSource = decoder.Frames[0];
                    }

                    if (bitmapSource == null) return null;

                    // Convert to Pbgra32 format for consistent pixel access
                    BitmapSource convertedBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Pbgra32, null, 0);
                    convertedBitmap.Freeze();

                    // Scale down if pixelSize > 1 (load at 1:1, then scale down)
                    if (pixelSize > 1)
                    {
                        int scaledWidth = convertedBitmap.PixelWidth / pixelSize;
                        int scaledHeight = convertedBitmap.PixelHeight / pixelSize;
                        
                        var scaledBitmap = new TransformedBitmap(
                            convertedBitmap,
                            new System.Windows.Media.ScaleTransform(1.0 / pixelSize, 1.0 / pixelSize));
                        scaledBitmap.Freeze();
                        convertedBitmap = scaledBitmap;
                    }

                    // Create PixelGrid from bitmap
                    var width = convertedBitmap.PixelWidth;
                    var height = convertedBitmap.PixelHeight;

                    var pixelGrid = new PixelGrid(width, height);

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
                            byte a = pixelData[offset + 3];

                            var color = System.Windows.Media.Color.FromArgb(a, r, g, b);
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
        /// Get file dialog filter string for OpenFileDialog/SaveFileDialog
        /// </summary>
        public string GetFileDialogFilter()
        {
            return "PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|All Files (*.*)|*.*";
        }

        /// <summary>
        /// Check if file extension is supported
        /// </summary>
        public bool IsSupportedFormat(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            extension = extension.ToLower();
            if (!extension.StartsWith(".")) extension = "." + extension;
            return extension == ".png" || extension == ".bmp";
        }

        private WriteableBitmap ScaleBitmap(WriteableBitmap source, int scaleFactor)
        {
            int newWidth = source.PixelWidth * scaleFactor;
            int newHeight = source.PixelHeight * scaleFactor;

            var scaled = new WriteableBitmap(
                newWidth,
                newHeight,
                96, 96,
                PixelFormats.Pbgra32,
                null);

            // Simple nearest-neighbor scaling
            scaled.Lock();
            try
            {
                unsafe
                {
                    byte* sourceBuffer = (byte*)source.BackBuffer;
                    byte* destBuffer = (byte*)scaled.BackBuffer;
                    int sourceStride = source.BackBufferStride;
                    int destStride = scaled.BackBufferStride;

                    for (int y = 0; y < newHeight; y++)
                    {
                        int sourceY = y / scaleFactor;
                        for (int x = 0; x < newWidth; x++)
                        {
                            int sourceX = x / scaleFactor;
                            
                            int sourceOffset = sourceY * sourceStride + sourceX * 4;
                            int destOffset = y * destStride + x * 4;

                            destBuffer[destOffset] = sourceBuffer[sourceOffset];
                            destBuffer[destOffset + 1] = sourceBuffer[sourceOffset + 1];
                            destBuffer[destOffset + 2] = sourceBuffer[sourceOffset + 2];
                            destBuffer[destOffset + 3] = sourceBuffer[sourceOffset + 3];
                        }
                    }
                }
                scaled.AddDirtyRect(new System.Windows.Int32Rect(0, 0, newWidth, newHeight));
            }
            finally
            {
                scaled.Unlock();
            }

            return scaled;
        }

        private async Task SaveBitmapAsync(WriteableBitmap bitmap, string path, string extension)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            // Create frozen copy for thread-safe access
            BitmapSource? frozenBitmap = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (bitmap.IsFrozen)
                {
                    frozenBitmap = bitmap;
                }
                else
                {
                    // Copy pixel data
                    int width = bitmap.PixelWidth;
                    int height = bitmap.PixelHeight;
                    int stride = bitmap.BackBufferStride;
                    int totalBytes = stride * height;

                    byte[] pixelData = new byte[totalBytes];
                    bitmap.Lock();
                    try
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)bitmap.BackBuffer;
                            System.Runtime.InteropServices.Marshal.Copy((System.IntPtr)buffer, pixelData, 0, totalBytes);
                        }
                    }
                    finally
                    {
                        bitmap.Unlock();
                    }

                    var copiedBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
                    copiedBitmap.Lock();
                    try
                    {
                        unsafe
                        {
                            byte* destBuffer = (byte*)copiedBitmap.BackBuffer;
                            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, (System.IntPtr)destBuffer, totalBytes);
                        }
                        copiedBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                    }
                    finally
                    {
                        copiedBitmap.Unlock();
                    }

                    var converted = new FormatConvertedBitmap(copiedBitmap, PixelFormats.Pbgra32, null, 0);
                    converted.Freeze();
                    frozenBitmap = converted;
                }
            });

            if (frozenBitmap == null) throw new InvalidOperationException("Failed to create frozen bitmap copy");

            // Save on background thread
            await Task.Run(() =>
            {
                BitmapEncoder encoder = extension switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".bmp" => new BmpBitmapEncoder(),
                    _ => throw new NotSupportedException($"Format {extension} is not supported")
                };

                encoder.Frames.Add(BitmapFrame.Create(frozenBitmap));

                using (var stream = File.Create(path))
                {
                    encoder.Save(stream);
                }
            });
        }
    }
}
