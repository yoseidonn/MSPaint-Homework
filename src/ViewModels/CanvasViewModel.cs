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
            }
        }

        public WriteableBitmap? Bitmap
        {
            get => _bitmap;
            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        public HistoryService History => _history;
        public RenderService Renderer => _renderer;

        public System.Windows.Input.ICommand UndoCommand => new MainViewModel.RelayCommand(async () => await Undo());
        public System.Windows.Input.ICommand RedoCommand => new MainViewModel.RelayCommand(async () => await Redo());

        public int PixelSize
        {
            get => _pixelSize;
            set
            {
                _pixelSize = value;
                OnPropertyChanged();
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

        public void OnMouseDown(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null) return;

            // Convert screen coordinates to pixel grid coordinates
            int gridX = x / PixelSize;
            int gridY = y / PixelSize;

            // Special handling for TextTool - show dialog immediately
            if (_currentTool is TextTool)
            {
                // TextTool will be handled by MainViewModel
                return;
            }

            _currentTool.StartCollectingChanges();
            _currentTool.OnMouseDown(gridX, gridY);
        }

        public void OnMouseMove(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null) return;

            // Convert screen coordinates to pixel grid coordinates
            int gridX = x / PixelSize;
            int gridY = y / PixelSize;

            _currentTool.OnMouseMove(gridX, gridY);

            // Render preview if tool supports it
            if (_currentTool.UsesPreview && Bitmap != null)
            {
                _currentTool.RenderPreview(Bitmap, PixelSize);
            }
            else
            {
                // Render dirty region for tools without preview
                _ = RenderAsync();
            }
        }

        public void OnMouseUp(int x, int y)
        {
            if (_currentTool == null || PixelGrid == null) return;

            // Convert screen coordinates to pixel grid coordinates
            int gridX = x / PixelSize;
            int gridY = y / PixelSize;

            _currentTool.OnMouseUp(gridX, gridY);

            // Create command from collected changes
            var changes = _currentTool.StopCollectingChanges();
            if (changes != null && changes.Count > 0)
            {
                var command = new PixelChangeCommand(PixelGrid, changes);
                if (command.HasChanges)
                {
                    _history.AddCommand(command);
                }
            }

            // Final render
            _ = RenderAsync();
        }

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
