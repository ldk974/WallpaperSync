using System;
using System.IO;

namespace WallpaperSync
{
    public class UndoManager
    {
        private readonly string backupDir;
        public UndoManager(string backupDirectory)
        {
            backupDir = backupDirectory ?? throw new ArgumentNullException(nameof(backupDirectory));
            Directory.CreateDirectory(backupDir);
        }

        public string CreateBackupIfExists(string existingFilePath)
        {
            if (string.IsNullOrWhiteSpace(existingFilePath) || !File.Exists(existingFilePath)) return null;
            string backupTarget = Path.Combine(backupDir, "TranscodedWallpaper_original_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak");
            File.Copy(existingFilePath, backupTarget, false);
            DebugLogger.Log($"UndoManager: backup criado {backupTarget}");
            return backupTarget;
        }

        public string GetLastBackup()
        {
            var files = Directory.GetFiles(backupDir, "TranscodedWallpaper_original_*.bak").OrderByDescending(f => f).ToArray();
            return files.Length > 0 ? files[0] : null;
        }

        public bool Restore(string backupPath, string transcodedPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath)) return false;
            try
            {
                File.Copy(backupPath, transcodedPath, true);
                DebugLogger.Log($"UndoManager: restaurado {backupPath} -> {transcodedPath}");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"UndoManager: falha restore: {ex.Message}");
                return false;
            }
        }
    }
}
