using MSPaint.Models;

namespace MSPaint.Tools
{
    public class PencilTool : BaseTool
    {
        public PencilTool(PixelGrid grid) : base(grid) { }

        public override void OnMouseDown(int x, int y)
        {
            Grid.SetPixel(x, y, System.Windows.Media.Colors.Black);
        }
    }
}