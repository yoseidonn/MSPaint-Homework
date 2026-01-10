# Bug #00 - Patch #02: Preview Layer Implementation

**Date:** 2024-12-19

## Problem
Memory leak and performance issues persist after initial fix. Application still consumes GBs of RAM when drawing. Rectangle/Ellipse tools cause massive memory jumps because they modify PixelGrid on every mouse move, triggering full canvas renders.

## Probable Cause
1. Rectangle/Ellipse tools modify PixelGrid on every MouseMove (clearing previous preview with white, then drawing new preview)
2. Each preview change triggers full canvas render (entire PixelGrid → WriteableBitmap copy)
3. `BackImage.Source` set on every render - WPF may cache old bitmap references
4. Double buffering approach: PixelGrid + WriteableBitmap = 2x memory, but every render copies entire grid

## What Had Done to Solve
1. **Preview Layer**: Implemented FrontImage as preview layer for Rectangle/Ellipse tools
2. **ITool Interface Extension**: Added `UsesPreview` property and `RenderPreview()` method
3. **Tool Refactoring**: Rectangle/Ellipse tools now render to preview bitmap instead of modifying PixelGrid during preview
4. **Logger**: Added console logger to track every render operation
5. **Source Assignment Optimization**: Only set `BackImage.Source` when bitmap is newly created
6. **Preview Bitmap Management**: Preview bitmap only created when tool uses preview, cleared when not needed

## How Did It Effect
- TBD (needs testing) - Expected: No more PixelGrid modifications during preview, only final commit on MouseUp
- didnt work at all once again

**Files Modified:**
- `src/Utils/Logger.cs` (new)
- `src/Tools/ITool.cs`
- `src/Tools/BaseTool.cs`
- `src/Tools/RectangleTool.cs`
- `src/Tools/EllipseTool.cs`
- `src/Controls/DoubleBufferedCanvasControl.cs`
