using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WallpaperSync
{
    public class CatalogLoader
    {
        private readonly HttpClient http;
        public CatalogLoader(HttpClient httpClient) => http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        public async Task<List<ImageEntry>> LoadCatalogAsync(string currentUrlsTxt)
        {
            DebugLogger.Log("CatalogLoader: iniciando LoadCatalogAsync");
            var result = new List<ImageEntry>();

            string rawContent;
            try
            {
                rawContent = await http.GetStringAsync(currentUrlsTxt);
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"CatalogLoader: Falha ao baixar {currentUrlsTxt}: {ex.Message}");
                return result;
            }

            string baseUrl = rawContent
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();

            if (string.IsNullOrEmpty(baseUrl))
            {
                DebugLogger.Log("CatalogLoader: baseUrl vazia");
                return result;
            }
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            DebugLogger.Log($"CatalogLoader: baseUrl detectada: {baseUrl}");

            string[] categories = new[] { "sfw", "nsfw" };

            foreach (var folder in categories)
            {
                var urlWall = $"{baseUrl}wallpapers/{folder}/";
                var urlThumb = $"{baseUrl}thumbs/{folder}/";

                string htmlWall = "";
                string htmlThumb = "";

                try { htmlWall = await http.GetStringAsync(urlWall); }
                catch (Exception ex) { DebugLogger.Log($"CatalogLoader: Erro lendo {urlWall}: {ex.Message}"); continue; }

                try { htmlThumb = await http.GetStringAsync(urlThumb); }
                catch (Exception ex) { DebugLogger.Log($"CatalogLoader: Erro lendo {urlThumb}: {ex.Message}"); }

                var wallpaperFiles = ParseHtmlLinks(htmlWall);
                var thumbFiles = new HashSet<string>(ParseHtmlLinks(htmlThumb), StringComparer.OrdinalIgnoreCase);

                foreach (var fileServerName in wallpaperFiles)
                {
                    var decoded = Uri.UnescapeDataString(fileServerName);
                    var originalUrl = $"{urlWall}{fileServerName}";
                    var thumbMatch = MatchThumb(fileServerName, thumbFiles);
                    var thumbUrl = thumbMatch != null ? $"{urlThumb}{thumbMatch}" : null;
                    var fileId = HashSha256(originalUrl.ToLowerInvariant());

                    result.Add(new ImageEntry
                    {
                        FileServerName = fileServerName,
                        FileDecodedName = decoded,
                        Name = decoded,
                        OriginalUrl = originalUrl,
                        ThumbnailUrl = thumbUrl,
                        Category = folder,
                        FileId = fileId
                    });
                }
            }

            DebugLogger.Log($"CatalogLoader: finalizado. Itens={result.Count}");
            return result;
        }

        private static string HashSha256(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 20);
        }

        private static List<string> ParseHtmlLinks(string html)
        {
            if (string.IsNullOrEmpty(html)) return new List<string>();

            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(html, "<a\\s+href\\s*=\\s*\"([^\"]+)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return matches
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(h =>
                        h.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        h.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        h.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        h.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"CatalogLoader.ParseHtmlLinks falhou: {ex.Message}");
                return new List<string>();
            }
        }

        private static string MatchThumb(string fileServerName, HashSet<string> thumbFiles)
        {
            if (thumbFiles.Contains(fileServerName)) return fileServerName;
            var name = System.IO.Path.GetFileNameWithoutExtension(fileServerName);
            var ext = System.IO.Path.GetExtension(fileServerName);
            var alt = $"{name}_thumb{ext}";
            return thumbFiles.Contains(alt) ? alt : null;
        }
    }
}
