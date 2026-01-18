using MSPaint.Core;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.Tools
{
    /// <summary>
    /// Text tool - renders text to the canvas
    /// </summary>
    public class TextTool : ToolBase
    {
        private MediaColor _drawColor = MediaColors.Black;
        private int _fontSize = 12;
        private string _fontFamily = "Consolas";

        public TextTool(PixelGrid grid) : base(grid) { }

        public MediaColor DrawColor
        {
            get => _drawColor;
            set => _drawColor = value;
        }

        public int FontSize
        {
            get => _fontSize;
            set => _fontSize = Math.Max(1, Math.Min(50, value));
        }

        public string FontFamily
        {
            get => _fontFamily;
            set => _fontFamily = value ?? "Consolas";
        }

        public override void OnMouseDown(int x, int y)
        {
            // Show text input dialog
            // Note: Dialog will be shown from ViewModel/View layer
            // This method will be called after dialog returns
        }

        public override void OnMouseMove(int x, int y)
        {
            // Not used for text tool
        }

        public override void OnMouseUp(int x, int y)
        {
            // Not used for text tool
        }

        /// <summary>
        /// Render text to grid at specified position
        /// Called from ViewModel after text dialog returns
        /// </summary>
        public void RenderTextToGrid(int startX, int startY, string text, int fontSize)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                // Get PixelsPerDip from main window (for proper DPI scaling)
                double pixelsPerDip = 1.0;
                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    var source = System.Windows.PresentationSource.FromVisual(mainWindow);
                    if (source != null)
                    {
                        var dpi = source.CompositionTarget.TransformToDevice;
                        pixelsPerDip = 1.0 / dpi.M11; // M11 is the X scale factor
                    }
                }

                // Create FormattedText with PixelsPerDip
                var formattedText = new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface(new System.Windows.Media.FontFamily(_fontFamily), System.Windows.FontStyles.Normal, System.Windows.FontWeights.Normal, System.Windows.FontStretches.Normal),
                    fontSize,
                    new SolidColorBrush(_drawColor),
                    null,
                    TextFormattingMode.Display,
                    pixelsPerDip
                );

                // Calculate bitmap dimensions (add small padding to avoid clipping)
                int bitmapWidth = (int)Math.Ceiling(formattedText.Width) + 2;
                int bitmapHeight = (int)Math.Ceiling(formattedText.Height) + 2;

                if (bitmapWidth <= 0 || bitmapHeight <= 0) return;

                // Render to bitmap
                var renderTarget = new RenderTargetBitmap(
                    bitmapWidth,
                    bitmapHeight,
                    96, 96,
                    PixelFormats.Pbgra32
                );

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    // Draw text with small offset to avoid clipping
                    drawingContext.DrawText(formattedText, new System.Windows.Point(1, 1));
                }
                renderTarget.Render(drawingVisual);

                // Convert bitmap pixels to grid
                int stride = bitmapWidth * 4; // 4 bytes per pixel (BGRA)
                byte[] pixelData = new byte[stride * bitmapHeight];
                renderTarget.CopyPixels(pixelData, stride, 0);

                // Place pixels on grid
                for (int y = 0; y < bitmapHeight; y++)
                {
                    for (int x = 0; x < bitmapWidth; x++)
                    {
                        int offset = y * stride + x * 4;
                        byte a = pixelData[offset + 3];
                        
                        // Only place non-transparent pixels (alpha > 0)
                        if (a > 0)
                        {
                            int gridX = startX + x - 1; // Adjust for padding offset
                            int gridY = startY + y - 1;
                            
                            if (IsValidPosition(gridX, gridY))
                            {
                                byte r = pixelData[offset + 2];
                                byte g = pixelData[offset + 1];
                                byte b = pixelData[offset];
                                
                                // Use the actual rendered color (may have anti-aliasing)
                                var color = MediaColor.FromArgb(a, r, g, b);
                                SetPixelWithTracking(gridX, gridY, color);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                System.Diagnostics.Debug.WriteLine($"Error rendering text: {ex.Message}");
            }
        }
    }
}
