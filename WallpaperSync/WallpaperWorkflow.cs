using System;
using System.IO;
using System.Threading.Tasks;

namespace WallpaperSync
{
    public class WallpaperWorkflow
    {
        private readonly ImageTransformer transformer;
        private readonly BackupService backupService;
        private readonly WallpaperApplier applier;
        private readonly string transcodedPath;

        public WallpaperWorkflow(ImageTransformer transformer, BackupService backupService, WallpaperApplier applier, string transcodedPath)
        {
            this.transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            this.applier = applier ?? throw new ArgumentNullException(nameof(applier));
            this.transcodedPath = transcodedPath ?? throw new ArgumentNullException(nameof(transcodedPath));
        }
        public async Task<bool> ApplyAsync(string inputPath)
        {
            try
            {
                DebugLogger.Log("WallpaperWorkflow.ApplyAsync iniciando.");
                DebugLogger.Log($"Imagem de entrada: {inputPath}");

                // carrega imagem
                using var original = transformer.LoadBitmapUnlocked(inputPath);
                DebugLogger.Log($"Imagem carregada: {original.Width}x{original.Height}");

                // garante 16:9
                using var img16 = transformer.EnsureImageIs16by9(original);
                DebugLogger.Log($"Após EnsureImageIs16by9: {img16.Width}x{img16.Height}");

                // resize se necessário
                using var finalBmp = transformer.ResizeIfNeeded(img16);
                DebugLogger.Log($"Após ResizeIfNeeded: {finalBmp.Width}x{finalBmp.Height}");

                // salva JPEG temporário
                string tempJpeg = transformer.SaveBitmapToTempJpeg(finalBmp);
                DebugLogger.Log($"Imagem final salva temporariamente em: {tempJpeg}");

                // backup do transcoded original
                string backup = backupService.CreateBackupIfExists(transcodedPath);
                if (!string.IsNullOrEmpty(backup))
                {
                    DebugLogger.Log($"Backup criado: {backup}");
                }
                else
                {
                    DebugLogger.Log("Nenhum transcodedPath existente para backup.");
                }

                // copia temporário -> transcodedPath
                File.Copy(tempJpeg, transcodedPath, true);
                DebugLogger.Log($"Arquivo copiado para transcodedPath: {transcodedPath}");

                // tenta aplicar via API
                DebugLogger.Log("Tentando aplicar via API...");
                bool apiOk = applier.ApplyViaApi(transcodedPath);
                DebugLogger.Log($"Resultado da API: {(apiOk ? "Sucesso" : "FALHA")}");

                if (!apiOk)
                {
                    DebugLogger.Log("API falhou — tentando fallback via TranscodedWallpaper.");
                    bool fallbackOk = applier.ApplyViaTranscodedWallpaper(transcodedPath);
                    DebugLogger.Log($"Resultado do fallback TranscodedWallpaper: {(fallbackOk ? "Sucesso" : "FALHA")}");
                    FileExtensions.DeleteIfExists(tempJpeg);
                    return fallbackOk;
                }

                FileExtensions.DeleteIfExists(tempJpeg);
                DebugLogger.Log("Aplicação concluída com SUCESSO via API.");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"ERRO no workflow: {ex}");
                return false;
            }
        }
    }
    internal static class FileExtensions
    {
        public static void DeleteIfExists(this string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}