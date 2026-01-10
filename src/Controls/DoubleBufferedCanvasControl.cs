using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MSPaint.Models;
using MSPaint.Services;
using MSPaint.Tools;
using MSPaint.Utils;
using MSPaint.Commands;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MSPaint.Controls
{
    public partial class DoubleBufferedCanvasControl : WpfUserControl
    {
        private PixelGrid? _pixelGrid;
        private RenderService _renderService;
        private HistoryService _historyService;
        private ProjectIOService _projectIOService;
        private ITool? _currentTool;
        private bool _isDrawing;
        private System.Windows.Point _lastMousePosition;
        private WriteableBitmap? _cachedBitmap;
        private WriteableBitmap? _previewBitmap;
        private DateTime _lastRenderTime = DateTime.MinValue;
        private const int MinRenderIntervalMs = 16; // ~60 FPS max
        private bool _bitmapSourceSet = false; // Track if Source has been set to avoid redundant assignments
        private bool _previewBitmapSourceSet = false;

        public PixelGrid? PixelGrid => _pixelGrid;

        public DoubleBufferedCanvasControl()
        {
            InitializeComponent();
            _renderService = new RenderService();
            _historyService = new HistoryService(maxHistorySize: 50);
            _projectIOService = new ProjectIOService();
            this.Loaded += DoubleBufferedCanvasControl_Loaded;
            
            // Enable mouse capture for smooth drawing
            this.MouseLeftButtonDown += DoubleBufferedCanvasControl_MouseLeftButtonDown;
            this.MouseMove += DoubleBufferedCanvasControl_MouseMove;
            this.MouseLeftButtonUp += DoubleBufferedCanvasControl_MouseLeftButtonUp;
        }

        public void SetTool(ITool tool)
        {
            _currentTool = tool;
        }

        public ITool? GetCurrentTool()
        {
            return _currentTool;
        }

        public async Task InitializeCanvas(CanvasSettings settings)
        {
            _pixelGrid = new PixelGrid(settings.Width, settings.Height, settings.PixelSize);
            
            // Clear cached bitmaps when reinitializing
            _cachedBitmap = null;
            _previewBitmap = null;
            _previewBitmapSourceSet = false;
            
            // Clear history when initializing new canvas
            _historyService.Clear();
            
            // Initialize all pixels with background color
            for (int y = 0; y < _pixelGrid.Height; y++)
            {
                for (int x = 0; x < _pixelGrid.Width; x++)
                {
                    _pixelGrid.SetPixel(x, y, settings.Background);
                }
            }
            
            // Mark all as dirty for initial render
            _pixelGrid.MarkAllDirty();

            // Initialize default tool
            _currentTool = new PencilTool(_pixelGrid);

            // Render the grid
            await RenderAsync(force: true);
            
            // Notify parent that canvas size has changed
            CanvasSizeChanged?.Invoke(this, EventArgs.Empty);
        }
        
        public event EventHandler? CanvasSizeChanged;

        public async Task InitializeCanvas(PixelGrid grid)
        {
            _pixelGrid = grid;
            
            // Clear cached bitmaps when reinitializing
            _cachedBitmap = null;
            _previewBitmap = null;
            _previewBitmapSourceSet = false;
            
            // Clear history when loading from file
            _historyService.Clear();
            
            // Mark all as dirty for initial render
            _pixelGrid.MarkAllDirty();
            
            _currentTool = new PencilTool(_pixelGrid);
            await RenderAsync(force: true);
            
            // Notify parent that canvas size has changed
            CanvasSizeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void DoubleBufferedCanvasControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Canvas will be initialized from setup window or MainWindow
            // This event handler is kept for compatibility but won't auto-initialize
        }

        private async void DoubleBufferedCanvasControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pixelGrid == null || _currentTool == null) return;

            CaptureMouse();
            _isDrawing = true;
            
            // Start collecting pixel changes for command pattern
            if (_currentTool is BaseTool baseTool)
            {
                baseTool.StartCollectingChanges();
            }
            
            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                _lastMousePosition = gridPosition.Value;
                _currentTool.OnMouseDown((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
                await RenderAsync(force: true);
            }
        }

        private async void DoubleBufferedCanvasControl_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (!_isDrawing || _pixelGrid == null || _currentTool == null) return;

            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                var currentPos = gridPosition.Value;
                
                // Only process if position changed (avoid duplicate calls)
                if (currentPos != _lastMousePosition)
                {
                    _currentTool.OnMouseMove((int)currentPos.X, (int)currentPos.Y);
                    _lastMousePosition = currentPos;
                    await RenderAsync();
                }
            }
        }

        private async void DoubleBufferedCanvasControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing || _pixelGrid == null || _currentTool == null) return;

            ReleaseMouseCapture();
            _isDrawing = false;

            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            var gridPosition = ScreenToGrid(position);
            
            if (gridPosition.HasValue)
            {
                _currentTool.OnMouseUp((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
                
                // Stop collecting changes and create command
                if (_currentTool is BaseTool baseTool)
                {
                    var pixelChanges = baseTool.StopCollectingChanges();
                    
                    if (pixelChanges != null && pixelChanges.Count > 0)
                    {
                        // Create command and add to history
                        var command = new DrawCommand(_pixelGrid, pixelChanges);
                        _historyService.AddCommand(command);
                    }
                }
                
                await RenderAsync(force: true);
            }
        }

        private System.Windows.Point? ScreenToGrid(System.Windows.Point screenPoint)
        {
            if (_pixelGrid == null) return null;

            // Convert screen coordinates to grid coordinates
            // screenPoint is already relative to BackImage, so we can directly divide by PixelSize
            int gridX = (int)(screenPoint.X / _pixelGrid.PixelSize);
            int gridY = (int)(screenPoint.Y / _pixelGrid.PixelSize);

            // Clamp to grid bounds
            if (gridX < 0 || gridX >= _pixelGrid.Width || gridY < 0 || gridY >= _pixelGrid.Height)
                return null;

            return new System.Windows.Point(gridX, gridY);
        }

        /// <summary>
        /// Undo the last drawing operation
        /// </summary>
        public async Task Undo()
        {
            if (_pixelGrid == null) return;

            if (_historyService.Undo())
            {
                // Mark all as dirty and re-render
                _pixelGrid.MarkAllDirty();
                await RenderAsync(force: true);
            }
        }

        /// <summary>
        /// Redo the last undone operation
        /// </summary>
        public async Task Redo()
        {
            if (_pixelGrid == null) return;

            if (_historyService.Redo())
            {
                // Mark all as dirty and re-render
                _pixelGrid.MarkAllDirty();
                await RenderAsync(force: true);
            }
        }

        /// <summary>
        /// Save current canvas to file (memory-efficient: uses existing cached bitmap)
        /// </summary>
        public async Task SaveAsync(string filePath)
        {
            if (_cachedBitmap == null)
            {
                throw new InvalidOperationException("No bitmap to save. Canvas must be rendered first.");
            }

            // Ensure bitmap is not frozen (should never be, but safety check)
            if (_cachedBitmap.IsFrozen)
            {
                throw new InvalidOperationException("Cannot save frozen bitmap. Bitmap must be editable.");
            }

            // Ensure bitmap is not locked (wait if necessary)
            // Note: WriteableBitmap doesn't have IsLocked property, but we can check by trying to access it
            // If locked, we'll get an exception which we'll handle
            
            await _projectIOService.SaveAsync(filePath, _cachedBitmap);
        }

        /// <summary>
        /// Get file dialog filter for save/load dialogs
        /// </summary>
        public string GetFileDialogFilter()
        {
            return _projectIOService.GetFileDialogFilter();
        }

        private async Task RenderAsync(bool force = false)
        {
            if (_pixelGrid == null) return;

            // Throttling: limit to 60 FPS to prevent excessive rendering
            if (!force)
            {
                var now = DateTime.Now;
                var elapsed = (now - _lastRenderTime).TotalMilliseconds;
                if (elapsed < MinRenderIntervalMs)
                {
                    Logger.LogRender("SKIPPED (throttled)", _pixelGrid.Width * _pixelGrid.PixelSize, _pixelGrid.Height * _pixelGrid.PixelSize, force);
                    return; // Skip this render to maintain frame rate
                }
                _lastRenderTime = now;
            }
            else
            {
                _lastRenderTime = DateTime.Now;
            }

            Logger.LogRender("START", _pixelGrid.Width, _pixelGrid.Height, force);

            try
            {
                // Calculate required dimensions - 1:1 with grid (NO PixelSize multiplication)
                int width = _pixelGrid.Width;
                int height = _pixelGrid.Height;
                width = System.Math.Max(1, width);
                height = System.Math.Max(1, height);

                // Create or reuse main bitmap
                bool bitmapCreated = false;
                if (_cachedBitmap == null || 
                    _cachedBitmap.PixelWidth != width || 
                    _cachedBitmap.PixelHeight != height)
                {
                    Logger.LogMemory("Creating new main bitmap", width * height * 4);
                    // Clear old bitmap reference before creating new one
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (BackImage.Source != null)
                        {
                            BackImage.Source = null; // Release old bitmap reference
                        }
                    });
                    
                    // Create new bitmap on UI thread (must be on UI thread)
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _cachedBitmap = new WriteableBitmap(
                            width, height, 96, 96, 
                            PixelFormats.Pbgra32, null);
                    });
                    bitmapCreated = true;
                    _bitmapSourceSet = false;
                    // Initial render - full update for new bitmap
                    _pixelGrid.MarkAllDirty();
                    if (_cachedBitmap != null)
                    {
                        await _renderService.UpdateBitmapFullAsync(_cachedBitmap, _pixelGrid);
                    }
                    Logger.LogRender("Main bitmap created and rendered (full)", width, height, force);
                }
                else
                {
                    // Update existing bitmap (reuse to prevent memory leak) - only dirty region
                    var (rendered, dirtyWidth, dirtyHeight) = await _renderService.UpdateBitmapAsync(_cachedBitmap, _pixelGrid);
                    if (rendered)
                    {
                        Logger.LogRender("Main bitmap updated (dirty region)", width, height, force, dirtyWidth, dirtyHeight);
                    }
                    else
                    {
                        Logger.LogRender("Main bitmap updated (no dirty region)", width, height, force);
                    }
                }

                // Handle preview layer if tool uses it
                bool needsPreview = _currentTool?.UsesPreview == true && _isDrawing;
                if (needsPreview)
                {
                    // Create or reuse preview bitmap (1:1 with grid)
                    if (_previewBitmap == null || 
                        _previewBitmap.PixelWidth != width || 
                        _previewBitmap.PixelHeight != height)
                    {
                        Logger.LogMemory("Creating new preview bitmap", width * height * 4);
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (FrontImage.Source != null)
                            {
                                FrontImage.Source = null;
                            }
                            _previewBitmap = new WriteableBitmap(
                                width, height, 96, 96,
                                PixelFormats.Pbgra32, null);
                        });
                        _previewBitmapSourceSet = false;
                    }
                    
                    // Clear preview bitmap
                    if (_previewBitmap == null) return; // Safety check
                    _previewBitmap.Lock();
                    try
                    {
                        unsafe
                        {
                            byte* buffer = (byte*)_previewBitmap.BackBuffer;
                            int stride = _previewBitmap.BackBufferStride;
                            int totalBytes = stride * height;
                            System.Runtime.InteropServices.Marshal.Copy(
                                new byte[totalBytes], 0, (System.IntPtr)buffer, totalBytes);
                        }
                        _previewBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                    }
                    finally
                    {
                        _previewBitmap.Unlock();
                    }
                    
                    // Render preview (1:1 mapping - no pixelSize parameter needed)
                    if (_currentTool != null && _previewBitmap != null)
                    {
                        _currentTool.RenderPreview(_previewBitmap, 1);
                    }
                    Logger.LogRender("Preview rendered", width, height, force);
                }
                else
                {
                    // Clear preview if not needed
                    if (_previewBitmap != null)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            FrontImage.Source = null;
                        });
                        _previewBitmap = null;
                        _previewBitmapSourceSet = false;
                    }
                }

                // Update UI on UI thread - scale images by PixelSize for visual display
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (_cachedBitmap != null)
                    {
                        // Only set Source if it's a new bitmap or hasn't been set yet
                        if (!_bitmapSourceSet || bitmapCreated)
                        {
                            BackImage.Source = _cachedBitmap;
                            _bitmapSourceSet = true;
                        }
                        // Scale image by PixelSize for visual display (bitmap is 1:1, UI scales it)
                        BackImage.Width = _cachedBitmap.PixelWidth * _pixelGrid.PixelSize;
                        BackImage.Height = _cachedBitmap.PixelHeight * _pixelGrid.PixelSize;
                    }
                    
                    // Update preview image - scale by PixelSize
                    if (needsPreview && _previewBitmap != null)
                    {
                        if (!_previewBitmapSourceSet)
                        {
                            FrontImage.Source = _previewBitmap;
                            _previewBitmapSourceSet = true;
                        }
                        FrontImage.Width = _previewBitmap.PixelWidth * _pixelGrid.PixelSize;
                        FrontImage.Height = _previewBitmap.PixelHeight * _pixelGrid.PixelSize;
                    }
                });
                
                Logger.LogRender("COMPLETE", width, height, force);
            }
            catch (Exception ex)
            {
                Logger.Log($"ERROR: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Error rendering canvas: {ex.Message}",
                    "Rendering Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}