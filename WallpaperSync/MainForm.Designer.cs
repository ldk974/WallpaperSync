using System.Diagnostics;
using System.Windows.Forms;
using WallpaperSync;

namespace WallpaperSync
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flpGrid;
        private System.Windows.Forms.CheckBox chkShowPreviews;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnHambuguer;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ListBox listWallpapers;
        private SmoothListBox listCategories;


        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            flpGrid = new FlowLayoutPanel();
            chkShowPreviews = new CheckBox();
            btnRefresh = new Button();
            btnUndo = new Button();
            btnHambuguer = new Button();
            lblStatus = new Label();
            panelBottom = new Panel();
            panelTop = new Panel();
            panelList = new Panel();
            panelHamburguer = new Panel();
            listWallpapers = new ListBox();
            listCategories = new SmoothListBox();
            panelBottom.SuspendLayout();
            panelTop.SuspendLayout();
            panelList.SuspendLayout();
            SuspendLayout();
            // 
            // flpGrid
            // 
            flpGrid.AutoScroll = true;
            flpGrid.BackColor = Color.FromArgb(30, 31, 34);
            flpGrid.Dock = DockStyle.Fill;
            flpGrid.Location = new Point(12, 0);
            flpGrid.Name = "flpGrid";
            flpGrid.Size = new Size(760, 382);
            flpGrid.TabIndex = 0;
            // 
            // chkShowPreviews
            // 
            chkShowPreviews.AutoSize = true;
            chkShowPreviews.Checked = true;
            chkShowPreviews.CheckState = CheckState.Checked;
            chkShowPreviews.ForeColor = Color.White;
            chkShowPreviews.Location = new Point(14, 16);
            chkShowPreviews.Name = "chkShowPreviews";
            chkShowPreviews.Size = new Size(107, 19);
            chkShowPreviews.TabIndex = 1;
            chkShowPreviews.Text = "Mostrar prévias";
            chkShowPreviews.UseVisualStyleBackColor = true;
            chkShowPreviews.CheckedChanged += chkShowPreviews_CheckedChanged;
            // 
            // btnRefresh
            // 
            btnRefresh.Image = Properties.Resources.refresh_icon;
            btnRefresh.ImageAlign = ContentAlignment.MiddleCenter;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Location = new Point(674, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(32, 32);
            //btnRefresh.Size = new Size(96, 28);
            btnRefresh.TabIndex = 2;
            //btnRefresh.Text = "Atualizar";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            //
            // btnHambuguer
            //
            btnHambuguer.Text = "☰";
            btnHambuguer.Size = new Size(36, 32);
            btnHambuguer.Left = 8;
            btnHambuguer.Top = 6;
            btnHambuguer.FlatStyle = FlatStyle.Flat;
            btnHambuguer.FlatAppearance.BorderSize = 0;
            btnHambuguer.UseVisualStyleBackColor = true;
            btnHambuguer.Click += (s, e) => ToggleHamburguer();
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(10, 7);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(760, 23);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Aguardando...";
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 432);
            panelBottom.Name = "panelBottom";
            panelBottom.Padding = new Padding(5);
            panelBottom.Size = new Size(784, 30);
            panelBottom.TabIndex = 2;
            // 
            // panelTop
            // 
            //panelTop.Controls.Add(btnHambuguer);
            panelTop.Controls.Add(btnRefresh);
            panelTop.Controls.Add(chkShowPreviews);
            panelTop.Dock = DockStyle.Top;
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Padding = new Padding(5);
            panelTop.Size = new Size(784, 50);
            panelTop.TabIndex = 3;
            // 
            // panelList
            // 
            panelList.Controls.Add(listWallpapers);
            panelList.Controls.Add(flpGrid);
            panelList.Dock = DockStyle.Fill;
            panelList.Location = new Point(0, 50);
            panelList.Name = "panelList";
            panelList.Padding = new Padding(12, 0, 12, 0);
            panelList.Size = new Size(784, 382);
            panelList.TabIndex = 1;
            //
            // panelHamburguer
            //
            panelHamburguer.Controls.Add(listCategories);
            panelHamburguer.Width = 180;
            panelHamburguer.Height = (ClientSize.Height*2);
            panelHamburguer.Left = -180;
            panelHamburguer.Top = panelTop.Bottom;
            panelHamburguer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            panelHamburguer.BackColor = Color.FromArgb(40, 40, 40);
            // 
            // listWallpapers
            // 
            listWallpapers.BackColor = Color.FromArgb(30, 31, 34);
            listWallpapers.BorderStyle = BorderStyle.None;
            listWallpapers.Dock = DockStyle.Fill;
            listWallpapers.Font = new Font("Segoe UI", 10F);
            listWallpapers.ForeColor = Color.White;
            listWallpapers.IntegralHeight = false;
            listWallpapers.ItemHeight = 17;
            listWallpapers.Location = new Point(12, 0);
            listWallpapers.Name = "listWallpapers";
            listWallpapers.Size = new Size(760, 382);
            listWallpapers.TabIndex = 0;
            listWallpapers.Visible = false;
            listWallpapers.DoubleClick += listWallpapers_DoubleClick;
            //
            // listCategories
            //
            listCategories.ItemHeight = 32;
            listCategories.BorderStyle = BorderStyle.None;
            listCategories.IntegralHeight= false;
            listCategories.BackColor = Color.FromArgb(40, 40, 40);
            listCategories.ForeColor = Color.White;
            // MainForm
            // 
            BackColor = Color.FromArgb(30, 31, 34);
            ClientSize = new Size(784, 462);
            //Controls.Add(panelHamburguer);
            Controls.Add(panelList);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            Font = new Font("Segoe UI", 9F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "WallpaperSync";
            panelBottom.ResumeLayout(false);
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelList.ResumeLayout(false);
            ResumeLayout(false);
        }
        private Panel panelHamburguer;
        private Panel panelBottom;
        private Panel panelTop;
        private Panel panelList;
    }
}
