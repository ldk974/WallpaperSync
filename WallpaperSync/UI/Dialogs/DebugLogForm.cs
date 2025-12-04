using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.UI.Components;
using WallpaperSync.UI.Controls;

namespace WallpaperSync.UI.Dialogs
{
    public partial class DebugLogForm : Form
    {
        private readonly ILogService _logService;
        private readonly PerformanceMonitor _performanceMonitor;

        private ListBox _navList;
        private Panel _contentPanel;
        private Panel _dividerLine;

        private LogsPanelControl _logsControl;
        private ConnectionPanelControl _connectionControl;
        private PerformancePanelControl _perfControl;

        public DebugLogForm(ILogService logService, PerformanceMonitor perfMonitor)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _performanceMonitor = perfMonitor ?? throw new ArgumentNullException(nameof(perfMonitor));

            Text = "Debug Tools";
            ClientSize = new Size(900, 600);
            MinimumSize = new Size(720, 550);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeLayout();

            // Apply persisted theme and listen for changes
            ApplyTheme();
        }

        private void InitializeLayout()
        {
            _navList = new ListBox { Dock = DockStyle.Left, Width = 220, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12) };
            _navList.Items.AddRange(new object[] { "Logs", "Conexão", "Desempenho" });
            _navList.SelectedIndex = 0;
            _navList.SelectedIndexChanged += NavList_SelectedIndexChanged;

            _dividerLine = new Panel { Dock = DockStyle.Left, Width = 1 };
            _contentPanel = new Panel { Dock = DockStyle.Fill };

            Controls.Add(_contentPanel);
            Controls.Add(_dividerLine);
            Controls.Add(_navList);

            // instantiate controls
            _logsControl = new LogsPanelControl(_logService);
            _connectionControl = new ConnectionPanelControl();
            _perfControl = new PerformancePanelControl(_performanceMonitor);

            _contentPanel.Controls.Add(_logsControl);
            _contentPanel.Controls.Add(_connectionControl);
            _contentPanel.Controls.Add(_perfControl);

            ShowPanel("Logs");
        }

        private void NavList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var sel = _navList.SelectedItem as string;
            ShowPanel(sel);
        }

        private void ShowPanel(string key)
        {
            _logsControl.Visible = false;
            _connectionControl.Visible = false;
            _perfControl.Visible = false;

            switch (key)
            {
                case "Conexão": _connectionControl.Visible = true; break;
                case "Desempenho": _perfControl.Visible = true; break;
                default: _logsControl.Visible = true; break;
            }
        }

        private void ApplyTheme()
        {
            bool dark = ThemeManager.IsDarkMode();
            Color back = dark ? Color.FromArgb(15, 15, 15) : Color.FromArgb(250, 250, 250);
            Color fore = dark ? Color.White : Color.Black;
            Color divider = dark ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 200, 200);

            BackColor = back;

            _navList.BackColor = back;
            _navList.ForeColor = fore;
            _dividerLine.BackColor = divider;
            _contentPanel.BackColor = back;

            _perfControl.ApplyTheme();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _perfControl.Dispose();
            _logsControl.Dispose();
            _logService.Dispose();
        }
    }
}