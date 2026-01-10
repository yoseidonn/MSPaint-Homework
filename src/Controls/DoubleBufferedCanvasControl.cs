using System.Windows.Controls;
using System.Windows.Input;

namespace MSPaint.Controls
{
    public partial class DoubleBufferedCanvasControl : UserControl
    {
        public DoubleBufferedCanvasControl()
        {
            InitializeComponent();
            this.Loaded += DoubleBufferedCanvasControl_Loaded;
        }

        private void DoubleBufferedCanvasControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // placeholder
        }

        // Mouse events will be forwarded to drawing service / tools later
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
        }
    }
}