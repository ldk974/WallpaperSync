using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WallpaperSync.UI.Components;

namespace WallpaperSync.UI.Dialogs
{
    public partial class PreviewForm : Form
    {
        private readonly string imagePath;
        private readonly string friendlyName;

        public PreviewForm(string name, string path)
        {
            InitializeComponent();
            friendlyName = name;
            imagePath = path;
            lblTitle.Text = name;
            Load += PreviewForm_Load;
        }

        private void PreviewForm_Load(object sender, EventArgs e)
        {
            ThemeManager.ApplyTheme(this);

            try
            {
                // carrega a imagem sem travar arquivo
                using (var img = Image.FromFile(imagePath))
                {
                    pbPreview.Image = new Bitmap(img);
                }
            }
            catch
            {
                // mostra uma mensagem simples se falhar
                lblTitle.Text = friendlyName + " (pré-visualização indisponível)";
            }
        }

        private void btnApplyNow_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void btnApplyLater_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
