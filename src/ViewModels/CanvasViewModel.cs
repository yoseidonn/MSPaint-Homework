using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MSPaint.Commands;
using MSPaint.Core;
using MSPaint.Services;
using MSPaint.Tools;

namespace MSPaint.ViewModels
{
    /// <summary>
    /// Canvas ViewModel - manages canvas state, tools, and rendering
    /// </summary>
    public class CanvasViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private PixelGrid? _pixelGrid;
        private WriteableBitmap? _bitmap;
        private ITool? _currentTool;
        private readonly HistoryService _history;
        private readonly RenderService _renderer;
        private int _pixelSize = 1;

        public CanvasViewModel()
        {
            _history = new HistoryService();
            _renderer = new RenderService();
        }

        public PixelGrid? PixelGrid
        {
            get => _pixelGrid;
            private set
            {
                _pixelGrid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasWidth));
                OnPropertyChanged(nameof(CanvasHeight));
            }
        }

        public WriteableBitmap? Bitmap
        {
            get => _bitmap;
            private set
            {
                _bitmap = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasWidth));
                OnPropertyChanged(nameof(CanvasHeight));
            }
        }

        public int CanvasWidth => PixelGrid != null ? PixelGrid.Width * PixelSize : 0;
        public int CanvasHeight => PixelGrid != null ? PixelGrid.Height * PixelSize : 0;

        public HistoryService History => _history;
        public RenderService Renderer => _renderer;

        public System.Windows.Input.ICommand UndoCommand => new RelayCommand(async () => await Undo());
        public System.Windows.Input.ICommand RedoCommand => new RelayCommand(async () => await Redo());

        public int PixelSize
        {
            get => _pixelSize;
            set
            {
                _pixelSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanvasWidth));
                OnPropertyChanged(nameof(CanvasHeight));
            }
        }

        public void Initialize(CanvasSettings settings)
        {
            PixelGrid = new PixelGrid(settings.Width, settings.Height);
            PixelSize = settings.PixelSize;
            _history.Clear();

            // Initialize all pixels with background color
            for (int y = 0; y < PixelGrid.Height; y++)
            {
                for (int x = 0; x < PixelGrid.Width; x++)
                {
                    PixelGrid.SetPixel(x, y, settings.Background);
                }
            }

            PixelGrid.MarkAllDirty();

            // Create and render bitmap
            Bitmap = _renderer.CreateBitmap(PixelGrid);
            _ = RenderAsync(force: true);
        }

        public void Initialize(PixelGrid grid)
        {
            PixelGrid = grid;
            _history.Clear();
            grid.MarkAllDirty();

            // Create and render bitmap
            Bitmap = _renderer.CreateBitmap(PixelGrid);
            _ = RenderAsync(force: true);
        }

        public void SetTool(ITool? tool)
        {
            _currentTool = tool;
        }

        public void SetMouseCaptured(bool captured)
        {
            IsMouseCaptured = captured;
        }

        public void OnMouseDown(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null) return;

            // Coordinates are already in pixel grid space (converted in CanvasControl)
            // Special handling for TextTool - show dialog immediately
            if (_currentTool is TextTool)
            {
                // TextTool will be handled by MainViewModel
                return;
            }

            // Start collecting changes if tool supports it (ToolBase)
            if (_currentTool is ToolBase toolBase)
            {
                toolBase.StartCollectingChanges();
            }
            _currentTool.OnMouseDown(x, y);
        }

        public async void OnMouseMove(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null || !IsMouseCaptured) return;

            // Coordinates are already in pixel grid space
            _currentTool.OnMouseMove(x, y);

            // Render preview if tool supports it
            if (_currentTool.UsesPreview && Bitmap != null)
            {
                // For preview tools, we need to clear the previous preview first
                // by re-rendering the grid, then draw the new preview
                await RenderAsync();
                // Then render preview on top (1:1 mapping since bitmap is already grid-sized)
                _currentTool.RenderPreview(Bitmap, 1);
            }
            else
            {
                // Render dirty region for tools without preview
                await RenderAsync();
            }
        }

        public void OnMouseUp(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null) return;

            // Coordinates are already in pixel grid space
            _currentTool.OnMouseUp(x, y);

            // Create command from collected changes
            List<(int x, int y, System.Windows.Media.Color oldColor, System.Windows.Media.Color newColor)>? changes = null;
            if (_currentTool is ToolBase toolBase)
            {
                changes = toolBase.StopCollectingChanges();
            }
            
            if (changes != null && changes.Count > 0)
            {
                var command = new PixelChangeCommand(PixelGrid, changes);
                if (command.HasChanges)
                {
                    _history.AddCommand(command);
                }
            }

            // Final render (clears any preview)
            _ = RenderAsync(force: true);
        }

        private bool IsMouseCaptured { get; set; }

        public async Task Undo()
        {
            if (_history.Undo())
            {
                await RenderAsync(force: true);
            }
        }

        public async Task Redo()
        {
            if (_history.Redo())
            {
                await RenderAsync(force: true);
            }
        }

        public async Task RenderAsync(bool force = false)
        {
            if (Bitmap == null || PixelGrid == null) return;

            if (force)
            {
                await _renderer.UpdateBitmapFullAsync(Bitmap, PixelGrid);
            }
            else
            {
                await _renderer.UpdateBitmapAsync(Bitmap, PixelGrid);
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
