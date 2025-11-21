using System.Diagnostics;
using System.Windows.Forms;
using WallpaperSync.UI.Components;

namespace WallpaperSync.Application.Shell
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.FlowLayoutPanel flpGrid;
        private System.Windows.Forms.CheckBox chkShowPreviews;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnHambuguer;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnPrev;
        private System.Windows.Forms.Button btnHome;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label pageStatus;
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
            btnNext = new Button();
            btnPrev = new Button();
            btnHome = new Button();
            lblStatus = new Label();
            pageStatus = new Label();
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
            chkShowPreviews.Checked = false;
            chkShowPreviews.CheckState = CheckState.Unchecked;
            chkShowPreviews.ForeColor = Color.White;
            chkShowPreviews.Location = new Point(53, 20);
            chkShowPreviews.Name = "chkShowPreviews";
            chkShowPreviews.Size = new Size(107, 19);
            chkShowPreviews.TabIndex = 1;
            chkShowPreviews.Text = "Mostrar prévias";
            chkShowPreviews.UseVisualStyleBackColor = true;
            chkShowPreviews.CheckedChanged += OnPreviewToggleChanged;
            // 
            // btnRefresh
            // 
            btnRefresh.Image = Properties.Resources.refresh_icon;
            btnRefresh.ImageAlign = ContentAlignment.MiddleCenter;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Location = new Point(740, 10);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(32, 32);
            btnRefresh.TabIndex = 2;
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += OnRefreshClicked;
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
            btnHambuguer.Click += (s, e) => ToggleHamburger();
            // 
            // btnNext
            // 
            btnNext.ImageAlign = ContentAlignment.MiddleCenter;
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNext.Location = new Point(705, 10);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(32, 32);
            btnNext.TabIndex = 2;
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += NextClick;
            // 
            // btnPrev
            // 
            btnPrev.ImageAlign = ContentAlignment.MiddleCenter;
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.FlatStyle = FlatStyle.Flat;
            btnPrev.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrev.Location = new Point(670, 10);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(32, 32);
            btnPrev.TabIndex = 2;
            btnPrev.UseVisualStyleBackColor = true;
            btnPrev.Click += PrevClick;
            // 
            // btnHome
            // 
            btnHome.ImageAlign = ContentAlignment.MiddleCenter;
            btnHome.FlatAppearance.BorderSize = 0;
            btnHome.FlatStyle = FlatStyle.Flat;
            btnHome.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnHome.Location = new Point(10, 10);
            btnHome.Name = "btnHome";
            btnHome.Size = new Size(32, 32);
            btnHome.TabIndex = 2;
            btnHome.UseVisualStyleBackColor = true;
            btnHome.Click += HomeClick;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(10, 7);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(250, 23);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Aguardando...";
            // 
            // pageStatus
            // 
            pageStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            pageStatus.TextAlign = ContentAlignment.MiddleRight;
            pageStatus.ForeColor = Color.White;
            pageStatus.Location = new Point(676, 3);
            pageStatus.Name = "pageStatus";
            pageStatus.Size = new Size(100, 23);
            pageStatus.TabIndex = 4;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(pageStatus);
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
            panelTop.Controls.Add(btnNext);
            panelTop.Controls.Add(btnPrev);
            panelTop.Controls.Add(btnHome);
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
            panelList.AutoScroll = false;
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
            listWallpapers.DoubleClick += OnListDoubleClick;
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
            ClientSize = new Size(705, 464);
            //Controls.Add(panelHamburguer);
            Controls.Add(panelList);
            Controls.Add(panelBottom);
            Controls.Add(panelTop);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 9F);
            Icon = Properties.Resources.logo;
            Name = "MainForm";
            Text = "WallpaperSync";
            StartPosition = FormStartPosition.CenterParent;
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
