using System.Windows.Media.Imaging;

namespace MSPaint.Tools
{
    /// <summary>
    /// Interface for drawing tools
    /// </summary>
    public interface ITool
    {
        void OnMouseDown(int x, int y);
        void OnMouseMove(int x, int y);
        void OnMouseUp(int x, int y);
        
        // Preview support - returns true if tool uses preview layer
        bool UsesPreview { get; }
        
        // Render preview to bitmap (only called if UsesPreview returns true)
        void RenderPreview(WriteableBitmap previewBitmap, int pixelSize);
    }
}
