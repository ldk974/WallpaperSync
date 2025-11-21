using System.Diagnostics;
using WallpaperSync.UI.Dialogs;

namespace WallpaperSync.Infrastructure.Logging
{
    public static class CoreLogger
    {
        public static void Log(string message)
        {
#if DEBUG
            try
            {
                DebugLogForm.Instance?.AppendLine(message);
            }
            catch
            {
                // ignored
            }
            Debug.WriteLine(message);
#else
            _ = message;
#endif
        }

        public static void Log(Exception ex, string context)
            => Log($"{context}: {ex}");
    }
}

