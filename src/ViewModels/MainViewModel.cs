using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MSPaint.Commands;
using MSPaint.Core;
using MSPaint.Services;
using MSPaint.Tools;
using MSPaint.Views;
using Microsoft.Win32;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace MSPaint.ViewModels
{
    /// <summary>
    /// Main ViewModel - manages tool selection, colors, and file operations
    /// </summary>
    public class MainViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private ITool? _currentTool;
        private MediaColor _primaryColor = MediaColors.Black;
        private MediaColor _secondaryColor = MediaColors.White;
        private string? _currentFilePath;
        private CanvasViewModel _canvas;

        public MainViewModel()
        {
            _canvas = new CanvasViewModel();
            _canvas.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Canvas));
        }

        public CanvasViewModel Canvas
        {
            get => _canvas;
            private set
            {
                _canvas = value;
                OnPropertyChanged();
            }
        }

        public ITool? CurrentTool
        {
            get => _currentTool;
            set
            {
                _currentTool = value;
                _canvas.SetTool(value);
                OnPropertyChanged();
            }
        }

        public MediaColor PrimaryColor
        {
            get => _primaryColor;
            set
            {
                _primaryColor = value;
                UpdateToolColor(value);
                OnPropertyChanged();
            }
        }

        public MediaColor SecondaryColor
        {
            get => _secondaryColor;
            set
            {
                _secondaryColor = value;
                OnPropertyChanged();
            }
        }

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                _currentFilePath = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand SelectToolCommand => new RelayCommand<string>(SelectTool);
        public ICommand SelectColorCommand => new RelayCommand<MediaColor>(color => SelectColor(color, true));
        public ICommand FileNewCommand => new RelayCommand(FileNew);
        public ICommand FileOpenCommand => new RelayCommand(FileOpen);
        public ICommand FileSaveCommand => new RelayCommand(FileSave);
        public ICommand FileSaveAsCommand => new RelayCommand(FileSaveAs);

        public void SelectTool(string? toolName)
        {
            if (string.IsNullOrEmpty(toolName) || Canvas.PixelGrid == null) return;

            CurrentTool = toolName switch
            {
                "Pencil" => new PencilTool(Canvas.PixelGrid) { DrawColor = PrimaryColor },
                "Rectangle" => new RectangleTool(Canvas.PixelGrid) { DrawColor = PrimaryColor },
                "Ellipse" => new EllipseTool(Canvas.PixelGrid) { DrawColor = PrimaryColor },
                "Text" => new TextTool(Canvas.PixelGrid) { DrawColor = PrimaryColor },
                "Fill" => new FillTool(Canvas.PixelGrid) { FillColor = PrimaryColor },
                "Eraser" => new EraserTool(Canvas.PixelGrid) { EraseColor = SecondaryColor },
                _ => null
            };
        }

        public async void HandleTextToolClick(int x, int y)
        {
            if (CurrentTool is TextTool textTool && Canvas.PixelGrid != null)
            {
                var dialog = new TextInputDialog(textTool.FontSize);
                dialog.Owner = Application.Current.MainWindow;
                
                if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Text))
                {
                    textTool.FontSize = dialog.FontSize;
                    textTool.StartCollectingChanges();
                    textTool.RenderTextToGrid(x, y, dialog.Text, dialog.FontSize);
                    var changes = textTool.StopCollectingChanges();
                    
                    if (changes != null && changes.Count > 0)
                    {
                        var command = new Commands.PixelChangeCommand(Canvas.PixelGrid, changes);
                        if (command.HasChanges)
                        {
                            Canvas.History.AddCommand(command);
                            await Canvas.RenderAsync();
                        }
                    }
                }
            }
        }

        public void SelectColor(MediaColor color, bool isPrimary)
        {
            if (isPrimary)
            {
                PrimaryColor = color;
            }
            else
            {
                SecondaryColor = color;
            }
        }

        public void FileNew()
        {
            var dialog = new CanvasSetupDialog();
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                CurrentFilePath = null;
                Canvas.Initialize(dialog.Result);
            }
        }

        public async void FileOpen()
        {
            var openDialog = new OpenFileDialog
            {
                Filter = new FileService().GetFileDialogFilter(),
                Title = "Open Image File"
            };

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    var fileService = new FileService();
                    var loadedGrid = await fileService.LoadAsync(openDialog.FileName);

                    if (loadedGrid != null)
                    {
                        Canvas.Initialize(loadedGrid);
                        CurrentFilePath = openDialog.FileName;
                    }
                    else
                    {
                        MessageBox.Show("Failed to load file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public async void FileSave()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                FileSaveAs();
                return;
            }

            try
            {
                await SaveToFile(CurrentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void FileSaveAs()
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = new FileService().GetFileDialogFilter(),
                Title = "Save Image As",
                FileName = CurrentFilePath ?? "Untitled.png"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    await SaveToFile(saveDialog.FileName);
                    CurrentFilePath = saveDialog.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async System.Threading.Tasks.Task SaveToFile(string path)
        {
            if (Canvas.PixelGrid == null) return;

            var fileService = new FileService();
            // Get pixel size from canvas settings if available, otherwise use 1
            int pixelSize = 1; // TODO: Get from canvas settings
            await fileService.SaveAsync(path, Canvas.PixelGrid, pixelSize);
        }

        private void UpdateToolColor(MediaColor color)
        {
            if (CurrentTool == null) return;

            switch (CurrentTool)
            {
                case PencilTool pencil:
                    pencil.DrawColor = color;
                    break;
                case RectangleTool rect:
                    rect.DrawColor = color;
                    break;
                case EllipseTool ellipse:
                    ellipse.DrawColor = color;
                    break;
                case TextTool text:
                    text.DrawColor = color;
                    break;
                case FillTool fill:
                    fill.FillColor = color;
                    break;
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Simple RelayCommand implementation for ICommand
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
