using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace WallpaperSync
{
    public class ImageDownloader
    {
        private readonly HttpClient http;
        private readonly string cacheDir;

        public ImageDownloader(HttpClient httpClient, string cacheDirectory)
        {
            http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            cacheDir = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
        }

        public async Task<string> DownloadOriginalAsync(ImageEntry entry)
        {
            var dir = Path.Combine(cacheDir, "originals");
            Directory.CreateDirectory(dir);

            string ext = Path.GetExtension(entry.FileServerName);
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            string target = Path.Combine(dir, $"{entry.FileId}{ext}");
            if (File.Exists(target)) return target;

            DebugLogger.Log($"ImageDownloader: Baixando original {entry.OriginalUrl}");
            var resp = await http.GetAsync(new Uri(entry.OriginalUrl));
            if (!resp.IsSuccessStatusCode) throw new Exception($"Falha ao baixar imagem original ({resp.StatusCode})");
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(target, bytes);
            DebugLogger.Log($"ImageDownloader: salvo em {target}");
            return target;
        }
        public async Task<string> DownloadThumbnailAsync(ImageEntry entry)
        {
            string thumbDir = Path.Combine(cacheDir, "thumbs");
            Directory.CreateDirectory(thumbDir);

            string thumbPath = Path.Combine(thumbDir, $"{entry.FileId}_thumb.jpg");
            string thumbUrl = entry.OriginalUrl + "?w=300";

            DebugLogger.Log($"ImageDownloader: Baixando thumbnail {thumbUrl}");

            HttpResponseMessage resp = await http.GetAsync(thumbUrl);
            if (resp.IsSuccessStatusCode)
            {
                byte[] bytes = await resp.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(thumbPath, bytes);
                DebugLogger.Log($"ImageDownloader: thumbnail salvo em {thumbPath}");
                return thumbPath;
            }
            DebugLogger.Log("Servidor não suporta ?w=300 — usando fallback parcial");

            var req = new HttpRequestMessage(HttpMethod.Get, entry.OriginalUrl);
            req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 150000); // baixa 150 KB

            resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Falha ao baixar miniatura (HTTP {resp.StatusCode})");

            byte[] partialBytes = await resp.Content.ReadAsByteArrayAsync();

            // gera thumb a partir do pedaço baixado
            using (var ms = new MemoryStream(partialBytes))
            using (var original = Image.FromStream(ms, false, false))
            using (var thumb = new Bitmap(original, new Size(300, 200)))
            {
                thumb.Save(thumbPath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            DebugLogger.Log($"ImageDownloader: thumb gerada via fallback em {thumbPath}");
            return thumbPath;
        }

        public async Task<string> DownloadCustomAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) throw new ArgumentException("URL inválida.");

            var resp = await http.GetAsync(imageUrl);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Falha ao baixar imagem (HTTP {resp.StatusCode}).");

            byte[] bytes = await resp.Content.ReadAsByteArrayAsync();

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
            string fileName = $"custom_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            string finalPath = Path.Combine(workspaceDir, fileName);

            await File.WriteAllBytesAsync(finalPath, bytes);
            DebugLogger.Log($"ImageDownloader: custom salvo em {finalPath}");
            return finalPath;
        }
        public async Task<byte[]?> DownloadRawBytesAsync(string url)
        {
            try
            {
                using var resp = await http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                    return null;
                return await resp.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidImageExtension(string ext)
        {
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".webp";
        }
    }
}