using System;
using System.Net.Http;
using System.Threading;

namespace WallpaperSync.Infrastructure.Http
{
    /// <summary>
    /// Garante um único HttpClient configurado para todo o app.
    /// </summary>
    public static class HttpClientProvider
    {
        private static readonly Lazy<HttpClient> _client = new(() =>
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "WallpaperSync/2.0 (Windows NT 10.0; Win64; x64)");
            return http;
        });

        public static HttpClient Shared => _client.Value;
    }
}

