using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MSPaint.ViewModels;

namespace MSPaint.Canvas
{
    /// <summary>
    /// Canvas control - displays the pixel grid and handles mouse events
    /// </summary>
    public partial class CanvasControl : UserControl
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
            
            // Update scale transform when pixel size changes
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CanvasViewModel.PixelSize))
                    {
                        UpdateScaleTransform();
                    }
                };
                UpdateScaleTransform();
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

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (_viewModel == null) return;

            var position = e.GetPosition(CanvasImage);
            _viewModel.OnMouseDown((int)position.X, (int)position.Y);
            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_viewModel == null || !IsMouseCaptured) return;

            var position = e.GetPosition(CanvasImage);
            _viewModel.OnMouseMove((int)position.X, (int)position.Y);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_viewModel == null) return;

            var position = e.GetPosition(CanvasImage);
            _viewModel.OnMouseUp((int)position.X, (int)position.Y);
            ReleaseMouseCapture();
        }
    }
}
