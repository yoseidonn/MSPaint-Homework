using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MSPaint.Models;

namespace MSPaint.Controls.Helpers
{
    /// <summary>
    /// Manages bitmap creation, caching, and lifecycle for canvas rendering
    /// </summary>
    public class BitmapManager
    {
        private WriteableBitmap? _cachedBitmap;
        private WriteableBitmap? _previewBitmap;
        private bool _bitmapSourceSet = false;
        private bool _previewBitmapSourceSet = false;

        public WriteableBitmap? CachedBitmap => _cachedBitmap;
        public WriteableBitmap? PreviewBitmap => _previewBitmap;
        public bool BitmapSourceSet => _bitmapSourceSet;
        public bool PreviewBitmapSourceSet => _previewBitmapSourceSet;

        /// <summary>
        /// Create or get main bitmap with specified dimensions
        /// </summary>
        public async Task<WriteableBitmap> GetOrCreateMainBitmap(int width, int height, System.Windows.Controls.Image? backImage = null)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (_cachedBitmap == null || 
                _cachedBitmap.PixelWidth != width || 
                _cachedBitmap.PixelHeight != height)
            {
                // Clear old bitmap reference before creating new one
                if (backImage != null)
                {
                    await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (backImage.Source != null)
                        {
                            backImage.Source = null; // Release old bitmap reference
                        }
                    });
                }
                
                // Create new bitmap on UI thread (must be on UI thread)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _cachedBitmap = new WriteableBitmap(
                        width, height, 96, 96, 
                        PixelFormats.Pbgra32, null);
                });
                _bitmapSourceSet = false;
            }

            return _cachedBitmap!;
        }

        /// <summary>
        /// Create or get preview bitmap with specified dimensions
        /// </summary>
        public async Task<WriteableBitmap> GetOrCreatePreviewBitmap(int width, int height, System.Windows.Controls.Image? frontImage = null)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (_previewBitmap == null || 
                _previewBitmap.PixelWidth != width || 
                _previewBitmap.PixelHeight != height)
            {
                if (frontImage != null)
                {
                    await WpfApplication.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (frontImage.Source != null)
                        {
                            frontImage.Source = null;
                        }
                    });
                }
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _previewBitmap = new WriteableBitmap(
                        width, height, 96, 96,
                        PixelFormats.Pbgra32, null);
                });
                _previewBitmapSourceSet = false;
            }

            return _previewBitmap!;
        }

        /// <summary>
        /// Clear preview bitmap
        /// </summary>
        public async Task ClearPreviewBitmap(System.Windows.Controls.Image? frontImage = null)
        {
            if (_previewBitmap != null && frontImage != null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    frontImage.Source = null;
                });
                _previewBitmap = null;
                _previewBitmapSourceSet = false;
            }
        }

        /// <summary>
        /// Clear all bitmaps (used when reinitializing canvas)
        /// </summary>
        public void ClearAll()
        {
            _cachedBitmap = null;
            _previewBitmap = null;
            _bitmapSourceSet = false;
            _previewBitmapSourceSet = false;
        }

        /// <summary>
        /// Mark that main bitmap source has been set
        /// </summary>
        public void MarkBitmapSourceSet()
        {
            _bitmapSourceSet = true;
        }

        /// <summary>
        /// Mark that preview bitmap source has been set
        /// </summary>
        public void MarkPreviewBitmapSourceSet()
        {
            _previewBitmapSourceSet = true;
        }

        /// <summary>
        /// Clear preview bitmap buffer (fill with transparent)
        /// </summary>
        public void ClearPreviewBuffer(int width, int height)
        {
            if (_previewBitmap == null) return;

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
                _previewBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                _previewBitmap.Unlock();
            }
        }
    }
}
