using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MSPaint.Services.ImageFormats
{
    /// <summary>
    /// Factory for creating image format strategies
    /// Uses Singleton pattern to cache strategy instances (stateless, GC-friendly)
    /// </summary>
    public class ImageFormatStrategyFactory
    {
        private static readonly Dictionary<string, IImageFormatStrategy> _strategies;
        private static readonly object _lock = new object();

        static ImageFormatStrategyFactory()
        {
            // Initialize singleton strategy instances (stateless, reusable)
            var pngStrategy = new PngFormatStrategy();
            var jpegStrategy = new JpegFormatStrategy();

            _strategies = new Dictionary<string, IImageFormatStrategy>(StringComparer.OrdinalIgnoreCase)
            {
                [".png"] = pngStrategy,
                [".jpg"] = jpegStrategy,
                [".jpeg"] = jpegStrategy // Reuse same instance
            };
        }

        /// <summary>
        /// Get strategy by file path (extracts extension automatically)
        /// </summary>
        public static IImageFormatStrategy GetStrategy(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var extension = Path.GetExtension(filePath);
            return GetStrategyByExtension(extension);
        }

        /// <summary>
        /// Get strategy by file extension
        /// </summary>
        public static IImageFormatStrategy GetStrategyByExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentException("Extension cannot be null or empty", nameof(extension));

            // Normalize extension (ensure it starts with dot)
            if (!extension.StartsWith("."))
                extension = "." + extension;

            lock (_lock)
            {
                if (_strategies.TryGetValue(extension, out var strategy))
                    return strategy;

                throw new NotSupportedException($"Image format '{extension}' is not supported. Supported formats: {string.Join(", ", _strategies.Keys)}");
            }
        }

        /// <summary>
        /// Get all supported formats
        /// </summary>
        public static IEnumerable<IImageFormatStrategy> GetSupportedFormats()
        {
            lock (_lock)
            {
                // Return unique strategies (avoid duplicates like .jpg and .jpeg)
                return _strategies.Values.Distinct();
            }
        }

        /// <summary>
        /// Get file filter string for OpenFileDialog/SaveFileDialog
        /// Format: "PNG Files (*.png)|*.png|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|All Files (*.*)|*.*"
        /// </summary>
        public static string GetFileDialogFilter()
        {
            lock (_lock)
            {
                var filters = new List<string>();

                // Add format-specific filters
                var formatGroups = _strategies
                    .GroupBy(kvp => kvp.Value.FormatName)
                    .OrderBy(g => g.Key);

                foreach (var group in formatGroups)
                {
                    var extensions = group.Select(kvp => "*" + kvp.Key).ToArray();
                    var extensionList = string.Join(";", extensions);
                    filters.Add($"{group.Key} Files ({extensionList})|{extensionList}");
                }

                // Add "All Supported" filter
                var allExtensions = _strategies.Keys.Select(ext => "*" + ext).ToArray();
                var allExtensionsList = string.Join(";", allExtensions);
                filters.Add($"All Supported Images ({allExtensionsList})|{allExtensionsList}");

                // Add "All Files" filter
                filters.Add("All Files (*.*)|*.*");

                return string.Join("|", filters);
            }
        }

        /// <summary>
        /// Check if file extension is supported
        /// </summary>
        public static bool IsSupported(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;

            if (!extension.StartsWith("."))
                extension = "." + extension;

            lock (_lock)
            {
                return _strategies.ContainsKey(extension);
            }
        }
    }
}
