using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.SystemIntegration
{
    public sealed class WallpaperApplier
    {
        private readonly string _transcodedPath;

        public WallpaperApplier(string transcodedPath)
        {
            _transcodedPath = transcodedPath ?? throw new ArgumentNullException(nameof(transcodedPath));
        }

        public bool ApplyViaApi(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                return WallpaperManager.SetWallpaper(path);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"WallpaperApplier.ApplyViaApi falhou: {ex.Message}");
                return false;
            }
        }

        public bool ApplyViaTranscodedWallpaper(string sourceFile)
        {
            try
            {
                File.Copy(sourceFile, _transcodedPath, overwrite: true);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"ApplyViaTranscodedWallpaper copy falhou: {ex.Message}");
                return false;
            }

            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", _transcodedPath, RegistryValueKind.String);
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "TranscodedImageCache", new byte[] { 0 }, RegistryValueKind.Binary);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"ApplyViaTranscodedWallpaper registry falhou: {ex.Message}");
                return false;
            }

            Thread.Sleep(40);
            return true;
        }
    }
}
