using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WallpaperSync.Domain.Models;
using WallpaperSync.Infrastructure.Logging;
using static System.Net.WebRequestMethods;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class CatalogService
    {
        private readonly HttpClient _http;
        private readonly string[] _folders = new[] { "sfw", "nsfw" };

        public CatalogService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            CoreLogger.Log("CatalogService inicializado.", LogLevel.Debug);
        }

        public async Task<IReadOnlyList<WallpaperItem>> LoadAsync(CancellationToken cancellationToken = default)
        {
            CoreLogger.Log("CatalogService.LoadAsync iniciado", LogLevel.Info);

            var catalog = new List<WallpaperItem>();
            var baseUrl = "https://ldk-ws.xyz/";

            foreach (var folder in _folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                CoreLogger.Log($"Carregando categoria '{folder}'.", LogLevel.Info);

                var wallUrl = $"{baseUrl}wallpapers/{folder}/";
                var thumbUrl = $"{baseUrl}thumbs/{folder}/";

                CoreLogger.Log($"Solicitando lista de wallpapers: {wallUrl}", LogLevel.Debug);
                var wallHtml = await TryGetString(wallUrl, cancellationToken) ?? string.Empty;

                CoreLogger.Log($"Solicitando lista de thumbnails: {thumbUrl}", LogLevel.Debug);
                var thumbHtml = await TryGetString(thumbUrl, cancellationToken) ?? string.Empty;

                var wallpaperFiles = ParseLinks(wallHtml);
                var thumbSet = ParseLinks(thumbHtml).ToHashSet(StringComparer.OrdinalIgnoreCase);

                CoreLogger.Log(
    $"Categoria '{folder}': encontrados {wallpaperFiles.Count()} wallpapers e {thumbSet.Count} thumbs.",
    LogLevel.Debug);

                foreach (var fileServerName in wallpaperFiles)
                {
                    var originalUrl = $"{wallUrl}{fileServerName}";
                    var thumbName = MatchThumb(fileServerName, thumbSet);
                    catalog.Add(new WallpaperItem
                    {
                        Id = Hash(originalUrl),
                        Name = Uri.UnescapeDataString(fileServerName),
                        OriginalUrl = originalUrl,
                        ThumbnailUrl = thumbName != null ? $"{thumbUrl}{thumbName}" : null,
                        Category = folder
                    });
                }
            }

            CoreLogger.Log($"CatalogService carregou {catalog.Count} itens no total.", LogLevel.Info);
            return catalog;
        }

        public IReadOnlyList<string> BuildCategories(IEnumerable<WallpaperItem> items)
        {
            CoreLogger.Log("CatalogService.BuildCategories iniciado.", LogLevel.Debug);

            return items
                .Select(i => i.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .Prepend("All")
                .ToList();
        }

        private async Task<string?> TryGetString(string url, CancellationToken token)
        {
            try
            {
                return await _http.GetStringAsync(url, token);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"Erro ao acessar '{url}': {ex.Message}", LogLevel.Warning);
                return null;
            }
        }

        private static IEnumerable<string> ParseLinks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                CoreLogger.Log("ParseLinks recebeu HTML vazio.", LogLevel.Warning);
                return Enumerable.Empty<string>();
            }

            var regex = new Regex("<a\\s+href\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return regex
                .Matches(html)
                .Select(m => m.Groups[1].Value)
                .Where(f =>
                    f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
        }

        private static string? MatchThumb(string fileServerName, HashSet<string> thumbs)
        {
            if (thumbs.Contains(fileServerName))
                return fileServerName;

            var name = System.IO.Path.GetFileNameWithoutExtension(fileServerName);
            var ext = System.IO.Path.GetExtension(fileServerName);
            var alt = $"{name}_thumb{ext}";
            return thumbs.Contains(alt) ? alt : null;
        }

        private static string Hash(string value)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(value.ToLowerInvariant());
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash)[..20];
        }
    }
}

