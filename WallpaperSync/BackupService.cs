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
            Directory.CreateDirectory(this.backupDir);
        }

        public string CreateBackupIfExists(string existingFilePath)
        {
            if (string.IsNullOrWhiteSpace(existingFilePath) || !File.Exists(existingFilePath))
                return null;

            string backupTarget = Path.Combine(backupDir,
                "TranscodedWallpaper_original_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak");

            File.Copy(existingFilePath, backupTarget, false);
            return backupTarget;
        }
    }
}
