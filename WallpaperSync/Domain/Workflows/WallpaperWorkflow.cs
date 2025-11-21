using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.Infrastructure.SystemIntegration;

namespace WallpaperSync.Domain.Workflows
{
    public sealed class WallpaperWorkflow
    {
        private readonly WallpaperTransformer _transformer;
        private readonly BackupService _backup;
        private readonly WallpaperApplier _applier;
        private readonly string _transcodedPath;

        public WallpaperWorkflow(
            WallpaperTransformer transformer,
            BackupService backup,
            WallpaperApplier applier,
            string transcodedPath)
        {
            _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            _backup = backup ?? throw new ArgumentNullException(nameof(backup));
            _applier = applier ?? throw new ArgumentNullException(nameof(applier));
            _transcodedPath = transcodedPath ?? throw new ArgumentNullException(nameof(transcodedPath));
        }

        public async Task<bool> ApplyAsync(string path, CancellationToken token = default)
        {
            string? prepared = null;
            try
            {
                prepared = await Task.Run(() => PrepareImage(path), token).ConfigureAwait(false);
                await BackupExistingAsync();

                var copyOk = await CopyWithRetryAsync(prepared, _transcodedPath, token).ConfigureAwait(false);
                if (!copyOk) return false;

                if (_applier.ApplyViaApi(_transcodedPath))
                    return true;

                CoreLogger.Log("WallpaperWorkflow fallback para TranscodedWallpaper.");
                return _applier.ApplyViaTranscodedWallpaper(prepared);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"WallpaperWorkflow.ApplyAsync falhou: {ex}");
                return false;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(prepared))
                {
                    TryDelete(prepared);
                }
            }
        }

        private string PrepareImage(string path)
        {
            using var original = _transformer.LoadUnlocked(path);
            using var cropped = _transformer.EnsureAspect(original);
            using var resized = _transformer.ResizeIfNeeded(cropped);
            return _transformer.SaveTemporaryJpeg(resized);
        }

        private Task BackupExistingAsync()
        {
            return Task.Run(() => _backup.CreateBackupIfExists(_transcodedPath));
        }

        private static async Task<bool> CopyWithRetryAsync(string source, string destination, CancellationToken token)
        {
            const int retries = 5;
            const int delayMs = 40;

            for (int attempt = 0; attempt < retries; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    File.Copy(source, destination, overwrite: true);
                    return true;
                }
                catch (IOException ex)
                {
                    CoreLogger.Log($"Copy retry {attempt + 1}/{retries}: {ex.Message}");
                    await Task.Delay(delayMs, token).ConfigureAwait(false);
                }
            }

            return false;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"WallpaperWorkflow não conseguiu remover temp '{path}': {ex.Message}");
            }
        }
    }
}
