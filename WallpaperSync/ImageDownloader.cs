using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WallpaperSync
{
    public class ImageDownloader
    {
        private readonly HttpClient http;
        private readonly string cacheDir;

        // apenas um download por FileId acontece de uma vez
        private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks = new();

        public ImageDownloader(HttpClient httpClient, string cacheDirectory)
        {
            http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            cacheDir = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
        }

        private SemaphoreSlim GetLock(string fileId)
            => fileLocks.GetOrAdd(fileId, _ => new SemaphoreSlim(1, 1));

    public static string ExtractCategoryFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "unknown";

        try
        {
            var uri = new Uri(url);
            var segs = uri.Segments
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            if (segs.Length >= 2)
                return segs[segs.Length - 2];

            if (segs.Length == 1)
                return "root";

            return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    public static Task<List<string>> GetCategoriesFromEntriesAsync(List<ImageEntry> entries)
    {
        if (entries == null) return Task.FromResult(new List<string> { "All" });

        var cats = entries
            .Select(e => ExtractCategoryFromUrl(e.OriginalUrl))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();

        // adiciona uma categoria "All" no topo
        cats.Insert(0, "All");
        return Task.FromResult(cats);
    }

    public async Task<string> DownloadOriginalAsync(ImageEntry entry, CancellationToken ct = default)
        {
            string dir = Path.Combine(cacheDir, "originals");
            Directory.CreateDirectory(dir);

            string ext = Path.GetExtension(entry.FileServerName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".jpg";

            string target = Path.Combine(dir, $"{entry.FileId}{ext}");

            // já existe -> retorna
            if (File.Exists(target))
                return target;

            var sem = GetLock(entry.FileId);
            await sem.WaitAsync(ct);
            try
            {
                if (File.Exists(target))
                    return target;

                DebugLogger.Log($"ImageDownloader: Baixando original {entry.OriginalUrl}");

                using var resp = await http.GetAsync(entry.OriginalUrl, ct);
                resp.EnsureSuccessStatusCode();

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                await File.WriteAllBytesAsync(target, bytes, ct);

                DebugLogger.Log($"ImageDownloader: salvo em {target}");
                return target;
            }
            finally
            {
                sem.Release();
            }
        }


        public async Task<string> DownloadCustomAsync(string imageUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("URL inválida.");

            using var resp = await http.GetAsync(imageUrl, ct);
            resp.EnsureSuccessStatusCode();

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync(ct);

            string ext = Path.GetExtension(imageUrl).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext) || !IsValidImageExtension(ext))
            {
                string contentType = resp.Content.Headers.ContentType?.MediaType;
                ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/bmp" => ".bmp",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }

            string workspaceDir = Path.Combine(cacheDir, "custom");
            Directory.CreateDirectory(workspaceDir);

            string fileName = $"custom_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}";
            string path = Path.Combine(workspaceDir, fileName);

            await File.WriteAllBytesAsync(path, bytes, ct);
            DebugLogger.Log($"ImageDownloader: custom salvo em {path}");

            return path;
        }


        public async Task<byte[]?> DownloadRawBytesAsync(string url, CancellationToken ct = default)
        {
            try
            {
                using var resp = await http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    DebugLogger.Log($"DownloadRawBytesAsync: falha HTTP {resp.StatusCode}");
                    return null;
                }

                return await resp.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"DownloadRawBytesAsync erro: {ex.Message}");
                return null;
            }
        }


        private static bool IsValidImageExtension(string ext)
        {
            return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp";
        }
    }
}