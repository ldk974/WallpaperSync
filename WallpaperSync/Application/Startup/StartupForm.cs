using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperSync.Application.Shell;
using WallpaperSync.Domain.Workflows;
using WallpaperSync.Infrastructure.Environment;
using WallpaperSync.Infrastructure.Http;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.Infrastructure.SystemIntegration;
using WallpaperSync.UI.Components;
using WallpaperSync.UI.Dialogs;

namespace WallpaperSync.Application.Startup
{
    public partial class StartupForm : Form
    {
        private readonly AppEnvironment _env;
        private readonly ImageCacheService _cache;
        private readonly UndoManager _undoManager;
        private readonly WallpaperWorkflow _workflow;

        public StartupForm()
        {
            InitializeComponent();

            _env = AppEnvironment.CreateDefault();
            _env.Ensure();

            var http = HttpClientProvider.Shared;
            _cache = new ImageCacheService(http, _env.CacheRoot);
            _undoManager = new UndoManager(_env.BackupRoot);
            _workflow = new WallpaperWorkflow(
                new WallpaperTransformer(),
                new BackupService(_env.BackupRoot),
                new WallpaperApplier(_env.TranscodedWallpaper),
                _env.TranscodedWallpaper);

            Load += StartupForm_Load;
            FormClosing += StartupForm_FormClosing;
        }

        private void StartupForm_Load(object? sender, EventArgs e)
        {
#if DEBUG
            var logForm = new DebugLogForm();
            logForm.Show();
            CoreLogger.Log("WallpaperSync iniciado em DEBUG.");
#endif
            ThemeManager.ApplyTheme(this);
        }

        private void StartupForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _cache.Dispose();
            _env.CleanupCache();
        }

        private void btnUseDefault_Click(object sender, EventArgs e)
        {
            var main = new MainForm();
            main.FormClosed += (_, __) => Close();
            Hide();
            main.Show();
        }

        private async void btnUseFile_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Escolher imagem",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            await ApplyWorkflowAsync(ofd.FileName);
        }

        private async void btnUseUrl_Click(object sender, EventArgs e)
        {
            var url = Microsoft.VisualBasic.Interaction.InputBox(
                "Digite a URL da imagem:",
                "Aplicar wallpaper da URL",
                "");

            if (string.IsNullOrWhiteSpace(url))
                return;

            try
            {
                var saved = await _cache.SaveCustomAsync(url);
                await ApplyWorkflowAsync(saved);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao baixar URL: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            var restore = new RestoreForm(_undoManager, _env.TranscodedWallpaper);
            restore.Show(this);
        }

        private async Task ApplyWorkflowAsync(string path)
        {
            try
            {
                bool applied = await _workflow.ApplyAsync(path);
                if (!applied)
                    MessageBox.Show("Não foi possível aplicar o wallpaper.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar wallpaper: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
