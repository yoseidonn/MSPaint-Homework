using System.Windows;

namespace MSPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Pages.DrawingPage? _drawingPage;

        public MainWindow()
        {
            InitializeComponent();

            // At startup we mount the drawing page control into the CanvasHost
            _drawingPage = new Pages.DrawingPage();
            CanvasHost.Content = _drawingPage;
        }

        public Pages.DrawingPage? GetDrawingPage()
        {
            return _drawingPage;
        }
    }
}