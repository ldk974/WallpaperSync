using System;
using System.Windows.Forms;

namespace WallpaperSync.UI.Dialogs
{
    public partial class DebugLogForm : Form
    {
        public static DebugLogForm Instance { get; private set; }

#if DEBUG
        private TextBox txtLog;
#endif

        public DebugLogForm()
        {
#if DEBUG
            try
            {
                InitializeComponent();
            }
            catch
            {

            }

            BuildUI();
            Instance = this;
#else
            Instance = this;
#endif
        }

#if DEBUG
        private void BuildUI()
        {
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = System.Drawing.Color.Black,
                ForeColor = System.Drawing.Color.Lime,
                Font = new System.Drawing.Font("Consolas", 9)
            };

            Controls.Add(txtLog);
            Text = "Debug Log";
            Width = 800;
            Height = 400;
        }

        public void AppendLine(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLine), text);
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\r\n");
        }
#else
        public void AppendLine(string text)
        {

        }
#endif
    }
}
