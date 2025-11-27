using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WallpaperSync.Domain.Models;
using WallpaperSync.Infrastructure.Environment;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.UI.Components;

namespace WallpaperSync.UI.Dialogs
{
    public partial class DebugLogForm : Form
    {
        public static DebugLogForm Instance { get; private set; }
        private readonly List<(DateTime time, LogLevel level, string message)> logEntries
    = new List<(DateTime, LogLevel, string)>();
        private ThumbnailService _thumbnailService;

        private SmoothListBox navList;
        private Panel contentPanel;
        private Panel dividerLine;
        private Panel logsPanel;
        private Panel connectionPanel;
        private Panel perfPanel;

        private Label levelLabel;
        private ComboBox logLevelCombo;
        private CheckBox verboseCheck;
        private TextBox searchBox;
        private Button exportLogsBtn;
        private Button clearLogsBtn;
        private RichTextBox txtLog;

        private CartesianChart ramChart;
        private LineSeries<double> ramLineSeries;
        private ISeries[] ramSeries;
        private Axis[] ramXAxis;
        private Axis[] ramYAxis;
        private System.Windows.Forms.Timer ramTimer;

        private Label lblDownload;
        private Label lblWallpaperTime;

        public DebugLogForm()
        {
            Text = "Debug Tools";
            ClientSize = new Size(720, 550);
            MinimumSize = new Size(720, 550);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(15,15,15);

            InitializeLayout();
            Instance = this;
        }
        private void InitializeLayout()
        {
            navList = new SmoothListBox()
            {
                Dock = DockStyle.Left,
                Width = 260,
                ItemHeight = 45,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 14)
            };

            navList.Items.AddRange(new object[] { "Logs", "Conexão", "Desempenho" });
            navList.SelectedIndex = 0;

            // linha divisória
            dividerLine = new Panel() { Dock = DockStyle.Left, Width = 1 };

            // Painel principal
            contentPanel = new Panel()
            {
                Dock = DockStyle.Fill,
            };

            logsPanel = new Panel() { Dock = DockStyle.Fill, Visible = true };
            connectionPanel = new Panel() { Dock = DockStyle.Fill, Visible = false };
            perfPanel = new Panel() { Dock = DockStyle.Fill, Visible = false };

            contentPanel.Controls.Add(logsPanel);
            contentPanel.Controls.Add(connectionPanel);
            contentPanel.Controls.Add(perfPanel);

            Controls.Add(contentPanel);
            Controls.Add(dividerLine);
            Controls.Add(navList);

            navList.SelectedIndexChanged += NavList_SelectedIndexChanged;

            LoadLogsPanel();
            LoadConnectionPanel();
            LoadPerformancePanel();
        }


        private void ApplyTheme()
        {
            bool dark = ThemeManager.IsDarkMode();

            Color back = dark ? Color.FromArgb(15, 15, 15) : Color.FromArgb(250, 250, 250);
            Color fore = dark ? Color.White : Color.Black;
            Color divider = dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 200, 200);

            BackColor = back;
            navList.BackColor = back;
            navList.ForeColor = fore;

            dividerLine.BackColor = divider;

            contentPanel.BackColor = back;
            
            levelLabel.ForeColor = fore;

            logLevelCombo.BackColor = back;
            logLevelCombo.ForeColor = fore;

            verboseCheck.ForeColor = fore;

            searchBox.BackColor = back;
            searchBox.ForeColor = fore;

            exportLogsBtn.BackColor = back;
            exportLogsBtn.ForeColor = fore;

            clearLogsBtn.BackColor = back;
            clearLogsBtn.ForeColor = fore;

            txtLog.BackColor = back;
            txtLog.ForeColor = fore;
        }
        private void ApplyChartTheme()
        {
            bool dark = ThemeManager.IsDarkMode();

            var axisColor = dark ? new SKColor(220, 220, 220) : new SKColor(40, 40, 40);
            var bgColor = dark ? Color.FromArgb(20, 20, 20) : Color.FromArgb(245, 245, 245);

            foreach (var axis in ramXAxis)
                axis.LabelsPaint = new SolidColorPaint(axisColor);

            foreach (var axis in ramYAxis)
                axis.LabelsPaint = new SolidColorPaint(axisColor);

            ramLineSeries.Stroke = new SolidColorPaint(
                dark ? new SKColor(0, 200, 255) : new SKColor(0, 120, 220)
            )
            { StrokeThickness = 3 };

            ramChart.Update();
        }

        private void NavList_SelectedIndexChanged(object sender, EventArgs e)
        {
            logsPanel.Visible = false;
            connectionPanel.Visible = false;
            perfPanel.Visible = false;

            switch (navList.SelectedItem.ToString())
            {
                case "Logs":
                    logsPanel.Visible = true;
                    break;

                case "Conexão":
                    connectionPanel.Visible = true;
                    break;

                case "Desempenho":
                    perfPanel.Visible = true;
                    break;
            }
        }

        private void LoadLogsPanel()
        {
            levelLabel = new Label()
            {
                AutoSize = true,
                Text = "Nível:",
                Left = 12,
                Top = 20,
                Font = new Font("Segoe UI", 14)
            };
            logsPanel.Controls.Add(levelLabel);

            logLevelCombo = new ComboBox()
            {
                Left = 74,
                Top = 20,
                Width = 96,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Popup
            };
            logLevelCombo.Items.AddRange(new object[] { "INFO", "WARNING", "ERROR" });
            logLevelCombo.SelectedIndex = 0;
            logLevelCombo.SelectedIndexChanged += (s, e) => RenderLogs();
            logsPanel.Controls.Add(logLevelCombo);

            verboseCheck = new CheckBox()
            {
                Left = 181,
                Top = 23,
                Text = "Verbose",
                Font = new Font("Segoe UI", 11)
            };
            verboseCheck.CheckedChanged += (s, e) => RenderLogs();
            logsPanel.Controls.Add(verboseCheck);

            searchBox = new TextBox()
            {
                Left = 16,
                Top = 60,
                Width = logsPanel.Width - 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11)
            };
            searchBox.TextChanged += (s, e) => RenderLogs();
            logsPanel.Controls.Add(searchBox);

            txtLog = new RichTextBox()
            {
                Left = 16,
                Top = 100,
                Width = logsPanel.Width - 32,
                Height = logsPanel.Height - 180,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                ReadOnly = true
            };
            logsPanel.Controls.Add(txtLog);

            var buttonsLayout = new TableLayoutPanel()
            {
                Left = 12,
                Top = logsPanel.Height - 60,
                Width = logsPanel.Width - 22,
                Height = 40,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 2
            };

            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            logsPanel.Controls.Add(buttonsLayout);

            exportLogsBtn = new Button()
            {
                Text = "Exportar Logs",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11)
            };
            buttonsLayout.Controls.Add(exportLogsBtn, 0, 0);

            clearLogsBtn = new Button()
            {
                Text = "Limpar Log",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11)
            };
            buttonsLayout.Controls.Add(clearLogsBtn, 1, 0);

            clearLogsBtn.Click += (s, e) =>
            {
                logEntries.Clear();
                RenderLogs();
            };

            exportLogsBtn.Click += (s, e) =>
            {
                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Filter = "Arquivo de texto (*.txt)|*.txt",
                    FileName = $"ws_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                using (var sw = new StreamWriter(sfd.FileName))
                {
                    foreach (var entry in logEntries)
                    {
                        sw.WriteLine($"[{entry.time:yyyy-MM-dd HH:mm:ss}] [{entry.level}] {entry.message}");
                    }
                }
            };

            ApplyTheme();
        }

        private async void LoadPerformancePanel()
        {
            lblDownload = new Label()
            {
                Text = "Tempo médio de download: 0.0ms (placeholder)",
                AutoSize = true,
                Left = 10,
                Top = 10,
                Font = new Font("Segoe UI", 12),
                ForeColor = ThemeManager.IsDarkMode() ? Color.White : Color.Black
            };
            perfPanel.Controls.Add(lblDownload);

            lblWallpaperTime = new Label()
            {
                Text = "Tempo para aplicar wallpaper: 0.0ms (placeholder)",
                AutoSize = true,
                Left = 10,
                Top = 40,
                Font = new Font("Segoe UI", 12),
                ForeColor = ThemeManager.IsDarkMode() ? Color.White : Color.Black
            };
            perfPanel.Controls.Add(lblWallpaperTime);

            // inicia objetos pesados sem travar ui
            await Task.Run(() => PrepareChartObjects());

            ramChart = new CartesianChart()
            {
                Series = ramSeries,
                XAxes = ramXAxis,
                YAxes = ramYAxis,
                Left = 10,
                Top = 110,
                Width = perfPanel.Width - 40,
                Height = perfPanel.Height - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };
            perfPanel.Controls.Add(ramChart);

            // timer
            StartRamTimer();
            ApplyChartTheme();
        }

        private void LoadConnectionPanel()
        {
            connectionPanel.Controls.Add(new Label()
            {
                Text = "Painel de conexão (em construção)",
                AutoSize = true,
                Font = new Font("Segoe UI", 14),
                Left = 20,
                Top = 20
            });
        }


        private void PrepareChartObjects()
        {
            ramLineSeries = new LineSeries<double>
            {
                Values = new ObservableCollection<double>(),
                GeometrySize = 0,
                LineSmoothness = 0.4,
                Stroke = new SolidColorPaint(new SKColor(0, 180, 255)) { StrokeThickness = 3 },
                Fill = null
            };

            SKColor gray = new SKColor(100, 100, 100);

            ramSeries = new ISeries[]
            {
        ramLineSeries
            };

            ramXAxis = new Axis[]
            {
        new Axis { Name = "Tempo (s)", TextSize = 12, NamePaint = new SolidColorPaint(gray) }
            };

            ramYAxis = new Axis[]
            {
        new Axis { Name = "RAM usada (MB)", TextSize = 12, NamePaint = new SolidColorPaint(gray), Labeler = value => value.ToString("F1") }
            };
        }

        private void StartRamTimer()
        {
            ramTimer = new System.Windows.Forms.Timer();
            ramTimer.Interval = 1500;

            ramTimer.Tick += (s, e) =>
            {
                double ramUsedMb = Process.GetCurrentProcess().WorkingSet64 / (1024d * 1024d);

                if (ramLineSeries.Values is ObservableCollection<double> list)
                {
                    list.Add(ramUsedMb);
                    if (list.Count > 60)
                        list.RemoveAt(0);
                }

                ramChart.Update();
            };

            ramTimer.Start();
        }


        public void AppendLine(string text, LogLevel level = LogLevel.Info)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, LogLevel>(AppendLine), text, level);
                return;
            }

            logEntries.Add((DateTime.Now, level, text));
            RenderLogs();
        }

        private void RenderLogs()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RenderLogs));
                return;
            }

            txtLog.SuspendLayout();
            txtLog.Clear();

            string selected = logLevelCombo.SelectedItem?.ToString() ?? "INFO";
            bool verbose = verboseCheck.Checked;
            string search = searchBox.Text?.Trim().ToLower() ?? "";

            LogLevel selectedLevel = selected switch
            {
                "INFO" => LogLevel.Info,
                "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Info
            };

            foreach (var entry in logEntries)
            {

                // DEBUG aparece apenas no verbose
                if (entry.level == LogLevel.Debug && !verbose)
                    continue;

                // filtro de nível
                if (entry.level != LogLevel.Debug && entry.level < selectedLevel)
                    continue;

                // filtro de busca
                if (!string.IsNullOrWhiteSpace(search) &&
                    !entry.message.ToLower().Contains(search))
                    continue;

                // construir linha
                string line = $"[{entry.time:HH:mm:ss}] [{entry.level}] {entry.message}\r\n";

                // aplica cor
                bool dark = ThemeManager.IsDarkMode();

                Color infoColor = dark ? Color.White : Color.Black;
                Color warnColor = dark ? Color.Orange : Color.DarkOrange;
                Color debugColor = dark ? Color.Gray : Color.DimGray;
                Color errorColor = dark ? Color.Red : Color.DarkRed;

                Color color = entry.level switch
                {
                    LogLevel.Error => errorColor,
                    LogLevel.Warning => warnColor,
                    LogLevel.Debug => debugColor,
                    _ => infoColor
                };


                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionColor = color;
                txtLog.AppendText(line);
                txtLog.SelectionColor = txtLog.ForeColor;
            }

            txtLog.ScrollToCaret();
            txtLog.ResumeLayout();
        }
        private void OnDownloadAverageUpdated(double media)
        {
            CoreLogger.Log("Evento recebido: média =" + media, LogLevel.Warning);

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnDownloadAverageUpdated(media)));
                return;
            }

            lblDownload.Text = $"Tempo médio de download: {media:F1} ms";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Obtém o serviço agora que a app já terminou de iniciar tudo
            _thumbnailService = AppEnvironment.ThumbnailService;

            if (_thumbnailService == null)
            {
                CoreLogger.Log("ThumbnailService ainda não carregado no OnShown!", LogLevel.Error);
                return;
            }

            CoreLogger.Log("Assinando evento de média de download...", LogLevel.Warning);

            _thumbnailService.DownloadAverageUpdated += OnDownloadAverageUpdated;
        }

    }
}
