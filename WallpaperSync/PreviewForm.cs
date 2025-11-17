using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace WallpaperSync
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

            bool dark = ThemeManager.IsDarkModeEnabled();
            bool transparency = SystemTransparency.IsTransparencyEnabled();
            bool acrylicSupported = AcrylicEffect.IsAcrylicSupported();

            if (dark && transparency && acrylicSupported)
            {
                AcrylicEffect.ApplyAcrylic(this.Handle);
                DebugLogger.Log("PreviewForm: Acrylic aplicado.");
            }
            else
            {
                DebugLogger.Log("PreviewForm: Acrylic não aplicado (modo claro, ou sem transparência, ou sem suporte).");
            }

            try
            {
                // Carrega a imagem sem manter o arquivo bloqueado
                using (var img = Image.FromFile(imagePath))
                {
                    pbPreview.Image = new Bitmap(img);
                }
            }
            catch
            {
                // mostrar uma mensagem simples se falhar
                lblTitle.Text = friendlyName + " (pré-visualização indisponível)";
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
