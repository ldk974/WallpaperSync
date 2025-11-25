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

            CoreLogger.Log("WallpaperWorkflow inicializado.", LogLevel.Debug);
        }

        public async Task<bool> ApplyAsync(string path, CancellationToken token = default)
        {
            CoreLogger.Log($"Iniciando ApplyAsync para '{path}'.", LogLevel.Info);

            string? prepared = null;
            try
            {
                prepared = await Task.Run(() => PrepareImage(path), token).ConfigureAwait(false);
                CoreLogger.Log($"Imagem preparada em '{prepared}'.", LogLevel.Debug);

                await BackupExistingAsync();

                var copyOk = await CopyWithRetryAsync(prepared, _transcodedPath, token).ConfigureAwait(false);
                if (!copyOk)
                {
                    CoreLogger.Log("Falha ao copiar imagem para TranscodedWallpaper após múltiplas tentativas.", LogLevel.Error);
                    return false;
                }

                CoreLogger.Log("Arquivo copiado para TranscodedWallpaper com sucesso.", LogLevel.Info);

                if (_applier.ApplyViaApi(_transcodedPath))
                {
                    CoreLogger.Log("Wallpaper aplicado via API com sucesso.", LogLevel.Info);
                    return true;
                }

                CoreLogger.Log("Falha ao aplicar via API — tentando fallback para TranscodedWallpaper.", LogLevel.Warning);

                bool fallbackOk = _applier.ApplyViaTranscodedWallpaper(prepared);
                CoreLogger.Log(
                    fallbackOk
                        ? "Wallpaper aplicado via TranscodedWallpaper com sucesso."
                        : "Falha ao aplicar",
                    fallbackOk ? LogLevel.Info : LogLevel.Error);

                return fallbackOk;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"WallpaperWorkflow.ApplyAsync falhou: {ex}", LogLevel.Error);
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
            CoreLogger.Log($"Preparando imagem '{path}'.", LogLevel.Debug);

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
                    CoreLogger.Log($"Arquivo temporário removido: '{path}'.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"Falha ao remover arquivo temporário '{path}': {ex.Message}", LogLevel.Warning);
            }
        }
    }
}
