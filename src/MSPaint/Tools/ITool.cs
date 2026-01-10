using System.Windows.Input;

namespace MSPaint.Tools
{
    public interface ITool
    {
        void OnMouseDown(int x, int y);
        void OnMouseMove(int x, int y);
        void OnMouseUp(int x, int y);
    }
}