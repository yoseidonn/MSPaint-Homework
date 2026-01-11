using System.Threading.Tasks;
using System.Windows.Input;
using MSPaint.Controls;
using MSPaint.Managers;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace MSPaint.Managers
{
    /// <summary>
    /// Handles keyboard shortcuts for the application
    /// </summary>
    public class KeyboardShortcutHandler
    {
        private readonly MainWindow _mainWindow;
        private readonly FileOperationHandler _fileHandler;

        public KeyboardShortcutHandler(MainWindow mainWindow, FileOperationHandler fileHandler)
        {
            _mainWindow = mainWindow;
            _fileHandler = fileHandler;
        }

        public async Task<bool> HandleKeyDown(WpfKeyEventArgs e)
        {
            var canvas = GetCanvasControl();
            if (canvas == null) return false;

            // Ctrl+Z: Undo
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await canvas.Undo();
                e.Handled = true;
                return true;
            }
            // Ctrl+Shift+Z or Ctrl+Y: Redo
            else if ((e.Key == Key.Z && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) ||
                     (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control))
            {
                await canvas.Redo();
                e.Handled = true;
                return true;
            }
            // Ctrl+S: Save
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await _fileHandler.FileSave();
                e.Handled = true;
                return true;
            }
            // Ctrl+Shift+S: Save As
            else if (e.Key == Key.S && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                await _fileHandler.FileSaveAs();
                e.Handled = true;
                return true;
            }
            // Ctrl+O: Open
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await _fileHandler.FileOpen();
                e.Handled = true;
                return true;
            }
            // Ctrl+N: New
            else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _fileHandler.FileNew();
                e.Handled = true;
                return true;
            }

            return false;
        }

        private DoubleBufferedCanvasControl? GetCanvasControl()
        {
            return _mainWindow.GetDrawingPage()?.GetCanvasControl();
        }
    }
}
