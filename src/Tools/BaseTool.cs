using MSPaint.Models;
using System.Windows.Media.Imaging;

namespace MSPaint.Tools
{
    public abstract class BaseTool : ITool
    {
        protected PixelGrid Grid;
        public BaseTool(PixelGrid grid)
        {
            Grid = grid;
        }

        public virtual void OnMouseDown(int x, int y) { }
        public virtual void OnMouseMove(int x, int y) { }
        public virtual void OnMouseUp(int x, int y) { }
        
        public virtual bool UsesPreview => false;
        public virtual void RenderPreview(WriteableBitmap previewBitmap, int pixelSize) { }
    }
}