using MSPaint.Models;
using System.Windows.Input;

namespace MSPaint.Services
{
    public class DrawingService
    {
        private PixelGrid _grid;
        public DrawingService(PixelGrid grid)
        {
            _grid = grid;
        }

        // placeholder methods
        public void BeginStroke(int x, int y)
        {
        }

        public void UpdateStroke(int x, int y)
        {
        }

        public void EndStroke(int x, int y)
        {
        }
    }
}