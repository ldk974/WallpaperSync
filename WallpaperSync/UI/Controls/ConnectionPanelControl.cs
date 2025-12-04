using System.Windows.Forms;
using System.Drawing;

namespace WallpaperSync.UI.Controls
{
    public class ConnectionPanelControl : UserControl
    {
        public ConnectionPanelControl()
        {
            Dock = DockStyle.Fill;
            Controls.Add(new Label { Text = "Painel de conexão (em construção)", AutoSize = true, Font = new Font("Segoe UI", 14), Left = 20, Top = 20 });
        }
    }
}