using System;
using System.IO;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class BackupService
    {
        private readonly string _directory;

        public BackupService(string backupDirectory)
        {
            _directory = backupDirectory ?? throw new ArgumentNullException(nameof(backupDirectory));
            Directory.CreateDirectory(_directory);
        }

        public string? CreateBackupIfExists(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return null;

            var file = $"TranscodedWallpaper_{DateTime.Now:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}.bak";
            var destination = Path.Combine(_directory, file);
            try
            {
                File.Copy(sourcePath, destination, overwrite: false);
                return destination;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"BackupService.CreateBackup falhou: {ex.Message}");
                return null;
            }
        }

        public sealed class BackupInfo
        {
            public required string Path { get; init; }
            public string FileName => System.IO.Path.GetFileName(Path);
            public DateTime Created { get; init; }
            public long SizeBytes { get; init; }
        }
    }
}
