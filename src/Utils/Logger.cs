using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MSPaint.Utils
{
    public static class Logger
    {
        private static bool _consoleAllocated = false;
        private static readonly object _lock = new object();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        static Logger()
        {
            // Allocate console for WPF application
            lock (_lock)
            {
                if (!_consoleAllocated)
                {
                    _consoleAllocated = AllocConsole();
                    if (_consoleAllocated)
                    {
                        Console.OutputEncoding = Encoding.UTF8;
                        Console.WriteLine("=== MSPaint Logger Initialized ===");
                        Console.WriteLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                        Console.WriteLine();
                    }
                }
            }
        }

        public static void Log(string message)
        {
            lock (_lock)
            {
                if (_consoleAllocated)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                }
            }
        }

        public static void LogRender(string action, int width, int height, bool force = false, int? dirtyWidth = null, int? dirtyHeight = null)
        {
            if (dirtyWidth.HasValue && dirtyHeight.HasValue)
            {
                double ratio = (dirtyWidth.Value * dirtyHeight.Value) / (double)(width * height) * 100.0;
                Log($"RENDER: {action} | Size: {width}x{height} | Dirty: {dirtyWidth}x{dirtyHeight} ({ratio:F1}%) | Force: {force}");
            }
            else
            {
                Log($"RENDER: {action} | Size: {width}x{height} | Force: {force}");
            }
        }

        public static void LogMemory(string action, long? bytes = null)
        {
            if (bytes.HasValue)
            {
                double mb = bytes.Value / (1024.0 * 1024.0);
                Log($"MEMORY: {action} | {mb:F2} MB");
            }
            else
            {
                Log($"MEMORY: {action}");
            }
        }
    }
}
