using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using WallpaperSync.UI.Components;
using WallpaperSync.Infrastructure.Services;

namespace WallpaperSync.UI.Controls
{
    public class PerformancePanelControl : UserControl, IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly CartesianChart _chart;
        private readonly LineSeries<double> _ramSeries;
        private readonly Label _lblDownload; // placeholder for external metric
        private readonly Label _lblWallpaperTime;

        public PerformancePanelControl(PerformanceMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            Dock = DockStyle.Fill;

            _lblDownload = new Label { Text = "Tempo médio de download: N/A", AutoSize = true, Left = 10, Top = 10, Font = new Font("Segoe UI", 10) };
            _lblWallpaperTime = new Label { Text = "Tempo para aplicar wallpaper: N/A", AutoSize = true, Left = 10, Top = 36, Font = new Font("Segoe UI", 10) };

            _ramSeries = new LineSeries<double>
            {
                Values = new ObservableCollection<double>(),
                GeometrySize = 0,
                LineSmoothness = 0.4,
                Stroke = new SolidColorPaint(new SKColor(0, 180, 255)) { StrokeThickness = 3 },
                Fill = null
            };

            _chart = new CartesianChart
            {
                Series = new ISeries[] { _ramSeries },
                XAxes = new Axis[] { new Axis { Name = "Tempo (s)", TextSize = 12 } },
                YAxes = new Axis[] { new Axis { Name = "RAM usada (MB)", TextSize = 12, Labeler = value => value.ToString("F1") } },
                Left = 10,
                Top = 80,
                Width = 600,
                Height = 300,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            Controls.Add(_chart);
            Controls.Add(_lblDownload);
            Controls.Add(_lblWallpaperTime);

            _monitor.SampleAvailable += OnSample;
            _monitor.Start();

            ApplyTheme();
        }

        private void OnSample(PerformanceSample s)
        {
            if (IsHandleCreated && InvokeRequired)
            {
                BeginInvoke(new Action(() => OnSample(s)));
                return;
            }

            if (_ramSeries.Values is ObservableCollection<double> list)
            {
                list.Add(s.RamMb);
                if (list.Count > 60) list.RemoveAt(0);
                _chart.Update();
            }
        }

        public void ApplyTheme()
        {
            bool dark = ThemeManager.IsDarkMode();
            var axisColor = dark ? new SKColor(220, 220, 220) : new SKColor(40, 40, 40);
            foreach (var a in _chart.XAxes) a.LabelsPaint = new SolidColorPaint(axisColor);
            foreach (var a in _chart.YAxes) a.LabelsPaint = new SolidColorPaint(axisColor);
            _ramSeries.Stroke = new SolidColorPaint(dark ? new SKColor(0, 200, 255) : new SKColor(0, 120, 220)) { StrokeThickness = 3 };
            _chart.Update();
        }

        public void Dispose()
        {
            _monitor.SampleAvailable -= OnSample;
            _monitor?.Dispose();
        }
    }
}