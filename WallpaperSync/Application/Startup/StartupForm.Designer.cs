using System.Diagnostics;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace WallpaperSync.Application.Startup
{
    partial class StartupForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.Button btnUseFile;
        private System.Windows.Forms.Button btnUseUrl;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartupForm));
            btnDefault = new Button();
            btnUseFile = new Button();
            btnUseUrl = new Button();
            pictureBox1 = new PictureBox();
            btnRestore = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // btnDefault
            // 
            btnDefault.Location = new Point(43, 223);
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
            btnUseFile.Location = new Point(43, 265);
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
            btnUseUrl.Location = new Point(43, 307);
            btnUseUrl.Margin = new Padding(3, 2, 3, 2);
            btnUseUrl.Name = "btnUseUrl";
            btnUseUrl.Size = new Size(228, 38);
            btnUseUrl.TabIndex = 4;
            btnUseUrl.Text = "Usar Url";
            btnUseUrl.UseVisualStyleBackColor = true;
            btnUseUrl.Click += btnUseUrl_Click;
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(43, 349);
            btnRestore.Margin = new Padding(3, 2, 3, 2);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(228, 38);
            btnRestore.TabIndex = 6;
            btnRestore.Text = "Restaurar";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += btnRestore_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(59, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(200, 200);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // StartupForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 31, 34);
            ClientSize = new Size(315, 409);
            Controls.Add(btnRestore);
            Controls.Add(pictureBox1);
            Controls.Add(btnUseUrl);
            Controls.Add(btnUseFile);
            Controls.Add(btnDefault);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            Icon = Properties.Resources.logo;
            Name = "StartupForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WallpaperSync";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }
        private PictureBox pictureBox1;
        private Button btnRestore;
    }
}