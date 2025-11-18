using System;
using System.IO;

namespace WallpaperSync
{
    public class BackupService
    {
        private readonly string backupDir;

        public BackupService(string backupDir)
        {
            this.backupDir = backupDir ?? throw new ArgumentNullException(nameof(backupDir));

            try
            {
                Directory.CreateDirectory(this.backupDir);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"BackupService: erro ao criar diretório de backup: {ex.Message}");
            }
        }

        public string CreateBackupIfExists(string existingFilePath)
        {
            if (string.IsNullOrWhiteSpace(existingFilePath) || !File.Exists(existingFilePath))
                return null;

            string fileName = $"TranscodedWallpaper_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.bak";
            string backupTarget = Path.Combine(backupDir, fileName);

            try
            {
                File.Copy(existingFilePath, backupTarget, false);
                DebugLogger.Log($"BackupService: backup criado em '{backupTarget}'");
                return backupTarget;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"BackupService: falha ao criar backup: {ex.Message}");
                return null;
            }
        }

        public class BackupInfo
        {
            public string Path { get; set; }
            public string FileName => System.IO.Path.GetFileName(Path);
            public DateTime Created { get; set; }
            public long SizeBytes { get; set; }
            public string ThumbnailCachePath { get; set; }
        }
    }
}
