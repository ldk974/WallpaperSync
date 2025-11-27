using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WallpaperSync.Domain.Models;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.UI.Dialogs;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class ThumbnailService : IDisposable
    {
        public static ThumbnailService Instance { get; private set; }

        private readonly ImageCacheService _cache;
        private readonly string _thumbRoot;
        private readonly SemaphoreSlim _limiter;
        private readonly SemaphoreSlim _downloadLimiter;
        private readonly SemaphoreSlim _ioLimiter;
        private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _inflight =
            new(StringComparer.OrdinalIgnoreCase);

        private long _totalDownloadTimeMs = 0;
        private int _downloadCount = 0;
        private int _updateEvery = 6;

        private System.Timers.Timer _avgTimer;

        // evento que é disparado a cada download concluído
        public event Action<double>? DownloadAverageUpdated;

        public ThumbnailService(ImageCacheService cache, string cacheRoot, int concurrency = 4)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _thumbRoot = Path.Combine(cacheRoot ?? throw new ArgumentNullException(nameof(cacheRoot)), "thumbs");
            Directory.CreateDirectory(_thumbRoot);

            int cpu = System.Environment.ProcessorCount;

            int limit = Math.Max(4, cpu * 3);
            int downloadLimit = Math.Max(2, (int)(cpu * 1.5));
            int ioLimit = Math.Clamp(cpu / 2, 1, 4);

            _limiter = new SemaphoreSlim(limit);
            _downloadLimiter = new SemaphoreSlim(downloadLimit);
            _ioLimiter = new SemaphoreSlim(ioLimit);

        }

        public Task<string> GetOrCreateAsync(WallpaperItem item, int width = 150, int height = 84, CancellationToken token = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var key = $"{item.Id}_{width}x{height}";
            var destination = Path.Combine(_thumbRoot, $"{key}.jpg");

            if (File.Exists(destination))
                return Task.FromResult(destination);

            var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<string>>(() => CreateThumbnailAsync(key, item, destination, width, height, CancellationToken.None), LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
        }

        private async Task<string> CreateThumbnailAsync(string cacheKey, WallpaperItem item, string destination, int width, int height, CancellationToken token)
        {
            var acquired = false;
            try
            {
                // timer de espera pelo limiter
                var swWait = Stopwatch.StartNew();
                await _downloadLimiter.WaitAsync(token).ConfigureAwait(false);
                swWait.Stop();
                acquired = true;

                //CoreLogger.Log($"WAIT TIME: {swWait.ElapsedMilliseconds} ms");

                // timer total de download + save
                var swTotal = Stopwatch.StartNew();
                
                // se já existe no disco -> retorna
                if (File.Exists(destination))
                    return destination;
                
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                
                // tenta thumbnail remota
                if (!string.IsNullOrEmpty(item.ThumbnailUrl))
                {
                    var swDownload = Stopwatch.StartNew();
                    try
                    {
                        var bytes = await _cache.DownloadBytesAsync(item.ThumbnailUrl, token).ConfigureAwait(false);
                        _downloadLimiter.Release();

                        if (bytes != null && bytes.Length > 0)
                        {
                            await _ioLimiter.WaitAsync(token).ConfigureAwait(false);
                            await File.WriteAllBytesAsync(destination, bytes, token).ConfigureAwait(false);
                            _ioLimiter.Release();
                            swDownload.Stop();
                            RegisterDownloadTime(swDownload.ElapsedMilliseconds);

                            CoreLogger.Log($"ThumbnailService: salvo thumbnail remota {destination} " + $"(download: {swDownload.ElapsedMilliseconds} ms, total: {swTotal.ElapsedMilliseconds} ms)", LogLevel.Debug);

                            return destination;
                        }
                    } 
                    catch (Exception ex)
                    { 
                        CoreLogger.Log($"ThumbnailService: falha ao baixar thumbnail remota {item.ThumbnailUrl}: {ex.Message}");
                    }
                    
                    
                }
                
                // se thumbail não existe = baixa original e gera thumb local
                await _downloadLimiter.WaitAsync(token);
                var original = await _cache.EnsureOriginalAsync(item, token).ConfigureAwait(false);
                _downloadLimiter.Release();
                
                await _limiter.WaitAsync(token).ConfigureAwait(false);

                using var source = await LoadBitmapAsync(original).ConfigureAwait(false);
                using var cropped = Crop(source, width, height);

                var temp = Path.Combine(_thumbRoot, $"{Guid.NewGuid():N}.tmp");
                SaveJpeg(cropped, temp, 85L);
                
                await _ioLimiter.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    File.Move(temp, destination, overwrite: true);
                }
                finally
                { 
                    _ioLimiter.Release();
                }
                
                CoreLogger.Log($"ThumbnailService: gerada localmente {destination} (total: {swTotal.ElapsedMilliseconds} ms)");
                return destination;
            }
            finally
            {
                if (acquired)
                    _limiter.Release();
            }
        }

        private void RegisterDownloadTime(long elapsedMs)
        {

            Interlocked.Add(ref _totalDownloadTimeMs, elapsedMs);
            var count = Interlocked.Increment(ref _downloadCount);

            if (count % _updateEvery != 0)
                return;

            double media = (double)_totalDownloadTimeMs / _downloadCount;
            // dispara evento
            DownloadAverageUpdated?.Invoke(media);
        }

        private static async Task<Bitmap> LoadBitmapAsync(string path)
        {
            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            await fs.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            using var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
            return new Bitmap(img);
        }

        private static Bitmap Crop(Image source, int targetWidth, int targetHeight)
        {
            Rectangle srcRect = CalculateCropRect(source.Width, source.Height, targetWidth, targetHeight);
            var bmp = new Bitmap(targetWidth, targetHeight);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.Low;
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight), srcRect, GraphicsUnit.Pixel);
            return bmp;
        }

        private static Rectangle CalculateCropRect(int srcW, int srcH, int dstW, int dstH)
        {
            float srcRatio = (float)srcW / srcH;
            float dstRatio = (float)dstW / dstH;

            if (srcRatio > dstRatio)
            {
                int newWidth = (int)Math.Round(srcH * dstRatio);
                int x = (srcW - newWidth) / 2;
                return new Rectangle(x, 0, newWidth, srcH);
            }
            else
            {
                int newHeight = (int)Math.Round(srcW / dstRatio);
                int y = (srcH - newHeight) / 2;
                return new Rectangle(0, y, srcW, newHeight);
            }
        }

        private static void SaveJpeg(Image bmp, string path, long quality)
        {
            var codec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
                ?? throw new InvalidOperationException("Codec JPEG não encontrado.");

            using var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            bmp.Save(path, codec, ep);
        }

        public void Dispose()
        {
            _limiter.Dispose();
            _inflight.Clear();
        }
    }
}

