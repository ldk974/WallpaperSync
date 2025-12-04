using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using static WallpaperSync.Infrastructure.Environment.AppEnvironment;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.UI.Components;
using WallpaperSync.Infrastructure.Services;

namespace WallpaperSync.UI.Controls
{
    public class LogsPanelControl : UserControl, IDisposable
    {
        private readonly ILogService _logService;
        private readonly Label _lblLevel;
        private readonly RichTextBox _txtLog;
        private readonly ComboBox _comboLevel;
        private readonly CheckBox _chkVerbose;
        private readonly TextBox _txtSearch;
        private readonly Button _btnExport;
        private readonly Button _btnClear;


        private readonly object _renderSync = new();

        public LogsPanelControl(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            Dock = DockStyle.Fill;

            // LABEL “Nível:”
            _lblLevel = new Label
            {
                AutoSize = true,
                Text = "Nível:",
                Left = 12,
                Top = 20,
                Font = new Font("Segoe UI", 14)
            };

            // COMBOBOX — nível de log
            _comboLevel = new ComboBox
            {
                Left = 74,
                Top = 20,
                Width = 96,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Popup
            };
            _comboLevel.Items.AddRange(new object[] { "INFO", "WARNING", "ERROR" });
            _comboLevel.SelectedIndex = 0;
            _comboLevel.SelectedIndexChanged += (s, e) => _ = RenderAsync();

            // CHECKBOX — Verbose
            _chkVerbose = new CheckBox
            {
                Left = 181,
                Top = 23,
                Text = "Verbose",
                Font = new Font("Segoe UI", 11)
            };
            _chkVerbose.CheckedChanged += (s, e) => _ = RenderAsync();

            // SEARCHBOX
            _txtSearch = new TextBox
            {
                Left = 16,
                Top = 60,
                Width = Width - 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 11)
            };
            _txtSearch.TextChanged += (s, e) => _ = RenderAsync();

            // MAIN TEXT LOG AREA
            _txtLog = new RichTextBox
            {
                Left = 16,
                Top = 100,
                Width = Width - 32,
                Height = Height - 180,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                ReadOnly = true,
                //HideSelection = false
            };

            // BUTTONS (Exportar / Limpar)
            var buttonsLayout = new TableLayoutPanel
            {
                Left = 12,
                Top = Height - 60,
                Width = Width - 22,
                Height = 40,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ColumnCount = 2
            };
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

            _btnExport = new Button
            {
                Text = "Exportar Logs",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11)
            };
            _btnExport.Click += async (s, e) => await ExportAsync();

            _btnClear = new Button
            {
                Text = "Limpar Log",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11)
            };
            _btnClear.Click += (s, e) =>
            {
                _logService.Clear();
                _ = RenderAsync();
            };

            buttonsLayout.Controls.Add(_btnExport, 0, 0);
            buttonsLayout.Controls.Add(_btnClear, 1, 0);

            // ADD CONTROLS
            
            Controls.Add(_lblLevel);
            Controls.Add(_comboLevel);
            Controls.Add(_chkVerbose);
            Controls.Add(_txtSearch);
            Controls.Add(buttonsLayout);
            Controls.Add(_txtLog);
            

            _logService.LogAppended += OnLogAppended;
            ApplyTheme();
            // initial render
            _ = RenderAsync();
        }

        private void OnLogAppended(LogEntry entry)
        {
            // append incrementally on UI thread
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendEntryToTextBox(entry)));
                return;
            }
            AppendEntryToTextBox(entry);
        }

        private void AppendEntryToTextBox(LogEntry entry)
        {
            lock (_renderSync)
            {
                var line = $"[{entry.Time:HH:mm:ss}] [{entry.Level}] {entry.Message}{Environment.NewLine}";
                var color = ChooseColorForLevel(entry.Level);
                _txtLog.SelectionStart = _txtLog.TextLength;
                _txtLog.SelectionColor = color;
                _txtLog.AppendText(line);
                _txtLog.SelectionColor = _txtLog.ForeColor;
                _txtLog.ScrollToCaret();
            }
        }

        private Color ChooseColorForLevel(LogLevel level)
        {
            bool dark = ThemeManager.IsDarkMode();
            return level switch
            {
                LogLevel.Error => dark ? Color.OrangeRed : Color.DarkRed,
                LogLevel.Warning => dark ? Color.Orange : Color.DarkOrange,
                LogLevel.Debug => dark ? Color.Gray : Color.DimGray,
                _ => dark ? Color.White : Color.Black
            };
        }

        private async Task RenderAsync()
        {
            // Full render (e.g., when filters change). Keep it async to avoid UI freeze.
            await Task.Yield();

            if (InvokeRequired)
            {
                BeginInvoke(new Action(async () => await RenderAsync()));
                return;
            }

            lock (_renderSync)
            {
                _txtLog.SuspendLayout();
                _txtLog.Clear();

                var selected = _comboLevel.SelectedItem?.ToString() ?? "INFO";
                var verbose = _chkVerbose.Checked;
                var search = _txtSearch.Text?.Trim().ToLowerInvariant() ?? string.Empty;

                LogLevel selLevel = selected switch
                {
                    "WARNING" => LogLevel.Warning,
                    "ERROR" => LogLevel.Error,
                    _ => LogLevel.Info
                };

                foreach (var entry in _logService.Entries)
                {
                    if (entry.Level == LogLevel.Debug && !verbose) continue;
                    if (entry.Level != LogLevel.Debug && entry.Level < selLevel) continue;
                    if (!string.IsNullOrWhiteSpace(search) && !entry.Message.ToLowerInvariant().Contains(search)) continue;

                    var line = $"[{entry.Time:HH:mm:ss}] [{entry.Level}] {entry.Message}{Environment.NewLine}";
                    var color = ChooseColorForLevel(entry.Level);
                    _txtLog.SelectionStart = _txtLog.TextLength;
                    _txtLog.SelectionColor = color;
                    _txtLog.AppendText(line);
                    _txtLog.SelectionColor = _txtLog.ForeColor;
                }

                _txtLog.ScrollToCaret();
                _txtLog.ResumeLayout();
            }
        }

        private async Task ExportAsync()
        {
            using var sfd = new SaveFileDialog { Filter = "Arquivo de texto (*.txt)|*.txt", FileName = $"ws_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            await _logService.ExportToFileAsync(sfd.FileName).ConfigureAwait(false);
            // ensure UI thread when logging
            if (this.IsHandleCreated)
                BeginInvoke(new Action(() => CoreLogger.Log($"Logs exportados para {sfd.FileName}", LogLevel.Info)));
        }

        private void ApplyTheme()
        {
            bool dark = ThemeManager.IsDarkMode();
            Color back = dark ? Color.FromArgb(15, 15, 15) : Color.FromArgb(250, 250, 250);
            Color fore = dark ? Color.White : Color.Black;
            Color divider = dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 200, 200);

            BackColor = back;

            _lblLevel.ForeColor = fore;
            _comboLevel.BackColor = back;
            _comboLevel.ForeColor = fore;
            _chkVerbose.ForeColor = fore;
            _txtLog.BackColor = back;
            _txtLog.ForeColor = fore;
            _txtSearch.BackColor = back;
            _txtSearch.ForeColor = fore;
            _btnExport.BackColor = back;
            _btnExport.ForeColor = fore;
            _btnClear.BackColor = back;
            _btnClear.ForeColor = fore;
        }

        public void Dispose()
        {
            _logService.LogAppended -= OnLogAppended;
        }
    }
}