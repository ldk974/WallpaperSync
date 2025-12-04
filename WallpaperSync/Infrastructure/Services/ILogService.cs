using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WallpaperSync.Infrastructure.Logging;
using static WallpaperSync.Infrastructure.Environment.AppEnvironment;

namespace WallpaperSync.Infrastructure.Services
{
    public interface ILogService : IDisposable
    {
        event Action<LogEntry> LogAppended;
        IReadOnlyList<LogEntry> Entries { get; }
        void Append(string message, LogLevel level = LogLevel.Info);
        Task ExportToFileAsync(string path);
        void Clear();
    }
}