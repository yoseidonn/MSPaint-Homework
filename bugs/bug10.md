# Bug #10: Thread Access Violation in Image Save Operations

**Date:** 2024-12-19

## Problem
When saving images (PNG/JPEG), application throws exception: "Başka bir iş parçacığına ait olduğundan çağıran iş parçacığı bu nesneye erişemiyor" (Cannot access object from a different thread).

## Probable Cause
1. `WriteableBitmap` is created on UI thread and is thread-affine
2. `PngFormatStrategy` and `JpegFormatStrategy` were using `Task.Run()` to perform file I/O on background thread
3. Inside `Task.Run()`, encoder tried to access `WriteableBitmap` directly, which violates WPF's thread affinity rules
4. WPF requires all UI thread-created objects (like `WriteableBitmap`) to be accessed only from the UI thread

## What Had Done to Solve
1. **Frozen Bitmap Copy**: Created a frozen copy of `WriteableBitmap` on UI thread using `FormatConvertedBitmap` and `Freeze()`
2. **Thread-Safe Access**: Frozen bitmaps can be safely accessed from any thread
3. **UI Thread Processing**: All `WriteableBitmap` access (conversion, alpha flattening) moved to UI thread via `Dispatcher.InvokeAsync()`
4. **Background File I/O**: Only file I/O operations (encoder.Save()) run on background thread using frozen bitmap copy
5. **Null Safety**: Added null checks after `InvokeAsync` to ensure bitmap was successfully created

## How Did It Effect
- Save operations now work correctly without thread exceptions
- File I/O still runs on background thread (non-blocking UI)
- Memory-efficient: frozen bitmap copy is thread-safe and reusable
- Both PNG and JPEG formats work correctly

**Files Modified:**
- `src/Services/ImageFormats/PngFormatStrategy.cs`
- `src/Services/ImageFormats/JpegFormatStrategy.cs`
