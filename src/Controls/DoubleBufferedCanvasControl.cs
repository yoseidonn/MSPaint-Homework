using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MSPaint.Controls.Helpers;
using CanvasMouseEventHandler = MSPaint.Controls.Helpers.MouseEventHandler;
using MSPaint.Models;
using MSPaint.Services;
using MSPaint.Tools;
using MSPaint.Utils;
using MSPaint.Commands;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMessageBox = System.Windows.MessageBox;
using WpfApplication = System.Windows.Application;
using MediaColor = System.Windows.Media.Color;

namespace MSPaint.Controls
{
    public partial class DoubleBufferedCanvasControl : WpfUserControl
    {
        private PixelGrid? _pixelGrid;
        private RenderService _renderService;
        private HistoryService _historyService;
        private ProjectIOService _projectIOService;
        private BitmapManager _bitmapManager;
        private CanvasMouseEventHandler? _mouseHandler;
        private ITool? _currentTool;
        private DateTime _lastRenderTime = DateTime.MinValue;
        private const int MinRenderIntervalMs = 16; // ~60 FPS max

        public PixelGrid? PixelGrid => _pixelGrid;

        public DoubleBufferedCanvasControl()
        {
            InitializeComponent();
            _renderService = new RenderService();
            _historyService = new HistoryService(maxHistorySize: 50);
            _projectIOService = new ProjectIOService();
            _bitmapManager = new BitmapManager();
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
            _mouseHandler = new CanvasMouseEventHandler(_pixelGrid);
            
            // Clear cached bitmaps when reinitializing
            _bitmapManager.ClearAll();
            
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
            _mouseHandler = new CanvasMouseEventHandler(_pixelGrid);
            
            // Clear cached bitmaps when reinitializing
            _bitmapManager.ClearAll();
            
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
            if (_mouseHandler == null || _currentTool == null) return;

            CaptureMouse();
            _mouseHandler.IsDrawing = true;
            
            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            
            _mouseHandler.HandleMouseDown(position, _currentTool, (baseTool) => baseTool.StartCollectingChanges());
            await RenderAsync(force: true);
        }

        private async void DoubleBufferedCanvasControl_MouseMove(object sender, WpfMouseEventArgs e)
        {
            if (_mouseHandler == null || _currentTool == null) return;

            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            
            if (_mouseHandler.HandleMouseMove(position, _currentTool))
            {
                await RenderAsync();
            }
        }

        private async void DoubleBufferedCanvasControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseHandler == null || _currentTool == null) return;

            ReleaseMouseCapture();
            _mouseHandler.IsDrawing = false;

            // Get position relative to BackImage (the actual rendered canvas)
            var position = e.GetPosition(BackImage);
            
            _mouseHandler.HandleMouseUp(position, _currentTool, (baseTool, pixelChanges) =>
            {
                if (pixelChanges != null && pixelChanges.Count > 0 && _pixelGrid != null)
                {
                    // Create command and add to history
                    var command = new DrawCommand(_pixelGrid, pixelChanges);
                    _historyService.AddCommand(command);
                }
            });
            
            await RenderAsync(force: true);
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
            var bitmap = _bitmapManager.CachedBitmap;
            if (bitmap == null)
            {
                throw new InvalidOperationException("No bitmap to save. Canvas must be rendered first.");
            }

            // Ensure bitmap is not frozen (should never be, but safety check)
            if (bitmap.IsFrozen)
            {
                throw new InvalidOperationException("Cannot save frozen bitmap. Bitmap must be editable.");
            }
            
            await _projectIOService.SaveAsync(filePath, bitmap);
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
            if (!ShouldRender(force))
            {
                return;
            }

            Logger.LogRender("START", _pixelGrid.Width, _pixelGrid.Height, force);

            try
            {
                int width = Math.Max(1, _pixelGrid.Width);
                int height = Math.Max(1, _pixelGrid.Height);

                // Render main bitmap
                bool bitmapCreated = await RenderMainBitmap(width, height, force);

                // Render preview bitmap if needed
                bool needsPreview = await RenderPreviewBitmap(width, height);

                // Update UI
                await UpdateUI(width, height, bitmapCreated, needsPreview);
                
                Logger.LogRender("COMPLETE", width, height, force);
            }
            catch (Exception ex)
            {
                Logger.Log($"ERROR: {ex.Message}");
                WpfMessageBox.Show(
                    $"Error rendering canvas: {ex.Message}",
                    "Rendering Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ShouldRender(bool force)
        {
            if (!force)
            {
                var now = DateTime.Now;
                var elapsed = (now - _lastRenderTime).TotalMilliseconds;
                if (elapsed < MinRenderIntervalMs)
                {
                    Logger.LogRender("SKIPPED (throttled)", _pixelGrid!.Width * _pixelGrid.PixelSize, _pixelGrid.Height * _pixelGrid.PixelSize, force);
                    return false;
                }
                _lastRenderTime = now;
            }
            else
            {
                _lastRenderTime = DateTime.Now;
            }
            return true;
        }

        private async Task<bool> RenderMainBitmap(int width, int height, bool force)
        {
            var cachedBitmap = _bitmapManager.CachedBitmap;
            bool bitmapCreated = false;

            if (cachedBitmap == null || 
                cachedBitmap.PixelWidth != width || 
                cachedBitmap.PixelHeight != height)
            {
                Logger.LogMemory("Creating new main bitmap", width * height * 4);
                var newBitmap = await _bitmapManager.GetOrCreateMainBitmap(width, height, BackImage);
                bitmapCreated = true;
                // Initial render - full update for new bitmap
                _pixelGrid!.MarkAllDirty();
                await _renderService.UpdateBitmapFullAsync(newBitmap, _pixelGrid);
                Logger.LogRender("Main bitmap created and rendered (full)", width, height, force);
            }
            else
            {
                // Update existing bitmap (reuse to prevent memory leak) - only dirty region
                var (rendered, dirtyWidth, dirtyHeight) = await _renderService.UpdateBitmapAsync(cachedBitmap, _pixelGrid!);
                if (rendered)
                {
                    Logger.LogRender("Main bitmap updated (dirty region)", width, height, force, dirtyWidth, dirtyHeight);
                }
                else
                {
                    Logger.LogRender("Main bitmap updated (no dirty region)", width, height, force);
                }
            }

            return bitmapCreated;
        }

        private async Task<bool> RenderPreviewBitmap(int width, int height)
        {
            bool needsPreview = _currentTool?.UsesPreview == true && (_mouseHandler?.IsDrawing ?? false);
            
            if (needsPreview)
            {
                var previewBitmap = await _bitmapManager.GetOrCreatePreviewBitmap(width, height, FrontImage);
                
                // Clear preview bitmap buffer
                _bitmapManager.ClearPreviewBuffer(width, height);
                
                // Render preview (1:1 mapping - no pixelSize parameter needed)
                if (_currentTool != null)
                {
                    _currentTool.RenderPreview(previewBitmap, 1);
                }
                Logger.LogRender("Preview rendered", width, height, false);
            }
            else
            {
                // Clear preview if not needed
                await _bitmapManager.ClearPreviewBitmap(FrontImage);
            }

            return needsPreview;
        }

        private async Task UpdateUI(int width, int height, bool bitmapCreated, bool needsPreview)
        {
            await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
            {
                var cachedBitmap = _bitmapManager.CachedBitmap;
                if (cachedBitmap != null)
                {
                    // Only set Source if it's a new bitmap or hasn't been set yet
                    if (!_bitmapManager.BitmapSourceSet || bitmapCreated)
                    {
                        BackImage.Source = cachedBitmap;
                        _bitmapManager.MarkBitmapSourceSet();
                    }
                    // Scale image by PixelSize for visual display (bitmap is 1:1, UI scales it)
                    BackImage.Width = cachedBitmap.PixelWidth * _pixelGrid!.PixelSize;
                    BackImage.Height = cachedBitmap.PixelHeight * _pixelGrid.PixelSize;
                }
                
                // Update preview image - scale by PixelSize
                if (needsPreview)
                {
                    var previewBitmap = _bitmapManager.PreviewBitmap;
                    if (previewBitmap != null)
                    {
                        if (!_bitmapManager.PreviewBitmapSourceSet)
                        {
                            FrontImage.Source = previewBitmap;
                            _bitmapManager.MarkPreviewBitmapSourceSet();
                        }
                        FrontImage.Width = previewBitmap.PixelWidth * _pixelGrid!.PixelSize;
                        FrontImage.Height = previewBitmap.PixelHeight * _pixelGrid.PixelSize;
                    }
                }
            });
        }
    }
}