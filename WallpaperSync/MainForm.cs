using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Interop;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallpaperSync
{
    public partial class MainForm : Form
    {
        private readonly string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperSyncGUI");
        private readonly string cacheDir;
        private readonly string backupDir;
        private readonly string transcodedPath;

        private List<ImageEntry> Images = new List<ImageEntry>();
        private List<ImageEntry> VisibleImages = new List<ImageEntry>();
        private string currentCategory = "All";

        private bool isHamburguerOpen = true;
        private System.Windows.Forms.Timer slideTimer;

        private HttpClient http;
        private CatalogLoader catalogLoader;
        private ImageDownloader imageDownloader;
        private ThumbnailService thumbnailService;
        private GridRenderer gridRenderer;
        private UiService uiService;

        public MainForm()
        {
            InitializeComponent();

            listCategories.SelectedIndexChanged += async (s, e) =>
            {
                if (listCategories.SelectedIndex < 0) return;
                var cat = listCategories.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(cat)) return;

                currentCategory = cat;
                await ApplyCategoryFilterAndRefreshAsync();
                ToggleHamburguer();
            };

            slideTimer = new System.Windows.Forms.Timer { Interval = 15 };
            slideTimer.Tick += (s, e) =>
            {
                int targetX = isHamburguerOpen ? 0 : -240;

                int distance = targetX - panelHamburguer.Left;

                int step = (int)(distance * 0.25);

                if (Math.Abs(step) < 2)
                    step = Math.Sign(step) * 2;

                panelHamburguer.Left += step;

                if (Math.Abs(panelHamburguer.Left - targetX) <= 1)
                {
                    panelHamburguer.Left = targetX;
                    slideTimer.Stop();
                }
            };

            http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36"
            );

            cacheDir = Path.Combine(appdata, "cache");
            backupDir = Path.Combine(appdata, "backup");
            transcodedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Themes\TranscodedWallpaper"
            );

            Directory.CreateDirectory(cacheDir);
            Directory.CreateDirectory(backupDir);

            listWallpapers.DisplayMember = "Name";
            chkShowPreviews.Checked = false;

            catalogLoader = new CatalogLoader(http);
            imageDownloader = new ImageDownloader(http, cacheDir);
            thumbnailService = new ThumbnailService(imageDownloader, cacheDir, concurrency: 6);
            gridRenderer = new GridRenderer(flpGrid, thumbnailService, OnThumbnailClicked);
            uiService = new UiService(this, chkShowPreviews, btnRefresh, btnUndo, lblStatus);

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;

            DebugLogger.Log("MainForm inicializada.");
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
            ThemeManager.ApplyTheme(this);

            try
            {
                uiService.ToggleControls(false);
                uiService.SetStatus("Carregando catálogo...");

                await LoadImages();
                await RefreshViewAsync();

                uiService.SetStatus($"Catálogo carregado ({Images.Count} imagens)");
            }
            finally
            {
                uiService.ToggleControls(true);
            }
        }

        private async Task LoadImages()
        {
            DebugLogger.Log("Iniciando MainForm.LoadImages");
            Images.Clear();
            listWallpapers.Items.Clear();

            const string urlTxt = "https://raw.githubusercontent.com/ldk974/WallpaperSync/refs/heads/master/current_urls.txt";
            var loaded = await catalogLoader.LoadCatalogAsync(urlTxt);
            Images.AddRange(loaded);
            DebugLogger.Log($"MainForm.LoadImages finalizado: {Images.Count} imagens.");

            currentCategory = "All";
            VisibleImages = Images.ToList();

            // popula categorias no menu hamburguer
            var cats = await ImageDownloader.GetCategoriesFromEntriesAsync(Images);
            listCategories.Items.Clear();
            foreach (var c in cats)
                listCategories.Items.Add(c);

            // seleciona "All" por padrão
            if (listCategories.Items.Count > 0)
                listCategories.SelectedIndex = 0;

            await RefreshViewAsync();
        }

        private async Task OnThumbnailClicked(ImageEntry entry)
        {
            uiService.ToggleControls(false);
            uiService.SetStatus($"Preparando preview: {entry.Name}");

            try
            {
                string path = await imageDownloader.DownloadOriginalAsync(entry);
                using var preview = new PreviewForm(entry.Name, path);
                uiService.SetStatus($"Mostrando preview: {entry.Name}");
                var res = preview.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    var imageTransformer = new ImageTransformer();
                    var backupService = new BackupService(backupDir);
                    var wallpaperApplier = new WallpaperApplier(transcodedPath);
                    var workflow = new WallpaperWorkflow(imageTransformer, backupService, wallpaperApplier, transcodedPath);

                    var applied = await workflow.ApplyAsync(path);
                    if (applied)
                    {
                        DebugLogger.Log("MainForm OTC: Imagem aplicada com sucesso.");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"MainForm.OnThumbnailClicked erro: {ex.Message}");
                MessageBox.Show($"Erro: {ex.Message}");
            }
            finally
            {
                uiService.ToggleControls(true);
                uiService.SetStatus($"Catálogo carregado ({Images.Count} imagens)");
            }
        }

        private async void listWallpapers_DoubleClick(object sender, EventArgs e)
        {
            if (listWallpapers.SelectedItem is not ImageEntry img) return;
            try
            {
                string path = await imageDownloader.DownloadOriginalAsync(img);
                using var preview = new PreviewForm(img.Name, path);
                var res = preview.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    var imageTransformer = new ImageTransformer();
                    var backupService = new BackupService(backupDir);
                    var wallpaperApplier = new WallpaperApplier(transcodedPath);
                    var workflow = new WallpaperWorkflow(imageTransformer, backupService, wallpaperApplier, transcodedPath);

                    var applied = await workflow.ApplyAsync(path);
                    if (applied)
                    {
                        DebugLogger.Log("MainForm LWP: Imagem aplicada com sucesso.");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"listWallpapers_DoubleClick: {ex.Message}");
                MessageBox.Show($"Erro: {ex.Message}");
            }
            finally
            {
                uiService.ToggleControls(true);
                uiService.SetStatus($"Catálogo carregado ({Images.Count} imagens)");
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("Usuario clicou Refresh");
            uiService.ToggleControls(false);
            uiService.SetStatus("Atualizando catálogo...");
            try
            {
                await LoadImages();
                await RefreshViewAsync();
            }
            finally
            {
                uiService.ToggleControls(true);
            }
        }

        private async void chkShowPreviews_CheckedChanged(object sender, EventArgs e)
        {
            await RefreshViewAsync();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (Directory.Exists(cacheDir))
                    Directory.Delete(cacheDir, true);

                DebugLogger.Log("Cache apagado ao fechar o app.");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Erro ao limpar cache: {ex.Message}");
            }
            finally
            {
                thumbnailService.Dispose();
                Application.Exit();
            }
        }

        private async Task RefreshViewAsync()
        {
            flpGrid.Visible = chkShowPreviews.Checked;
            listWallpapers.Visible = !chkShowPreviews.Checked;
            DebugLogger.Log($"RefreshViewAsync > previews={chkShowPreviews.Checked}");

            if (chkShowPreviews.Checked)
            {
                DebugLogger.Log($"RefreshViewAsync: mostrando grid com {VisibleImages.Count} itens");
                await gridRenderer.RenderAsync(VisibleImages, lblStatus);
            }
            else
            {
                PopulateList();
                DebugLogger.Log($"RefreshViewAsync: mostrando lista com {VisibleImages.Count} itens");
            }
        }

        private void PopulateList()
        {
            listWallpapers.BeginUpdate();
            listWallpapers.Items.Clear();
            foreach (var img in VisibleImages)
            {
                listWallpapers.Items.Add(img);
            }
            listWallpapers.EndUpdate();
        }
        private async Task ApplyCategoryFilterAndRefreshAsync()
        {
            if (currentCategory == "All" || string.IsNullOrWhiteSpace(currentCategory))
            {
                VisibleImages = Images.ToList();
            }
            else
            {
                VisibleImages = Images
                    .Where(e => string.Equals(ImageDownloader.ExtractCategoryFromUrl(e.OriginalUrl), currentCategory, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            DebugLogger.Log($"CategoryFilter: '{currentCategory}' -> {VisibleImages.Count} imagens.");
            await RefreshViewAsync();
        }
        private void ToggleHamburguer()
        {
            isHamburguerOpen = !isHamburguerOpen;
            if (isHamburguerOpen)
            {
                panelHamburguer.Visible = true;
            }
            slideTimer.Start();
        }


    }

    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control c, Action a)
        {
            if (c == null) return;
            if (c.InvokeRequired) c.Invoke(a);
            else a();
        }
    }
    public class ImageEntry
    {
        public string Name { get; set; }
        public string FileServerName { get; set; }
        public string FileDecodedName { get; set; }
        public string OriginalUrl { get; set; }
        public string FileId { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
        public string FileName

        {
            get
            {
                try
                {
                    var p = new Uri(OriginalUrl).AbsolutePath;
                    return Path.GetFileName(Uri.UnescapeDataString(p));
                }
                catch
                {
                    return Name ?? OriginalUrl;
                }
            }
        }
    }
    public static class WallpaperManager
    {
        private const int SPI_SETDESKWALLPAPER = 0x14;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(
            int uiAction,
            int uiParam,
            string pvParam,
            int fWinIni);

        public static bool SetWallpaper(string file)
        {
            try
            {
                file = Path.GetFullPath(file);

                DebugLogger.Log($"WallpaperManager.SetWallpaper: tentando aplicar '{file}'");

                bool ok = SystemParametersInfo(
                    SPI_SETDESKWALLPAPER,
                    0,
                    file,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);

                if (!ok)
                {
                    int err = Marshal.GetLastWin32Error();
                    DebugLogger.Log($"SystemParametersInfo falhou. Win32Error={err}");
                }

                return ok;
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"WallpaperManager.SetWallpaper: exceção: {ex.Message}");
                return false;
            }
        }
    }
}