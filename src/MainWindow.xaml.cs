using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MSPaint.Controls;
using MSPaint.Managers;
using MSPaint.Pages;
using MSPaint.Tools;

namespace MSPaint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Pages.DrawingPage? _drawingPage;
        private ToolManager? _toolManager;
        private ColorManager? _colorManager;
        private FileOperationHandler? _fileHandler;
        private KeyboardShortcutHandler? _keyboardHandler;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // At startup we mount the drawing page control into the CanvasHost
                _drawingPage = new Pages.DrawingPage();
                CanvasHost.Content = _drawingPage;

                // Initialize managers
                _toolManager = new ToolManager(this);
                _colorManager = new ColorManager(this, _toolManager);
                _fileHandler = new FileOperationHandler(this);
                _keyboardHandler = new KeyboardShortcutHandler(this, _fileHandler);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error initializing MainWindow: {ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                throw; // Re-throw to let App.xaml.cs handle it
            }
        }

        public Pages.DrawingPage? GetDrawingPage()
        {
            return _drawingPage;
        }

        public async Task InitializeCanvasAsync(CanvasSetupWindow setupWindow)
        {
            // Wait for window to be fully rendered
            await Task.Delay(100);
            
            if (setupWindow.LoadedGrid != null)
            {
                // Load from file
                var page = GetDrawingPage();
                if (page != null)
                {
                    await page.InitializeCanvas(setupWindow.LoadedGrid);
                }
            }
            else if (setupWindow.Result != null)
            {
                // Create new canvas with settings
                var page = GetDrawingPage();
                if (page != null)
                {
                    await page.InitializeCanvas(setupWindow.Result);
                }
            }
        }

        // Tool selection handlers
        private void PencilButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectPencilTool(_colorManager?.PrimaryColor ?? System.Windows.Media.Colors.Black);
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectEraserTool(_colorManager?.SecondaryColor ?? System.Windows.Media.Colors.White);
        }

        private void FillButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectFillTool(_colorManager?.PrimaryColor ?? System.Windows.Media.Colors.Black);
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectRectangleTool(_colorManager?.PrimaryColor ?? System.Windows.Media.Colors.Black);
        }

        private void EllipseButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectEllipseTool(_colorManager?.PrimaryColor ?? System.Windows.Media.Colors.Black);
        }

        private void TextButton_Click(object sender, RoutedEventArgs e)
        {
            _toolManager?.SelectTextTool(_colorManager?.PrimaryColor ?? System.Windows.Media.Colors.Black);
        }

        // Color picker handlers
        private void PrimaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _colorManager?.ShowColorPicker(true);
        }

        private void SecondaryColorPreview_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _colorManager?.ShowColorPicker(false);
        }

        private void ColorSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string colorName)
            {
                _colorManager?.HandleColorSwatchClick(colorName);
            }
        }

        // Keyboard shortcut handling
        private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_keyboardHandler != null)
            {
                await _keyboardHandler.HandleKeyDown(e);
            }
        }

        // File menu handlers
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            _fileHandler?.FileNew();
        }

        private async void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_fileHandler != null)
            {
                await _fileHandler.FileOpen();
            }
        }

        private async void FileSave_Click(object sender, RoutedEventArgs e)
        {
            if (_fileHandler != null)
            {
                await _fileHandler.FileSave();
            }
        }

        private async void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (_fileHandler != null)
            {
                await _fileHandler.FileSaveAs();
            }
        }
    }
}