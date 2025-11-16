using System;
using System.Windows.Forms;

namespace WallpaperSync
{
    public class UiService
    {
        private readonly Control owner;
        private readonly CheckBox chkShowPreviews;
        private readonly Button btnRefresh;
        private readonly Button btnUndo;
        private readonly Label lblStatus;

        public UiService(Control owner, CheckBox chkShowPreviews, Button btnRefresh, Button btnUndo, Label lblStatus)
        {
            this.owner = owner;
            this.chkShowPreviews = chkShowPreviews;
            this.btnRefresh = btnRefresh;
            this.btnUndo = btnUndo;
            this.lblStatus = lblStatus;
        }

        public void ToggleControls(bool enabled)
        {
            chkShowPreviews.InvokeIfRequired(() => chkShowPreviews.Enabled = enabled);
            btnRefresh.InvokeIfRequired(() => btnRefresh.Enabled = enabled);
            btnUndo.InvokeIfRequired(() => btnUndo.Enabled = enabled);
        }

        public void SetStatus(string text)
        {
            lblStatus.InvokeIfRequired(() => lblStatus.Text = text);
            DebugLogger.Log($"UI: {text}");
        }
    }
}