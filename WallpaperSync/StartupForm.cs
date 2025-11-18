using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallpaperSync
{
    public partial class StartupForm : Form
    {
        private readonly string appdata = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WallpaperSyncGUI");
        private readonly string cacheDir;
        private readonly string backupDir;
        private readonly string transcodedPath;

        private readonly HttpClient http;
        private ImageDownloader imageDownloader;

        public StartupForm()
        {
            InitializeComponent();

            cacheDir = Path.Combine(appdata, "cache");
            backupDir = Path.Combine(appdata, "backup");
            transcodedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Themes\TranscodedWallpaper");

            Directory.CreateDirectory(cacheDir);
            Directory.CreateDirectory(backupDir);

            http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/124.0.0.0 Safari/537.36"
            );

            imageDownloader = new ImageDownloader(http, cacheDir);

            Load += StartupForm_Load;
            FormClosing += StartupForm_Closing;
        }

        private async void StartupForm_Load(object sender, EventArgs e)
        {
#if DEBUG
            var logForm = new DebugLogForm();
            logForm.Show();
            DebugLogger.Log("Aplicativo iniciado em modo DEBUG.");
#endif
            ThemeManager.ApplyTheme(this);
        }

        private void StartupForm_Closing(object sender, FormClosingEventArgs e)
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
        }


        private void btnUseDefault_Click(object sender, EventArgs e)
        {
            var mainForm = new MainForm();
            Hide();
            mainForm.Show();
        }

        private async void btnUseFile_Click(object sender, EventArgs e)
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

        private async void btnUseUrl_Click(object sender, EventArgs e)
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
    }
}
