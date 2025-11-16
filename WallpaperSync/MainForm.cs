using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperSync;

namespace WallpaperSync
{
    public partial class MainForm : Form
    {
        private readonly List<ImageEntry> Images = new();
        private readonly string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperSyncGUI");
        private readonly string cacheDir;
        private readonly string backupDir;
        private readonly string transcodedPath;

        private readonly HttpClient http;
        private CatalogLoader catalogLoader;
        private ImageDownloader imageDownloader;
        private ThumbnailService thumbnailService;
        private GridRenderer gridRenderer;
        private UiService uiService;
        private UndoManager undoManager;

        public MainForm()
        {
            InitializeComponent();

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
            undoManager = new UndoManager(backupDir);

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;

            DebugLogger.Log("MainForm inicializada.");
        }
        private async void MainForm_Load(object sender, EventArgs e)
        {
#if DEBUG
            var logForm = new DebugLogForm();
            logForm.Show();
            DebugLogger.Log("Aplicativo iniciado em modo DEBUG.");
#endif
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
        }

        private async Task OnThumbnailClicked(ImageEntry entry)
        {
            uiService.ToggleControls(false);
            uiService.SetStatus($"Preparando preview: {entry.Name}");

            try
            {
                string path = await imageDownloader.DownloadOriginalAsync(entry);
                using var preview = new PreviewForm(entry.Name, path);
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
                        DebugLogger.Log("MainForm: Imagem aplicada com sucesso.");
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

        private async void BtnApplyFromFile_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp", Title = "Escolher imagem" };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            try
            {
                var imageTransformer = new ImageTransformer();
                var backupService = new BackupService(backupDir);
                var wallpaperApplier = new WallpaperApplier(transcodedPath);
                var workflow = new WallpaperWorkflow(imageTransformer, backupService, wallpaperApplier, transcodedPath);

                bool applied = await workflow.ApplyAsync(ofd.FileName);
                if (applied) DebugLogger.Log("MainForm: aplicado de arquivo com sucesso.");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"ApplyFromFile erro: {ex.Message}");
                MessageBox.Show($"Erro ao aplicar: {ex.Message}");
            }
        }

        private async void BtnApplyFromUrl_Click(object sender, EventArgs e)
        {
            string url = Microsoft.VisualBasic.Interaction.InputBox("Digite a URL da imagem:", "Aplicar wallpaper da URL", "");
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                var saved = await imageDownloader.DownloadCustomAsync(url);

                var imageTransformer = new ImageTransformer();
                var backupService = new BackupService(backupDir);
                var wallpaperApplier = new WallpaperApplier(transcodedPath);
                var workflow = new WallpaperWorkflow(imageTransformer, backupService, wallpaperApplier, transcodedPath);

                bool applied = await workflow.ApplyAsync(saved);
                if (applied) DebugLogger.Log("MainForm: aplicado de URL com sucesso.");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"ApplyFromUrl erro: {ex.Message}");
                MessageBox.Show($"Erro ao aplicar: {ex.Message}");
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

        private void btnUndo_Click(object sender, EventArgs e)
        {
            DebugLogger.Log("Usuário clicou em UNDO.");
            try
            {
                var last = undoManager.GetLastBackup();
                if(string.IsNullOrEmpty(last))
                {
                    MessageBox.Show("Nenhum backup disponível.");
                    return;
                }

                var restored = undoManager.Restore(last, transcodedPath);
                var ok = WallpaperManager.SetWallpaper(transcodedPath);
                DebugLogger.Log(ok ? "Undo: wallpaper restaurado com sucesso." : "Undo: restaurado mas API retornou falha.");
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Erro ao restaurar wallpaper: {ex.Message}");
                MessageBox.Show($"Erro ao restaurar: {ex.Message}");
            }
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
            }
        }

        private async void listWallpapers_DoubleClick(object sender, EventArgs e)
        {
            if (listWallpapers.SelectedItem is not ImageEntry img) return;
            try
            {
                string path = await imageDownloader.DownloadOriginalAsync(img);
                using var preview = new PreviewForm(img.Name, path);
                preview.ShowDialog();
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Erro ao abrir preview: {ex.Message}");
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private async Task RefreshViewAsync()
        {
            flpGrid.Visible = chkShowPreviews.Checked;
            listWallpapers.Visible = !chkShowPreviews.Checked;
            DebugLogger.Log($"RefreshViewAsync > previews={chkShowPreviews.Checked}");

            if (chkShowPreviews.Checked)
            {
                DebugLogger.Log("Mostrando grade");
                await gridRenderer.RenderAsync(Images, lblStatus);
            }
            else
            {
                PopulateList();
                DebugLogger.Log("Mostrando lista");
            }
        }

        private void PopulateList()
        {
            listWallpapers.BeginUpdate();
            listWallpapers.Items.Clear();
            foreach (var img in Images)
            {
                listWallpapers.Items.Add(img);
            }
            listWallpapers.EndUpdate();
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(int action, int uParam, string vParam, int winIni);

        public static bool SetWallpaper(string file)
        {
            return SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, file,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}