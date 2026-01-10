using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MSPaint.Models;
using MSPaint.Services.ImageFormats;

namespace MSPaint.Services
{
    /// <summary>
    /// Service for saving and loading image files
    /// Uses Strategy Pattern via ImageFormatStrategyFactory
    /// Memory-efficient: uses existing WriteableBitmap instead of creating new one
    /// </summary>
    public class ProjectIOService
    {
        /// <summary>
        /// Save bitmap to file (memory-efficient: uses existing bitmap)
        /// </summary>
        public async Task SaveAsync(string path, WriteableBitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));

            var strategy = ImageFormatStrategyFactory.GetStrategy(path);
            await strategy.SaveAsync(bitmap, path);
        }

        /// <summary>
        /// Load image from file and return as PixelGrid
        /// </summary>
        public async Task<PixelGrid?> LoadAsync(string path, CanvasSettings? settings = null)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path cannot be null or empty", nameof(path));

            var strategy = ImageFormatStrategyFactory.GetStrategy(path);
            return await strategy.LoadAsync(path, settings);
        }

        /// <summary>
        /// Get file dialog filter string for OpenFileDialog/SaveFileDialog
        /// </summary>
        public string GetFileDialogFilter()
        {
            return ImageFormatStrategyFactory.GetFileDialogFilter();
        }

        /// <summary>
        /// Check if file extension is supported
        /// </summary>
        public bool IsSupportedFormat(string extension)
        {
            return ImageFormatStrategyFactory.IsSupported(extension);
        }
    }
}