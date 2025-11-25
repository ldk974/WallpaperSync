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
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.UI.Components;

namespace WallpaperSync.UI.Dialogs
{
    public partial class DebugLogForm : Form
    {
        public static DebugLogForm Instance { get; private set; }
        private readonly List<(DateTime time, LogLevel level, string message)> logEntries
    = new List<(DateTime, LogLevel, string)>();

        private SmoothListBox navList;
        private Panel contentPanel;
        private Panel dividerLine;

        private Label levelLabel;
        private ComboBox logLevelCombo;
        private CheckBox verboseCheck;
        private TextBox searchBox;
        private Button exportLogsBtn;
        private Button clearLogsBtn;
        private RichTextBox txtLog;
        private bool logsPanelInitialized = false;

        private CartesianChart ramChart;
        private LineSeries<double> ramLineSeries;
        private ISeries[] ramSeries;
        private Axis[] ramXAxis;
        private Axis[] ramYAxis;
        private PerformanceCounter ramCounter;
        private System.Windows.Forms.Timer ramTimer;

        private Label lblDownload;
        private Label lblWallpaperTime;

        private bool performancePanelInitialized = false;

        public DebugLogForm()
        {
            Text = "Debug Tools";
            Width = 720;
            Height = 550;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(15,15,15);

            InitializeLayout();
            ApplyTheme();
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

            navList.Items.AddRange(new object[]
            {
        "Logs",
        "Conexão",
        "Desempenho"
            });

            // Linha divisória
            dividerLine = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 1
            };

            // Painel principal
            contentPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };

            Controls.Add(contentPanel);
            Controls.Add(dividerLine);
            Controls.Add(navList);

            navList.SelectedIndexChanged += NavList_SelectedIndexChanged;
            navList.SelectedIndex = 0;

            LoadLogsPanel();
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

            // Se o painel de logs já existe, aplica tema também
            if (logsPanelInitialized)
            {
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
            contentPanel.Controls.Clear();


            switch (navList.SelectedItem.ToString())
            {
                case "Logs": LoadLogsPanel(); break;
                case "Conexão": LoadPlaceholder("Connection Panel"); break;
                case "Desempenho": LoadPerformancePanel(); break;
            }
        }

        private void LoadPlaceholder(string text)
        {
            contentPanel.Controls.Add(new Label()
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 14)
            });
        }

        private void LoadLogsPanel()
        {
            if (logsPanelInitialized)  // já existe
            {
                ApplyTheme();
                contentPanel.Controls.Add(levelLabel);
                contentPanel.Controls.Add(logLevelCombo);
                contentPanel.Controls.Add(verboseCheck);
                contentPanel.Controls.Add(searchBox);
                contentPanel.Controls.Add(txtLog);
                contentPanel.Controls.Add(exportLogsBtn);
                contentPanel.Controls.Add(clearLogsBtn);
                return;
            }

            levelLabel = new Label()
            {
                AutoSize = true,
                Text = "Nível:",
                Left = 12,
                Top = 20,
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.Black
            };

            logLevelCombo = new ComboBox()
            {
                Left = 74,
                Top = 20,
                Width = 96,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(15, 15, 15),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Popup
            };
            logLevelCombo.Items.AddRange(new object[] { "INFO", "WARNING", "ERROR" });
            logLevelCombo.SelectedIndex = 0;
            logLevelCombo.SelectedIndexChanged += (s, e) => RenderLogs();

            contentPanel.Controls.Add(logLevelCombo);

            verboseCheck = new CheckBox()
            {
                Left = 181,
                Top = 23,
                Text = "Verbose",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White
            };
            verboseCheck.CheckedChanged += (s, e) => RenderLogs();

            contentPanel.Controls.Add(verboseCheck);

            searchBox = new TextBox()
            {
                Left = 16,
                Top = 60,
                Width = 410,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.White
            };
            searchBox.TextChanged += (s, e) => RenderLogs();

            contentPanel.Controls.Add(searchBox);

            txtLog = new RichTextBox()
            {
                Left = 16,
                Top = 100,
                Width = contentPanel.Width -32,
                Height = contentPanel.Height - 160,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                ReadOnly = true,
                Multiline = true,
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.White
            };

            contentPanel.Controls.Add(txtLog);

            exportLogsBtn = new Button()
            {
                Text = "Export Logs",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Width = 195,
                Height = 35,
                Left = 16,
                Top = txtLog.Bottom + 12,
                Font = new Font("Segoe UI", 11),
                UseVisualStyleBackColor = true,
            };

            clearLogsBtn = new Button()
            {
                Text = "Clear",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Width = 195,
                Height = 35,
                Left = exportLogsBtn.Right + 15,
                Top = txtLog.Bottom + 12,
                Font = new Font("Segoe UI", 11),
                UseVisualStyleBackColor = true
            };

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

            contentPanel.Controls.Add(exportLogsBtn);
            contentPanel.Controls.Add(clearLogsBtn);

            logsPanelInitialized = true;
        }

        private async void LoadPerformancePanel()
        {
            if (performancePanelInitialized)
            {
                contentPanel.Controls.Add(lblDownload);
                contentPanel.Controls.Add(lblWallpaperTime);
                contentPanel.Controls.Add(ramChart);
                return;
            }

            lblDownload = new Label()
            {
                Text = "Tempo médio de download: 0.0ms (placeholder)",
                AutoSize = true,
                Left = 10,
                Top = 10,
                Font = new Font("Segoe UI", 12),
                ForeColor = ThemeManager.IsDarkMode() ? Color.White : Color.Black
            };

            lblWallpaperTime = new Label()
            {
                Text = "Tempo para aplicar wallpaper: 0.0ms (placeholder)",
                AutoSize = true,
                Left = 10,
                Top = 40,
                Font = new Font("Segoe UI", 12),
                ForeColor = ThemeManager.IsDarkMode() ? Color.White : Color.Black
            };

            // inicia objetos pesados sem travar UI
            await Task.Run(() => PrepareChartObjects());

            // cria o controle no thread da UI
            CreateChartControl();

            // labels
            contentPanel.Controls.Add(lblDownload);
            contentPanel.Controls.Add(lblWallpaperTime);
            contentPanel.Controls.Add(ramChart);

            // timer
            StartRamTimer();

            performancePanelInitialized = true;

            ApplyChartTheme();
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

            SKColor gray = new SKColor(70, 70, 70);

            ramSeries = new ISeries[]
            {
        ramLineSeries
            };

            ramXAxis = new Axis[]
            {
        new Axis { Name = "Tempo (s)", TextSize = 12 }
            };

            ramYAxis = new Axis[]
            {
        new Axis { Name = "RAM usada (MB)", TextSize = 12, NamePaint = new SolidColorPaint(gray) }
            };
        }

        private void CreateChartControl()
        {
            ramChart = new CartesianChart
            {
                Series = ramSeries,
                XAxes = ramXAxis,
                YAxes = ramYAxis,
                Left = 10,
                Top = 80,
                Width = contentPanel.Width - 40,
                Height = contentPanel.Height - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
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
    }
}
