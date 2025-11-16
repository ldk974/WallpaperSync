using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WallpaperSync
{
    public class ThumbnailService : IDisposable
    {
        private readonly string cacheDir;
        private readonly ImageDownloader downloader;
        private readonly SemaphoreSlim limiter;
        private readonly ConcurrentDictionary<string, Image> memCache = new();

        public ThumbnailService(ImageDownloader downloader, string baseCacheDir, int concurrency = 6)
        {
            this.downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
            cacheDir = Path.Combine(baseCacheDir, "thumbs");
            Directory.CreateDirectory(cacheDir);
            limiter = new SemaphoreSlim(concurrency);
        }

        public async Task<string> GetOrCreateThumbPathAsync(ImageEntry entry, int w = 150, int h = 84)
        {
            string thumbPath = Path.Combine(cacheDir, $"{entry.FileId}_thumb.jpg");

            // se já existe no disco → retorna
            if (File.Exists(thumbPath))
                return thumbPath;

            await limiter.WaitAsync();
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

                        var bytes = await downloader.DownloadRawBytesAsync(entry.ThumbnailUrl);
                        if (bytes != null)
                        {
                            await File.WriteAllBytesAsync(thumbPath, bytes);
                            DebugLogger.Log($"ThumbnailService: salvo thumbnail remota {thumbPath}");
                            return thumbPath;
                        }

                        DebugLogger.Log("Thumbnail remota falhou, gerando local...");
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"Thumbnail remota erro: {ex.Message}, gerando local...");
                    }
                }

                // se thumbnail não existe = baixa original e gera thumb local
                string origPath = await downloader.DownloadOriginalAsync(entry);

                using var img = LoadImageUnlocked(origPath);
                using var bmp = new Bitmap(w, h);
                using var g = Graphics.FromImage(bmp);

                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                float srcRatio = (float)img.Width / img.Height;
                float dstRatio = (float)w / h;

                Rectangle srcRect;

                if (srcRatio > dstRatio)
                {
                    int newWidth = (int)(img.Height * dstRatio);
                    int x = (img.Width - newWidth) / 2;
                    srcRect = new Rectangle(x, 0, newWidth, img.Height);
                }
                else
                {
                    int newHeight = (int)(img.Width / dstRatio);
                    int y = (img.Height - newHeight) / 2;
                    srcRect = new Rectangle(0, y, img.Width, newHeight);
                }

                g.DrawImage(img, new Rectangle(0, 0, w, h), srcRect, GraphicsUnit.Pixel);
                bmp.Save(thumbPath, System.Drawing.Imaging.ImageFormat.Jpeg);

                DebugLogger.Log($"ThumbnailService: salvo gerada localmente {thumbPath}");
                return thumbPath;
            }
            finally
            {
                limiter.Release();
            }
        }

        public Image LoadThumbIntoMemory(string thumbPath)
        {
            // volta cópia pra evitar locks
            var bytes = File.ReadAllBytes(thumbPath);
            using var ms = new MemoryStream(bytes);
            return Image.FromStream(ms);
        }

        private static Image LoadImageUnlocked(string path)
        {
            var bytes = File.ReadAllBytes(path);
            using var ms = new MemoryStream(bytes);
            return Image.FromStream(ms);
        }

        public void Dispose()
        {
            foreach (var kv in memCache)
                kv.Value.Dispose();
            memCache.Clear();
            limiter.Dispose();
        }
    }
}
