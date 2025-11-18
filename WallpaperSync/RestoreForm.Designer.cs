using System.Diagnostics;
using System.Windows.Forms;
using WallpaperSync;

namespace WallpaperSync
{
    partial class RestoreForm
    {

        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnRestore;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnDeleteAll;
        private System.Windows.Forms.Button btnRestoreLast;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.PictureBox previewBox;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.FlowLayoutPanel btnPanel;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnRestore = new Button();
            btnDelete = new Button();
            btnOpenFolder = new Button();
            btnDeleteAll = new Button();
            btnRestoreLast = new Button();
            btnClose = new Button();
            lv = new ListView();
            Arquivo = new ColumnHeader();
            Data = new ColumnHeader();
            Tamanho = new ColumnHeader();
            imageListSmall = new ImageList(components);
            btnPanel = new FlowLayoutPanel();
            rightPanel = new Panel();
            previewBox = new PictureBox();
            lblInfo = new Label();
            btnPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewBox).BeginInit();
            SuspendLayout();
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(235, 3);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(120, 23);
            btnRestore.TabIndex = 4;
            btnRestore.Text = "Restaurar Selecionado";
            btnRestore.UseVisualStyleBackColor = true;
            btnRestore.Click += BtnRestore_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(129, 32);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(120, 23);
            btnDelete.TabIndex = 3;
            btnDelete.Text = "Excluir Selecionado";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.Location = new Point(109, 3);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new Size(120, 23);
            btnOpenFolder.TabIndex = 2;
            btnOpenFolder.Text = "Abrir pasta";
            btnOpenFolder.UseVisualStyleBackColor = true;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            // 
            // btnDeleteAll
            // 
            btnDeleteAll.Location = new Point(3, 32);
            btnDeleteAll.Name = "btnDeleteAll";
            btnDeleteAll.Size = new Size(120, 23);
            btnDeleteAll.TabIndex = 1;
            btnDeleteAll.Text = "Excluir todos";
            btnDeleteAll.UseVisualStyleBackColor = true;
            btnDeleteAll.Click += BtnDeleteAll_Click;
            // 
            // btnRestoreLast
            // 
            btnRestoreLast.Location = new Point(361, 3);
            btnRestoreLast.Name = "btnRestoreLast";
            btnRestoreLast.Size = new Size(140, 23);
            btnRestoreLast.TabIndex = 5;
            btnRestoreLast.Text = "Restaurar último";
            btnRestoreLast.UseVisualStyleBackColor = true;
            btnRestoreLast.Click += BtnRestoreLast_Click;
            // 
            // lv
            // 
            lv.Columns.AddRange(new ColumnHeader[] { Arquivo, Data, Tamanho });
            lv.Dock = DockStyle.Left;
            lv.FullRowSelect = true;
            lv.Location = new Point(0, 0);
            lv.Name = "lv";
            lv.Size = new Size(485, 481);
            lv.SmallImageList = imageListSmall;
            lv.TabIndex = 0;
            lv.UseCompatibleStateImageBehavior = false;
            lv.View = View.Details;
            lv.SelectedIndexChanged += Lv_SelectedIndexChanged;
            // 
            // Arquivo
            // 
            Arquivo.Text = "Arquivo";
            Arquivo.Width = 320;
            // 
            // Data
            // 
            Data.Text = "Data";
            Data.Width = 73;
            // 
            // Tamanho
            // 
            Tamanho.Text = "Tamanho";
            Tamanho.Width = 68;
            // 
            // imageListSmall
            // 
            imageListSmall.ColorDepth = ColorDepth.Depth32Bit;
            imageListSmall.ImageSize = new Size(96, 54);
            imageListSmall.TransparentColor = Color.Transparent;
            // 
            // btnPanel
            // 
            btnPanel.FlowDirection = FlowDirection.LeftToRight;
            btnPanel.WrapContents = false;
            btnPanel.AutoSize = true;
            btnPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPanel.Dock = DockStyle.None; // importante
            btnPanel.Controls.Add(btnOpenFolder);
            btnPanel.Controls.Add(btnRestore);
            btnPanel.Controls.Add(btnRestoreLast);
            btnPanel.Controls.Add(btnDeleteAll);
            btnPanel.Controls.Add(btnDelete);
            btnPanel.Name = "btnPanel";
            btnPanel.TabIndex = 1;
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(previewBox);
            rightPanel.Controls.Add(btnPanel);
            rightPanel.Controls.Add(lblInfo);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(12);
            rightPanel.TabIndex = 1;
            // 
            // previewBox
            // 
            previewBox.BackColor = Color.FromArgb(28, 32, 40);
            previewBox.Dock = DockStyle.Top;
            previewBox.Location = new Point(12, 36);
            previewBox.Name = "previewBox";
            previewBox.Size = new Size(400, 360);
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewBox.TabIndex = 0;
            previewBox.TabStop = false;
            // 
            // lblInfo
            // 
            lblInfo.Dock = DockStyle.Top;
            lblInfo.Location = new Point(12, 12);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(581, 24);
            lblInfo.TabIndex = 2;
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // RestoreForm
            // 
            ClientSize = new Size(1160, 450);
            Controls.Add(rightPanel);
            Controls.Add(lv);
            MinimumSize = new Size(1180, 490);
            Name = "RestoreForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "WallpaperSync";
            btnPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)previewBox).EndInit();

            rightPanel.Resize += (s, e) =>
            {
                btnPanel.Left = (rightPanel.ClientSize.Width - btnPanel.Width) / 2;
                btnPanel.Top = rightPanel.ClientSize.Height - btnPanel.Height - 12;
            };

            btnPanel.Left = (rightPanel.ClientSize.Width - btnPanel.Width) / 2;
            btnPanel.Top = rightPanel.ClientSize.Height - btnPanel.Height - 12;

            ResumeLayout(false);
        }
        private Panel rightPanel;
        private ListView lv;
        private ColumnHeader Arquivo;
        private ColumnHeader Data;
        private ColumnHeader Tamanho;
        private ImageList imageListSmall;
    }
}