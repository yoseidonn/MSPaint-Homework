using MSPaint.Models;

namespace MSPaint.Tools
{
    public class EraserTool : BaseTool
    {
        public EraserTool(PixelGrid grid) : base(grid) { }

        public override void OnMouseDown(int x, int y)
        {
            Grid.SetPixel(x, y, System.Windows.Media.Colors.Transparent);
        }
    }
}