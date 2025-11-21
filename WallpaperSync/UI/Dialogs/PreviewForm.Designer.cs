namespace WallpaperSync.UI.Dialogs
{
    partial class PreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox pbPreview;
        private System.Windows.Forms.Button btnApplyNow;
        private System.Windows.Forms.Button btnApplyLater;
        private System.Windows.Forms.Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pbPreview = new PictureBox();
            btnApplyNow = new Button();
            btnApplyLater = new Button();
            lblTitle = new Label();
            ((System.ComponentModel.ISupportInitialize)pbPreview).BeginInit();
            SuspendLayout();
            // 
            // pbPreview
            // 
            pbPreview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbPreview.Location = new Point(12, 34);
            pbPreview.Name = "pbPreview";
            pbPreview.Size = new Size(614, 345);
            pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
            pbPreview.TabIndex = 0;
            pbPreview.TabStop = false;
            // 
            // btnApplyNow
            // 
            btnApplyNow.Anchor = AnchorStyles.Bottom;
            btnApplyNow.Location = new Point(187, 393);
            btnApplyNow.Name = "btnApplyNowNow";
            btnApplyNow.Size = new Size(100, 30);
            btnApplyNow.TabIndex = 1;
            btnApplyNow.Text = "Aplicar agora";
            btnApplyNow.UseVisualStyleBackColor = true;
            btnApplyNow.Click += btnApplyNow_Click;
            btnApplyNow.DialogResult = DialogResult.None;
            // 
            // btnApplyLater
            // 
            btnApplyLater.Anchor = AnchorStyles.Bottom;
            btnApplyLater.Location = new Point(327, 393);
            btnApplyLater.Name = "btnApplyLater";
            btnApplyLater.Size = new Size(130, 30);
            btnApplyLater.TabIndex = 2;
            btnApplyLater.Text = "Aplicar ao reiniciar";
            btnApplyLater.UseVisualStyleBackColor = true;
            btnApplyLater.Click += btnApplyLater_Click;
            btnApplyLater.DialogResult = DialogResult.None;
            // 
            // lblTitle
            // 
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(12, 10);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(614, 24);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "Titulo";
            lblTitle.TextAlign = ContentAlignment.TopCenter;
            // 
            // PreviewForm
            // 
            ClientSize = new Size(638, 437);
            Controls.Add(lblTitle);
            Controls.Add(btnApplyLater);
            Controls.Add(btnApplyNow);
            Controls.Add(pbPreview);
            Icon = Properties.Resources.logo;
            Font = new Font("Segoe UI", 9F);
            Name = "PreviewForm";
            Text = "Preview";
            ((System.ComponentModel.ISupportInitialize)pbPreview).EndInit();
            ResumeLayout(false);
        }
    }
}
