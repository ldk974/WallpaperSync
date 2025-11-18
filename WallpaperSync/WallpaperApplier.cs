using System;
using System.IO;
using Microsoft.Win32;
using System.Threading;

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
            DebugLogger.Log("ApplyViaTranscodedWallpaper iniciado.");

            // copia pro TranscodedWallpaper
            try
            {
                File.Copy(sourceFile, transcodedPath, overwrite: true);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Falha ao copiar para TranscodedWallpaper: {ex.Message}");
                return false;
            }

            // atualiza registro
            try
            {
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "Wallpaper",
                    transcodedPath,
                    RegistryValueKind.String);

                Registry.SetValue(
                    @"HKEY_CURRENT_USER\Control Panel\Desktop",
                    "TranscodedImageCache",
                    new byte[] { 0 },
                    RegistryValueKind.Binary);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Aviso: não foi possível atualizar registro: {ex.Message}");
            }

            Thread.Sleep(40);

            // aplica via API
            try
            {
                bool ok = WallpaperManager.SetWallpaper(transcodedPath);
                DebugLogger.Log($"WallpaperManager.SetWallpaper (via transcoded) retornou: {(ok ? "SUCESSO" : "FALHA")}");
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
            if (!File.Exists(file))
            {
                DebugLogger.Log($"ApplyViaApi: arquivo não existe: {file}");
                return false;
            }

            try
            {
                bool ok = WallpaperManager.SetWallpaper(file);
                DebugLogger.Log($"WallpaperManager.SetWallpaper retornou: {(ok ? "SUCESSO" : "FALHA")}");
                return ok;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"WallpaperManager.SetWallpaper lançou exceção: {ex.Message}");
                return false;
            }
        }
    }
}
