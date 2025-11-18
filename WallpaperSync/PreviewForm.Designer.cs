namespace WallpaperSync
{
    partial class PreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox pbPreview;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblTitle;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            pbPreview = new PictureBox();
            btnApply = new Button();
            btnCancel = new Button();
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
            // btnApply
            // 
            btnApply.Anchor = AnchorStyles.Bottom;
            btnApply.Location = new Point(187, 393);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(100, 30);
            btnApply.TabIndex = 1;
            btnApply.Text = "Aplicar";
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom;
            btnCancel.Location = new Point(327, 393);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 30);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancelar";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
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
            Controls.Add(btnCancel);
            Controls.Add(btnApply);
            Controls.Add(pbPreview);
            Font = new Font("Segoe UI", 9F);
            Name = "PreviewForm";
            Text = "Preview";
            ((System.ComponentModel.ISupportInitialize)pbPreview).EndInit();
            ResumeLayout(false);
        }
    }
}
