using System;
using System.IO;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;

namespace WallpaperSync.Infrastructure.Environment
{
    /// <summary>
    /// Centraliza o conhecimento de onde os diretórios da aplicação vivem no disco.
    /// </summary>
    public sealed class AppEnvironment
    {
        private static AppEnvironment _instance;
        public static AppEnvironment Instance =>
            _instance ??= CreateDefault();

        private AppEnvironment(string appDataRoot)
        {
            AppDataRoot = appDataRoot;
            CacheRoot = Path.Combine(appDataRoot, "cache");
            BackupRoot = Path.Combine(appDataRoot, "backup");
            TranscodedWallpaper = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Themes\TranscodedWallpaper");
        }

        public string AppDataRoot { get; }
        public string CacheRoot { get; }
        public string BackupRoot { get; }
        public string TranscodedWallpaper { get; }
        public static ThumbnailService? ThumbnailService { get; set; }

        public static AppEnvironment CreateDefault()
        {
            var baseDir = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "WallpaperSyncGUI");

            return new AppEnvironment(baseDir);
        }

        public void Ensure()
        {
            Directory.CreateDirectory(AppDataRoot);
            Directory.CreateDirectory(CacheRoot);
            Directory.CreateDirectory(BackupRoot);
        }

        public void CleanupCache()
        {
            TryDeleteDirectory(CacheRoot);
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;

            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"AppEnvironment.Cleanup '{path}' falhou: {ex.Message}");
            }
        }
    }
}

