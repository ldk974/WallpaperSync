using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WallpaperSync
{
    public class ThumbnailService : IDisposable
    {
        private readonly ImageDownloader downloader;
        private readonly string cacheDir;
        private readonly SemaphoreSlim limiter;
        // evita gerar mesma thumb varias vezes ao mesmo tempo
        private readonly ConcurrentDictionary<string, Lazy<Task<string>>> creationTasks = new();
        private bool disposed;

        public ThumbnailService(ImageDownloader downloader, string baseCacheDir, int concurrency = 6)
        {
            this.downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            if (string.IsNullOrWhiteSpace(baseCacheDir)) throw new ArgumentNullException(nameof(baseCacheDir));

            cacheDir = Path.Combine(baseCacheDir, "thumbs");
            Directory.CreateDirectory(cacheDir);

            limiter = new SemaphoreSlim(Math.Max(1, concurrency));
        }

        public async Task<string> GetOrCreateThumbPathAsync(ImageEntry entry, int w = 150, int h = 84, CancellationToken cancellationToken = default)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            string key = $"{entry.FileId}_{w}x{h}";
            string thumbPath = Path.Combine(cacheDir, $"{entry.FileId}_thumb_{w}x{h}.jpg");

            if (File.Exists(thumbPath))
                return thumbPath;

            var lazyTask = creationTasks.GetOrAdd(key, _ => new Lazy<Task<string>>(() => CreateThumbnailAsync(entry, thumbPath, w, h, cancellationToken)));

            try
            {
                var result = await lazyTask.Value.ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
            {
                // se cancelado, remove tarefa pra permitir re-tentativa futura
                creationTasks.TryRemove(key, out _);
                throw;
            }
            catch (Exception ex)
            {
                // remove a entry pra evitar cache de falha e log completo
                creationTasks.TryRemove(key, out _);
                DebugLogger.Log($"ThumbnailService: criação de {key} falhou: {ex}");
                throw;
            }
        }

        private async Task<string> CreateThumbnailAsync(ImageEntry entry, string thumbPath, int w, int h, CancellationToken cancellationToken)
        {
            // se já existe no disco -> retorna
            if (File.Exists(thumbPath))
                return thumbPath;

            await limiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (File.Exists(thumbPath))
                    return thumbPath;

                // tenta thumbnail remota
                if (!string.IsNullOrEmpty(entry.ThumbnailUrl))
                {
                    try
                    {
                        DebugLogger.Log($"ThumbnailService: Baixando thumbnail remota {entry.ThumbnailUrl}");

                        var bytes = await downloader.DownloadRawBytesAsync(entry.ThumbnailUrl, cancellationToken).ConfigureAwait(false);
                        if (bytes != null && bytes.Length > 0)
                        {
                            var temp = Path.Combine(cacheDir, $"{Guid.NewGuid()}.tmp");
                            await File.WriteAllBytesAsync(temp, bytes, cancellationToken).ConfigureAwait(false);
                            File.Move(temp, thumbPath);
                            DebugLogger.Log($"ThumbnailService: salvo thumbnail remota {thumbPath}");
                            return thumbPath;
                        }
                        DebugLogger.Log($"ThumbnailService: thumbnail remota vazia ({entry.ThumbnailUrl}), gerando local...");
                    }
                    catch (OperationCanceledException)
                    {
                        DebugLogger.Log($"ThumbnailService: download remoto cancelado ({entry.ThumbnailUrl})");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"ThumbnailService: erro ao baixar thumbnail remota ({entry.ThumbnailUrl}): {ex.Message}");
                    }
                }

                // se thumbnail não existe = baixa original e gera thumb local
                string origPath = await downloader.DownloadOriginalAsync(entry, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(origPath) || !File.Exists(origPath))
                    throw new FileNotFoundException("Arquivo original não disponível para gerar thumbnail.", origPath);

                using var srcImage = LoadImageClone(origPath);

                Rectangle srcRect = CalculateCropRect(srcImage.Width, srcImage.Height, w, h);

                // cria bitmap de destino
                using var bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Black);
                    g.DrawImage(srcImage, new Rectangle(0, 0, w, h), srcRect, GraphicsUnit.Pixel);
                }

                // salva JPEG com qualidade decente (85)
                var tempFile = Path.Combine(cacheDir, $"{Guid.NewGuid()}.tmp");
                SaveJpeg(bmp, tempFile, 85L);

                // Move atômico
                File.Move(tempFile, thumbPath);

                DebugLogger.Log($"ThumbnailService: salvo gerada localmente {thumbPath}");
                return thumbPath;
            }
            finally
            {
                limiter.Release();
            }
        }

        private static Bitmap LoadImageClone(string path)
        {
            // lê bytes para evitar trava no arquivo
            var bytes = File.ReadAllBytes(path);
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
            // cria clone independente
            return new Bitmap(img);
        }

        private static Rectangle CalculateCropRect(int srcW, int srcH, int dstW, int dstH)
        {
            float srcRatio = (float)srcW / srcH;
            float dstRatio = (float)dstW / dstH;

            if (srcRatio > dstRatio)
            {
                // imagem mais larga -> cortar nas laterais
                int newWidth = (int)Math.Round(srcH * dstRatio);
                int x = (srcW - newWidth) / 2;
                return new Rectangle(x, 0, newWidth, srcH);
            }
            else
            {
                // imagem mais alta -> cortar topo/baixo
                int newHeight = (int)Math.Round(srcW / dstRatio);
                int y = (srcH - newHeight) / 2;
                return new Rectangle(0, y, srcW, newHeight);
            }
        }

        private static void SaveJpeg(Bitmap bmp, string path, long quality)
        {
            var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
                        ?? throw new InvalidOperationException("JPEG encoder não encontrado.");

            var enc = System.Drawing.Imaging.Encoder.Quality;
            using var ep = new EncoderParameters(1) { Param = { [0] = new EncoderParameter(enc, quality) } };

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            bmp.Save(path, codec, ep);
        }

        public Image LoadThumbIntoMemory(string thumbPath)
        {
            if (string.IsNullOrEmpty(thumbPath)) throw new ArgumentNullException(nameof(thumbPath));
            if (!File.Exists(thumbPath)) throw new FileNotFoundException("thumbnail não encontrado", thumbPath);

            var bytes = File.ReadAllBytes(thumbPath);
            using var ms = new MemoryStream(bytes);
            using var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
            return new Bitmap(img);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            try
            {
                limiter.Dispose();
            }
            catch { /* swallow */ }

            creationTasks.Clear();
        }
    }
}
