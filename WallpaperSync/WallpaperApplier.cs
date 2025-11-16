using System;
using System.IO;
using Microsoft.Win32;

namespace WallpaperSync
{
    public class WallpaperApplier
    {
        private readonly string transcodedPath;

        public WallpaperApplier(string transcodedPath)
        {
            this.transcodedPath = transcodedPath ?? throw new ArgumentNullException(nameof(transcodedPath));
        }
        public bool ApplyViaTranscodedWallpaper(string sourceFile)
        {
            try
            {
                File.Copy(sourceFile, transcodedPath, true);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Falha ao copiar para TranscodedWallpaper: {ex.Message}");
                return false;
            }

            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "WallPaper", transcodedPath);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Aviso: não foi possível atualizar o registry: {ex.Message}");
            }

            try
            {
                bool ok = WallpaperManager.SetWallpaper(transcodedPath);
                DebugLogger.Log($"WallpaperManager.SetWallpaper (via Transcoded) retornou: {(ok ? "Sucesso" : "FALHA")}");
                return ok;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Erro ao acionar API no método TranscodedWallpaper: {ex.Message}");
                return false;
            }
        }
        public bool ApplyViaApi(string file)
        {
            try
            {
                return WallpaperManager.SetWallpaper(file);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"WallpaperManager.SetWallpaper threw: {ex.Message}");
                return false;
            }
        }
    }
}
