using System;
using System.Windows.Forms;
using WallpaperSync.UI.Extensions;

namespace WallpaperSync.UI.Components
{
    public sealed class UiService
    {
        private readonly CheckBox _previewToggle;
        private readonly Button _refreshButton;
        private readonly Button _undoButton;
        private readonly Label _statusLabel;
        private readonly Label _pageLabel;

        public UiService(Control owner, CheckBox previewToggle, Button refreshButton, Button undoButton, Label statusLabel, Label pageStatus)
        {
            _ = owner ?? throw new ArgumentNullException(nameof(owner));
            _previewToggle = previewToggle ?? throw new ArgumentNullException(nameof(previewToggle));
            _refreshButton = refreshButton ?? throw new ArgumentNullException(nameof(refreshButton));
            _undoButton = undoButton ?? throw new ArgumentNullException(nameof(undoButton));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _pageLabel = pageStatus ?? throw new ArgumentNullException(nameof(pageStatus));
        }

        public void ToggleControls(bool enabled)
        {
            _previewToggle.InvokeIfRequired(() => _previewToggle.Enabled = enabled);
            _refreshButton.InvokeIfRequired(() => _refreshButton.Enabled = enabled);
            _undoButton.InvokeIfRequired(() => _undoButton.Enabled = enabled);
        }

        public void SetStatus(string message)
        {
            _statusLabel.InvokeIfRequired(() => _statusLabel.Text = message);
        }

        public void SetPage(string message)
        {
            _statusLabel.InvokeIfRequired(() => _pageLabel.Text = message);
        }
    }
}
