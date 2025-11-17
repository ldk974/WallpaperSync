using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WallpaperSync
{
    partial class StartupForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.Button btnUseFile;
        private System.Windows.Forms.Button btnUseUrl;
        private System.Windows.Forms.Label lblTitle;

        private void InitializeComponent()
        {
            btnDefault = new Button();
            btnUseFile = new Button();
            btnUseUrl = new Button();
            lblTitle = new Label();
            SuspendLayout();
            // 
            // btnDefault
            // 
            btnDefault.Location = new Point(44, 58);
            btnDefault.Margin = new Padding(3, 2, 3, 2);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(228, 38);
            btnDefault.TabIndex = 0;
            btnDefault.Text = "Usar Repertório Padrão";
            btnDefault.UseVisualStyleBackColor = true;
            btnDefault.Click += btnUseDefault_Click;
            // 
            // btnUseFile
            // 
            btnUseFile.Location = new Point(44, 100);
            btnUseFile.Margin = new Padding(3, 2, 3, 2);
            btnUseFile.Name = "btnUseFile";
            btnUseFile.Size = new Size(228, 38);
            btnUseFile.TabIndex = 3;
            btnUseFile.Text = "Usar Imagem Local";
            btnUseFile.UseVisualStyleBackColor = true;
            btnUseFile.Click += btnUseFile_Click;
            // 
            // btnUseUrl
            // 
            btnUseUrl.Location = new Point(44, 142);
            btnUseUrl.Margin = new Padding(3, 2, 3, 2);
            btnUseUrl.Name = "btnUseUrl";
            btnUseUrl.Size = new Size(228, 38);
            btnUseUrl.TabIndex = 4;
            btnUseUrl.Text = "Usar Url";
            btnUseUrl.UseVisualStyleBackColor = true;
            btnUseUrl.Click += btnUseUrl_Click;
            // 
            // lblTitle
            // 
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 18);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(294, 38);
            lblTitle.TabIndex = 2;
            lblTitle.Text = "WallpaperSync";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // StartupForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 31, 34);
            ClientSize = new Size(315, 195);
            Controls.Add(btnUseUrl);
            Controls.Add(btnUseFile);
            Controls.Add(lblTitle);
            Controls.Add(btnDefault);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Name = "StartupForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WallpaperSync Início";
            ResumeLayout(false);
        }
    }
}