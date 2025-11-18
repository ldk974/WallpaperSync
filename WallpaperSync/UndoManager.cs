using System;
using System.IO;
using System.Linq;
using static WallpaperSync.BackupService;

namespace WallpaperSync
{
    public class UndoManager
    {
        private readonly string backupDir;

        public UndoManager(string backupDir)
        {
            this.backupDir = backupDir ?? throw new ArgumentNullException(nameof(backupDir));
            Directory.CreateDirectory(this.backupDir);
        }
        public string? GetLastBackup()
        {
            try
            {
                var files = Directory
                    .EnumerateFiles(backupDir, "*.bak", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                return files.Count > 0 ? files[0].FullName : null;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"UndoManager: erro ao listar backups: {ex.Message}");
                return null;
            }
        }

        public IEnumerable<BackupInfo> GetBackups()
        {
            var files = Directory.Exists(backupDir)
                ? Directory.GetFiles(backupDir, "*.bak")
                : Array.Empty<string>();

            var list = files.Select(path =>
            {
                var info = new FileInfo(path);
                var created = TryParseTimestampFromName(info.Name, out var dt) ? dt : info.LastWriteTime;
                return new BackupInfo
                {
                    Path = path,
                    Created = created,
                    SizeBytes = info.Length
                };
            })
            .OrderByDescending(b => b.Created)
            .ToList();

            return list;
        }

        private bool TryParseTimestampFromName(string name, out DateTime dt)
        {
            dt = DateTime.MinValue;
            try
            {
                // espera TranscodedWallpaper_original_yyyyMMddHHmmss.bak
                var p = name.Split('_').Last(); // yyyyMMddHHmmss.bak
                p = p.Replace(".bak", "");
                if (p.Length >= 14)
                {
                    var y = int.Parse(p.Substring(0, 4));
                    var M = int.Parse(p.Substring(4, 2));
                    var d = int.Parse(p.Substring(6, 2));
                    var hh = int.Parse(p.Substring(8, 2));
                    var mm = int.Parse(p.Substring(10, 2));
                    var ss = int.Parse(p.Substring(12, 2));
                    dt = new DateTime(y, M, d, hh, mm, ss);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public bool Restore(string backupPath, string transcodedPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
                return false;

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

        public bool Delete(string backupPath, out string error)
        {
            error = null;
            try
            {
                File.Delete(backupPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public int DeleteAll(out string error)
        {
            error = null;
            int deleted = 0;
            try
            {
                var backups = GetBackups().ToArray();
                foreach (var b in backups)
                {
                    File.Delete(b.Path);
                    deleted++;
                }
                return deleted;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return deleted;
            }
        }
    }
}