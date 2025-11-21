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

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class CatalogService
    {
        private readonly HttpClient _http;
        private readonly string[] _folders = new[] { "sfw", "nsfw" };

        public CatalogService(HttpClient httpClient)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IReadOnlyList<WallpaperItem>> LoadAsync(string currentUrlsTxt, CancellationToken cancellationToken = default)
        {
            CoreLogger.Log("CatalogService.LoadAsync iniciado");

            var catalog = new List<WallpaperItem>();
            var raw = await TryGetString(currentUrlsTxt, cancellationToken);
            if (string.IsNullOrWhiteSpace(raw))
                return catalog;

            var baseUrl = raw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();

            if (string.IsNullOrWhiteSpace(baseUrl))
                return catalog;

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                baseUrl += "/";

            foreach (var folder in _folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var wallUrl = $"{baseUrl}wallpapers/{folder}/";
                var thumbUrl = $"{baseUrl}thumbs/{folder}/";

                var wallHtml = await TryGetString(wallUrl, cancellationToken) ?? string.Empty;
                var thumbHtml = await TryGetString(thumbUrl, cancellationToken) ?? string.Empty;

                var wallpaperFiles = ParseLinks(wallHtml);
                var thumbSet = ParseLinks(thumbHtml).ToHashSet(StringComparer.OrdinalIgnoreCase);

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

            CoreLogger.Log($"CatalogService carregou {catalog.Count} itens.");
            return catalog;
        }

        public IReadOnlyList<string> BuildCategories(IEnumerable<WallpaperItem> items)
        {
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
                CoreLogger.Log($"CatalogService falhou '{url}': {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<string> ParseLinks(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return Enumerable.Empty<string>();

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

