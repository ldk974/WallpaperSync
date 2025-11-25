using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class UndoManager
    {
        private readonly string _backupDir;

        public UndoManager(string backupDir)
        {
            _backupDir = backupDir ?? throw new ArgumentNullException(nameof(backupDir));
            Directory.CreateDirectory(_backupDir);
        }

        public IEnumerable<BackupService.BackupInfo> GetBackups()
        {
            if (!Directory.Exists(_backupDir))
                return Enumerable.Empty<BackupService.BackupInfo>();

            return Directory.GetFiles(_backupDir, "*.bak")
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new BackupService.BackupInfo
                    {
                        Path = path,
                        Created = info.LastWriteTime,
                        SizeBytes = info.Length
                    };
                })
                .OrderByDescending(b => b.Created)
                .ToList();
        }

        public bool Restore(string backupPath, string destination)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                return false;

            try
            {
                File.Copy(backupPath, destination, overwrite: true);
                CoreLogger.Log($"UndoManager: backup restaurado de {backupPath} para {destination}");
                return true;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"UndoManager.Restore falhou: {ex.Message}");
                return false;
            }
        }

        public bool Delete(string path, out string? error)
        {
            error = null;
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                CoreLogger.Log($"UndoManager: backup deletado com sucesso: {path}");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                CoreLogger.Log($"UndoManager.Delete falhou: {ex}");
                return false;
            }
        }

        public int DeleteAll(out string? error)
        {
            error = null;
            int deleted = 0;
            try
            {
                foreach (var backup in GetBackups())
                {
                    File.Delete(backup.Path);
                    deleted++;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return deleted;
        }
    }
}
