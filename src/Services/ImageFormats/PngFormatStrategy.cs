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
    /// PNG format strategy - lossless, supports alpha channel
    /// Stateless singleton-safe implementation
    /// </summary>
    public class PngFormatStrategy : IImageFormatStrategy
    {
        public string[] SupportedExtensions => new[] { ".png" };
        public string FormatName => "PNG";
        public bool SupportsCustomMetadata => false; // PNG doesn't support custom metadata

        public async Task SaveAsync(WriteableBitmap bitmap, string filePath)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            // WriteableBitmap must be accessed on UI thread
            // Create a frozen copy that can be used on background thread
            // IMPORTANT: Ensure original bitmap is NOT frozen - only the copy should be frozen
            BitmapSource? frozenBitmap = null;
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // Check if bitmap is frozen (should never be for WriteableBitmap, but safety check)
                if (bitmap.IsFrozen)
                {
                    throw new InvalidOperationException("Cannot save frozen WriteableBitmap. Bitmap must be editable.");
                }

                // Create a frozen copy of the bitmap (thread-safe)
                // FormatConvertedBitmap creates a NEW BitmapSource, original WriteableBitmap remains unfrozen and editable
                // The original bitmap is NOT modified or frozen - only the converted copy is frozen
                var converted = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Pbgra32, null, 0);
                converted.Freeze(); // Only freeze the copy, original WriteableBitmap remains editable
                frozenBitmap = converted;
            });

            if (frozenBitmap == null) throw new InvalidOperationException("Failed to create frozen bitmap copy");

            // Now we can use the frozen bitmap on background thread for file I/O
            await Task.Run(() =>
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(frozenBitmap));

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

                    // Convert to Pbgra32 format for consistent pixel access
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
                            byte a = pixelData[offset + 3];

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
    }
}
