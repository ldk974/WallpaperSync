using OpenTK.Graphics.ES10;
using System.Diagnostics;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.UI.Dialogs;

namespace WallpaperSync.Infrastructure.Logging
{
    public static class CoreLogger
    {
        public static ILogService LogService { get; set; }

        public static void Log(string message, LogLevel level = LogLevel.Info, int? errorCode = null)
        {
            string prefix = errorCode.HasValue ? $"[{errorCode}] " : "";
            LogService?.Append(message, level);

            Debug.WriteLine($"[{level}] {message}");
        }
    }
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}

