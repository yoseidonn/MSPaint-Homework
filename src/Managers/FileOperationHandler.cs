using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MSPaint.Controls;
using MSPaint.Pages;
using MSPaint.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace MSPaint.Managers
{
    /// <summary>
    /// Handles file operations: New, Open, Save, Save As
    /// </summary>
    public class FileOperationHandler
    {
        private readonly MainWindow _mainWindow;
        private string? _currentFilePath;

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                _currentFilePath = value;
                UpdateWindowTitle();
            }
        }

        public FileOperationHandler(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void FileNew()
        {
            // Open canvas setup window for new canvas
            var setupWindow = new CanvasSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                _currentFilePath = null; // Reset current file path for new canvas
                _ = _mainWindow.InitializeCanvasAsync(setupWindow);
                UpdateWindowTitle(); // Update title to show "Untitled"
            }
        }

        public async Task FileOpen()
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return;

            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = canvas.GetFileDialogFilter(),
                Title = "Open Image File"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var ioService = new ProjectIOService();
                    var loadedGrid = await ioService.LoadAsync(openDialog.FileName);

                    if (loadedGrid != null)
                    {
                        await canvas.InitializeCanvas(loadedGrid);
                        _currentFilePath = openDialog.FileName;
                        UpdateWindowTitle();
                    }
                    else
                    {
                        WpfMessageBox.Show("Failed to load file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async Task FileSave()
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return;

            // If we have a current file path, save to it directly
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                try
                {
                    await canvas.SaveAsync(_currentFilePath);
                    UpdateWindowTitle();
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // No current file path, use Save As dialog
                // After Save As, _currentFilePath will be set, so next Save will use it
                await FileSaveAs();
            }
        }

        public async Task FileSaveAs()
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = canvas.GetFileDialogFilter(),
                Title = "Save Image As",
                FileName = _currentFilePath ?? "Untitled.png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    await canvas.SaveAsync(saveDialog.FileName);
                    _currentFilePath = saveDialog.FileName;
                    UpdateWindowTitle();
                }
                catch (Exception ex)
                {
                    WpfMessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateWindowTitle()
        {
            var fileName = string.IsNullOrEmpty(_currentFilePath) 
                ? "Untitled" 
                : Path.GetFileName(_currentFilePath);
            _mainWindow.Title = $"MSPaint - {fileName}";
        }

        private DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return _mainWindow.GetDrawingPage()?.GetCanvasControl();
        }
    }
}
