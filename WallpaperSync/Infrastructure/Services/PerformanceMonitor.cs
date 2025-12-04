using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WallpaperSync.Infrastructure.Services
{
    public class PerformanceSample
    {
        public DateTime Timestamp { get; init; } = DateTime.Now;
        public double RamMb { get; init; }
    }

    public class PerformanceMonitor : IDisposable
    {
        private readonly TimeSpan _interval;
        private readonly CancellationTokenSource _cts = new();
        private Task _loop;

        public event Action<PerformanceSample> SampleAvailable;

        public PerformanceMonitor(TimeSpan? interval = null)
        {
            _interval = interval ?? TimeSpan.FromSeconds(1.5);
        }

        public void Start()
        {
            if (_loop != null) return;
            _loop = Task.Run(async () => await LoopAsync(_cts.Token).ConfigureAwait(false));
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var proc = Process.GetCurrentProcess();
                    double ram = proc.WorkingSet64 / (1024d * 1024d);
                    SampleAvailable?.Invoke(new PerformanceSample { RamMb = ram });
                    await Task.Delay(_interval, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Stop()
        {
            _cts.Cancel();
            _loop = null;
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
            SampleAvailable = null;
        }
    }
}