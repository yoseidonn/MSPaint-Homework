using MSPaint.Models;

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
    }
}