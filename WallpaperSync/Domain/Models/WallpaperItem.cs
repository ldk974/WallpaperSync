using System;
using System.IO;

namespace WallpaperSync.Domain.Models
{
    public sealed class WallpaperItem
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string OriginalUrl { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
        public string Category { get; init; } = "unknown";

        public string FileName
        {
            get
            {
                try
                {
                    var uri = new Uri(OriginalUrl);
                    var decoded = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
                    return string.IsNullOrWhiteSpace(decoded) ? Name : decoded;
                }
                catch
                {
                    return Name;
                }
            }
        }

        public override string ToString() => Name;
    }
}

