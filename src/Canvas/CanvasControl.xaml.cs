using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSPaint.ViewModels;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace MSPaint.Canvas
{
    /// <summary>
    /// Canvas control - displays the pixel grid and handles mouse events
    /// </summary>
    public partial class CanvasControl : WpfUserControl
    {
        private CanvasViewModel? _viewModel;

        public CanvasControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(CanvasViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            
            // Update scale transform and control size when properties change
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CanvasViewModel.PixelSize) ||
                        e.PropertyName == nameof(CanvasViewModel.PixelGrid) ||
                        e.PropertyName == nameof(CanvasViewModel.Bitmap))
                    {
                        UpdateScaleTransform();
                        UpdateControlSize();
                    }
                };
                UpdateScaleTransform();
                UpdateControlSize();
            }
        }

        private void UpdateScaleTransform()
        {
            if (_viewModel != null && ScaleTransform != null)
            {
                ScaleTransform.ScaleX = _viewModel.PixelSize;
                ScaleTransform.ScaleY = _viewModel.PixelSize;
            }
        }

        private void UpdateControlSize()
        {
            if (_viewModel?.PixelGrid != null)
            {
                // Set the UserControl size to match the scaled canvas size
                // This allows the ScrollViewer to know when to show scrollbars
                this.Width = _viewModel.PixelGrid.Width * _viewModel.PixelSize;
                this.Height = _viewModel.PixelGrid.Height * _viewModel.PixelSize;
            }
            else
            {
                this.Width = 0;
                this.Height = 0;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (_viewModel == null) return;

            // Get position relative to the Image
            // The Image has Stretch="None" so it's 1:1 with the bitmap
            // The ScaleTransform scales it visually, but GetPosition gives us coordinates
            // in the Image's coordinate space (which is 1:1 with the grid)
            var position = e.GetPosition(CanvasImage);
            // Clamp to grid bounds
            int gridX = Math.Max(0, Math.Min((int)position.X, _viewModel.PixelGrid?.Width - 1 ?? 0));
            int gridY = Math.Max(0, Math.Min((int)position.Y, _viewModel.PixelGrid?.Height - 1 ?? 0));
            CaptureMouse();
            _viewModel.SetMouseCaptured(true);
            _viewModel.OnMouseDown(gridX, gridY);
        }

        protected override void OnMouseMove(WpfMouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_viewModel == null || !IsMouseCaptured) return;

            var position = e.GetPosition(CanvasImage);
            // Clamp to grid bounds
            int gridX = Math.Max(0, Math.Min((int)position.X, _viewModel.PixelGrid?.Width - 1 ?? 0));
            int gridY = Math.Max(0, Math.Min((int)position.Y, _viewModel.PixelGrid?.Height - 1 ?? 0));
            _viewModel.OnMouseMove(gridX, gridY);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_viewModel == null) return;

            var position = e.GetPosition(CanvasImage);
            // Clamp to grid bounds
            int gridX = Math.Max(0, Math.Min((int)position.X, _viewModel.PixelGrid?.Width - 1 ?? 0));
            int gridY = Math.Max(0, Math.Min((int)position.Y, _viewModel.PixelGrid?.Height - 1 ?? 0));
            _viewModel.OnMouseUp(gridX, gridY);
            _viewModel.SetMouseCaptured(false);
            ReleaseMouseCapture();
        }
    }
}
