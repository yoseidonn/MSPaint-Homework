using System;
using System.Windows;
using System.Windows.Input;
using MSPaint.Models;
using MSPaint.Tools;
using WpfPoint = System.Windows.Point;

namespace MSPaint.Controls.Helpers
{
    /// <summary>
    /// Handles mouse events and coordinate conversion for canvas
    /// </summary>
    public class MouseEventHandler
    {
        private readonly PixelGrid? _pixelGrid;
        private WpfPoint _lastMousePosition;
        private bool _isDrawing;

        public bool IsDrawing
        {
            get => _isDrawing;
            set => _isDrawing = value;
        }

        public WpfPoint LastMousePosition
        {
            get => _lastMousePosition;
            set => _lastMousePosition = value;
        }

        public MouseEventHandler(PixelGrid? pixelGrid)
        {
            _pixelGrid = pixelGrid;
        }

        /// <summary>
        /// Convert screen coordinates to grid coordinates
        /// </summary>
        public WpfPoint? ScreenToGrid(WpfPoint screenPoint)
        {
            if (_pixelGrid == null) return null;

            // Convert screen coordinates to grid coordinates
            // screenPoint is already relative to BackImage, so we can directly divide by PixelSize
            int gridX = (int)(screenPoint.X / _pixelGrid.PixelSize);
            int gridY = (int)(screenPoint.Y / _pixelGrid.PixelSize);

            // Clamp to grid bounds
            if (gridX < 0 || gridX >= _pixelGrid.Width || gridY < 0 || gridY >= _pixelGrid.Height)
                return null;

            return new WpfPoint(gridX, gridY);
        }

        /// <summary>
        /// Handle mouse down event
        /// </summary>
        public void HandleMouseDown(WpfPoint screenPoint, ITool? currentTool, Action<BaseTool>? startCollectingChanges = null)
        {
            if (_pixelGrid == null || currentTool == null) return;

            var gridPosition = ScreenToGrid(screenPoint);
            if (gridPosition.HasValue)
            {
                _lastMousePosition = gridPosition.Value;
                
                // Start collecting pixel changes for command pattern
                if (currentTool is BaseTool baseTool)
                {
                    startCollectingChanges?.Invoke(baseTool);
                }
                
                currentTool.OnMouseDown((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
            }
        }

        /// <summary>
        /// Handle mouse move event
        /// </summary>
        public bool HandleMouseMove(WpfPoint screenPoint, ITool? currentTool)
        {
            if (!_isDrawing || _pixelGrid == null || currentTool == null) return false;

            var gridPosition = ScreenToGrid(screenPoint);
            if (gridPosition.HasValue)
            {
                var currentPos = gridPosition.Value;
                
                // Only process if position changed (avoid duplicate calls)
                if (currentPos != _lastMousePosition)
                {
                    currentTool.OnMouseMove((int)currentPos.X, (int)currentPos.Y);
                    _lastMousePosition = currentPos;
                    return true; // Indicates rendering should occur
                }
            }
            return false;
        }

        /// <summary>
        /// Handle mouse up event
        /// </summary>
        public void HandleMouseUp(WpfPoint screenPoint, ITool? currentTool, Action<BaseTool, System.Collections.Generic.List<(int x, int y, System.Windows.Media.Color oldColor, System.Windows.Media.Color newColor)>?>? stopCollectingChanges = null)
        {
            if (!_isDrawing || _pixelGrid == null || currentTool == null) return;

            var gridPosition = ScreenToGrid(screenPoint);
            if (gridPosition.HasValue)
            {
                currentTool.OnMouseUp((int)gridPosition.Value.X, (int)gridPosition.Value.Y);
                
                // Stop collecting changes and return pixel changes
                if (currentTool is BaseTool baseTool)
                {
                    var pixelChanges = baseTool.StopCollectingChanges();
                    stopCollectingChanges?.Invoke(baseTool, pixelChanges);
                }
            }
        }
    }
}
