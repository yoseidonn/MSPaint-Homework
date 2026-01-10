# Bug #20: Bitmap Frozen After Save and Save Behavior Issues

**Date:** 2024-12-19

## Problem
1. After saving a file, application throws error: "Belirtilen 'System.Windows.Media.Imaging.WriteableBitmap' türünde değerin değiştirilebilmesi için IsFrozen yanlış olarak ayarlanmış olmalıdır" (IsFrozen must be false to modify WriteableBitmap)
2. After opening or saving a file, Ctrl+S or Save menu should save to the same file without opening Save As dialog

## Probable Cause
1. **Frozen Bitmap Issue**: `FormatConvertedBitmap` with `Freeze()` is being created from the original `WriteableBitmap`, but the original bitmap might be getting frozen or a frozen copy is being used somewhere
2. **Save Behavior**: `_currentFilePath` is not being set properly after initial save, or Save logic doesn't check for existing file path correctly

## What Had Done to Solve
1. **Bitmap Freezing Fix**: Ensure we never freeze the original `_cachedBitmap` - only create frozen copies for encoding
   - Changed approach: Instead of using `FormatConvertedBitmap` directly on potentially locked bitmap, copy pixel data first
   - **PNG Strategy**: Lock bitmap, copy pixel data to byte array, unlock, create new WriteableBitmap from copied data, then convert to frozen BitmapSource
   - **JPEG Strategy**: Same approach for `FlattenAlphaChannel` - copy source pixel data first, process in memory, then create new bitmap
   - This avoids "IsFrozen" errors when bitmap is locked during render operations (mouse movement)
   - Original bitmap remains unfrozen and editable throughout the process
2. **Save Logic Fix**: Update `_currentFilePath` after Save As, and ensure Save (Ctrl+S) uses existing path if available
   - `FileSave_Click` now properly checks `_currentFilePath` and saves directly without dialog
   - `FileSaveAs_Click` sets `_currentFilePath` after successful save
3. **File Path Tracking**: Ensure `_currentFilePath` is set correctly after both Save and Open operations
   - `FileOpen_Click` sets `_currentFilePath` after loading file
   - `FileNew_Click` resets `_currentFilePath` to null for new canvas

## How Did It Effect
- Canvas remains editable after saving files (no frozen bitmap errors)
- Save operations work correctly even when bitmap is locked during render (mouse movement)
- Save (Ctrl+S) saves to current file without dialog
- Save As still works for choosing new location
- Pixel data copying approach ensures thread-safety and avoids locked bitmap conflicts

**Files Modified:**
- `src/Services/ImageFormats/PngFormatStrategy.cs`
- `src/Services/ImageFormats/JpegFormatStrategy.cs`
- `src/Controls/DoubleBufferedCanvasControl.cs`
- `src/MainWindow.xaml.cs`
