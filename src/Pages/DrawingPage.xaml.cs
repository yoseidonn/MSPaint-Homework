using System.Threading.Tasks;
using System.Windows.Controls;
using MSPaint.Controls;
using MSPaint.Models;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace MSPaint.Pages
{
    public partial class DrawingPage : WpfUserControl
    {
        public DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return CanvasControl; // Reference from XAML
        }

        public DrawingPage()
        {
            InitializeComponent();
        }

        public async Task InitializeCanvas(CanvasSettings settings)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                await canvas.InitializeCanvas(settings);
            }
        }

        public async Task InitializeCanvas(PixelGrid grid)
        {
            var canvas = GetCanvasControl();
            if (canvas != null)
            {
                await canvas.InitializeCanvas(grid);
            }
        }
    }
}