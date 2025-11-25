using System.Diagnostics;
using WallpaperSync.UI.Dialogs;

namespace WallpaperSync.Infrastructure.Logging
{
    public static class CoreLogger
    {
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
#if DEBUG
            try
            {
                DebugLogForm.Instance?.AppendLine(message, level);
            }
            catch
            {
                // ignored
            }
            Debug.WriteLine($"[{level}] {message}");
#else
            _ = message;
#endif
        }

        public static void Log(Exception ex, string context)
            => Log($"{context}: {ex}");
    }
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}

