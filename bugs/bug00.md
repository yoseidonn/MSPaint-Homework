# Bug #001: Memory Leak and Performance Issues

**Date:** 2024-12-19

## Problem
Application consumes 5-10GB RAM (expected ~1.5MB) and becomes laggy when drawing on 640x640+ canvases. MouseDown causes 250MB jump. Rectangle/Ellipse tools cause GB-level memory jumps.

## Probable Cause
- `RenderService.RenderAsync()` creates new `WriteableBitmap` on every call
- `RenderAsync()` called on every `MouseMove` event (hundreds per second)
- Each bitmap ~1.5MB, old bitmaps not immediately freed (GC delay)
- Rectangle/Ellipse tools redraw large preview areas on every mouse move

## What Had Done to Solve
1. **Bitmap Reuse**: Added `UpdateBitmapAsync()` to `RenderService` - updates existing bitmap instead of creating new one
2. **Caching**: Cached `WriteableBitmap` in `DoubleBufferedCanvasControl`, reuse when size matches
3. **Throttling**: Added 60 FPS limit (16ms between renders) to prevent excessive render calls
4. **Force Render**: Immediate render on MouseDown/MouseUp, throttled on MouseMove

## How Did It Effect
- Memory usage did not stabilize for 640x640 canvas
- FPS issue still persists
- Still GB level memory jumps

**Files Modified:**
- `src/Services/RenderService.cs`
- `src/Controls/DoubleBufferedCanvasControl.cs`
