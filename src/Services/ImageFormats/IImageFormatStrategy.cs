using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MSPaint.Models;

namespace MSPaint.Services.ImageFormats
{
    /// <summary>
    /// Strategy interface for image format encoding/decoding
    /// Uses Strategy Pattern to support multiple image formats (PNG, JPEG, etc.)
    /// </summary>
    public interface IImageFormatStrategy
    {
        /// <summary>
        /// Save bitmap to file (memory-efficient: uses existing bitmap, doesn't create new one)
        /// </summary>
        Task SaveAsync(WriteableBitmap bitmap, string filePath);

        /// <summary>
        /// Load image from file and return as PixelGrid
        /// </summary>
        /// <param name="filePath">Path to image file</param>
        /// <param name="settings">Optional canvas settings (for PixelSize, etc.)</param>
        Task<PixelGrid?> LoadAsync(string filePath, CanvasSettings? settings = null);

        /// <summary>
        /// Supported file extensions (e.g., [".png"], [".jpg", ".jpeg"])
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// Human-readable format name (e.g., "PNG", "JPEG")
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Whether this format supports custom metadata (for future .mspaint format)
        /// </summary>
        bool SupportsCustomMetadata { get; }
    }
}
