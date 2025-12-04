using System;
using System.IO;
using System.Runtime.InteropServices;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.SystemIntegration
{
    public static class WallpaperManager
    {
        private const int SPI_SETDESKWALLPAPER = 0x14;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(
            int uiAction,
            int uiParam,
            string pvParam,
            int fWinIni);

        public static bool SetWallpaper(string file)
        {
            try
            {
                file = Path.GetFullPath(file);
                bool result = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    file,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (!result)
                {
                    var error = Marshal.GetLastWin32Error();
                    CoreLogger.Log($"WallpaperManager: SystemParametersInfo falhou: {error}");
                }

                return result;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"WallpaperManager: erro: {ex.Message}");
                return false;
            }
        }
    }
}

