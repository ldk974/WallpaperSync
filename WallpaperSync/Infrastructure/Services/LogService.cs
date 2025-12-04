using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WallpaperSync.Infrastructure.Logging;
using static WallpaperSync.Infrastructure.Environment.AppEnvironment;

namespace WallpaperSync.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly List<LogEntry> _entries = new();
        private readonly object _sync = new();
        private bool _disposed;

        public event Action<LogEntry> LogAppended;

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_sync) { return _entries.AsReadOnly(); }
            }
        }

        public void Append(string message, LogLevel level = LogLevel.Info)
        {
            if (_disposed) return;
            var entry = new LogEntry(DateTime.Now, level, message);
            lock (_sync)
            {
                _entries.Add(entry);
            }
            LogAppended?.Invoke(entry);
        }

        public async Task ExportToFileAsync(string path)
        {
            StringBuilder sb = new();
            lock (_sync)
            {
                foreach (var e in _entries)
                {
                    sb.AppendLine($"[{e.Time:yyyy-MM-dd HH:mm:ss}] [{e.Level}] {e.Message}");
                }
            }

            using var sw = new StreamWriter(path, false, Encoding.UTF8);
            await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
        }

        public void Clear()
        {
            lock (_sync) { _entries.Clear(); }
        }

        public void Dispose()
        {
            _disposed = true;
            LogAppended = null;
        }
    }
}