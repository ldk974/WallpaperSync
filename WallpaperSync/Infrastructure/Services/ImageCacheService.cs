using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WallpaperSync.Domain.Models;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class ImageCacheService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _cacheRoot;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
        private const int MaxHttpRetries = 3;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(75);

        public ImageCacheService(HttpClient http, string cacheRoot)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _cacheRoot = cacheRoot ?? throw new ArgumentNullException(nameof(cacheRoot));

            CoreLogger.Log("Inicializando ImageCacheService...", LogLevel.Debug);

            Directory.CreateDirectory(OriginalsDir);
            Directory.CreateDirectory(CustomDir);

            CoreLogger.Log("Diretórios de cache criados/verificados.", LogLevel.Debug);
        }

        private string OriginalsDir => Path.Combine(_cacheRoot, "originals");
        private string CustomDir => Path.Combine(_cacheRoot, "custom");

        public async Task<string> EnsureOriginalAsync(WallpaperItem item, CancellationToken token = default)
        {
            CoreLogger.Log($"EnsureOriginalAsync iniciado para ID={item.Id}", LogLevel.Info);

            var extension = Path.GetExtension(item.FileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".jpg";

            var destination = Path.Combine(OriginalsDir, $"{item.Id}{extension}");
            if (File.Exists(destination))
            {
                CoreLogger.Log($"Arquivo já está em cache: {destination}", LogLevel.Debug);
                return destination;
            }

            var gate = Rent(item.Id);
            await gate.WaitAsync(token).ConfigureAwait(false);

            try
            {
                if (File.Exists(destination))
                {
                    CoreLogger.Log($"Arquivo encontrado em cache após lock: {destination}", LogLevel.Debug);
                    return destination;
                }

                CoreLogger.Log($"Baixando imagem original: {item.OriginalUrl}", LogLevel.Info);

                var bytes = await SendWithRetryAsync(
                    async ct =>
                    {
                        using var response = await _http.GetAsync(item.OriginalUrl, ct).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    },
                    $"EnsureOriginal {item.Id}",
                    token).ConfigureAwait(false);

                CoreLogger.Log($"Salvando imagem baixada em {destination}", LogLevel.Debug);
                await File.WriteAllBytesAsync(destination, bytes, token).ConfigureAwait(false);
                return destination;
            }
            finally
            {
                gate.Release();
                Release(item.Id);
            }
        }

        public async Task<string> SaveCustomAsync(string imageUrl, CancellationToken token = default)
        {
            CoreLogger.Log($"SaveCustomAsync solicitado para URL: {imageUrl}", LogLevel.Info);

            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("URL inválida", nameof(imageUrl));

            var download = await SendWithRetryAsync(
                async ct =>
                {
                    using var resp = await _http.GetAsync(imageUrl, ct).ConfigureAwait(false);
                    resp.EnsureSuccessStatusCode();
                    var bytes = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    var mediaType = resp.Content.Headers.ContentType?.MediaType;
                    return (bytes, mediaType);
                },
                "SaveCustom",
                token).ConfigureAwait(false);
            var ext = InferExtension(imageUrl, download.mediaType);

            var file = Path.Combine(CustomDir, $"custom_{DateTime.Now:yyyyMMdd_HHmmssfff}{ext}");
            await File.WriteAllBytesAsync(file, download.bytes, token).ConfigureAwait(false);
            return file;
        }

        public async Task<byte[]?> DownloadBytesAsync(string url, CancellationToken token = default)
        {
            CoreLogger.Log($"DownloadBytesAsync iniciado para: {url}", LogLevel.Debug);

            try
            {
                using var resp = await _http.GetAsync(url, token);
                if (!resp.IsSuccessStatusCode)
                {
                    CoreLogger.Log($"Erro HTTP {resp.StatusCode} ao baixar: {url}", LogLevel.Warning);
                    throw new HttpRequestException($"Status {resp.StatusCode}");
                }

                CoreLogger.Log($"Download concluído para {url}", LogLevel.Debug);
                return await resp.Content.ReadAsByteArrayAsync(token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                CoreLogger.Log($"ImageCacheService.DownloadBytes cancelado após múltiplas tentativas: {url}", LogLevel.Warning);
                return null;
            }
            catch (OperationCanceledException)
            {
                CoreLogger.Log($"Download cancelado pelo token: {url}", LogLevel.Debug);
                throw;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"ImageCacheService.DownloadBytes falhou: {ex.Message}", LogLevel.Warning);
                return null;
            }
        }

        private static string InferExtension(string url, string? contentType)
        {
            var ext = Path.GetExtension(url)?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(ext) && IsValidImageExtension(ext))
            {
                CoreLogger.Log($"Extensão inferida da URL: {ext}", LogLevel.Debug);
                return ext;
            }

            CoreLogger.Log($"Inferindo extensão a partir do content-type: {contentType}", LogLevel.Debug);

            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".jpg"
            };
        }

        private static bool IsValidImageExtension(string ext)
            => ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp";

        private async Task<T> SendWithRetryAsync<T>(
            Func<CancellationToken, Task<T>> action,
            string context,
            CancellationToken token)
        {
            CoreLogger.Log($"{context}: iniciando operação com retry.", LogLevel.Debug);

            for (int attempt = 1; attempt <= MaxHttpRetries; attempt++)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(RequestTimeout);
                try
                {
                    return await action(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    CoreLogger.Log($"{context}: tentativa {attempt} expirou por timeout.", LogLevel.Warning);

                    if (attempt == MaxHttpRetries)
                    {
                        CoreLogger.Log($"{context}: esgotou todas as tentativas.", LogLevel.Error);
                        throw;
                    }

                    CoreLogger.Log($"{context}: tentativa {attempt} cancelada (timeout). Retentando...");
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), token).ConfigureAwait(false);
                }
            }

            throw new InvalidOperationException("SendWithRetryAsync: não foi possível concluir a operação.");
        }

        private SemaphoreSlim Rent(string key)
            => _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        private void Release(string key)
        {
            if (_locks.TryRemove(key, out var gate))
            {
                gate.Dispose();
            }
        }

        public void Dispose()
        {
            CoreLogger.Log("Liberando ImageCacheService e descartando locks...", LogLevel.Info);

            foreach (KeyValuePair<string, SemaphoreSlim> entry in _locks)
            {
                entry.Value.Dispose();
            }
            _locks.Clear();

            CoreLogger.Log("ImageCacheService finalizado.", LogLevel.Debug);
        }
    }
}

