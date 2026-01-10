using System.Windows;

namespace MSPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // At startup we mount the drawing page control into the CanvasHost
            var page = new Pages.DrawingPage();
            CanvasHost.Content = page;
        }
    }
}