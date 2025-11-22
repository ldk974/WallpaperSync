using System;
using System.IO;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace WallpaperSync.Application.Startup
{
    public partial class StartupForm : Form
    {
        private readonly AppEnvironment _env;
        private readonly ImageCacheService _cache;
        private readonly UndoManager _undoManager;
        private readonly BackupService _backupService;
        private readonly WallpaperApplier _applier;
        private readonly WallpaperWorkflow _workflow;

        public StartupForm()
        {
            InitializeComponent();

            _env = AppEnvironment.CreateDefault();
            _env.Ensure();

            var http = HttpClientProvider.Shared;
            _cache = new ImageCacheService(http, _env.CacheRoot);
            _undoManager = new UndoManager(_env.BackupRoot);
            _backupService = new BackupService(_env.BackupRoot);
            _applier = new WallpaperApplier(_env.TranscodedWallpaper);
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
            var main = new MainForm(this);
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

            string path = ofd.FileName;

            TaskDialogButton nowButton = new ("Aplicar agora");
            TaskDialogButton laterButton = new ("Aplicar ao reiniciar");

            var dialog = new TaskDialogPage
            {
                Caption = "Escolher ação",
                Heading = "O que você deseja fazer com a imagem selecionada?",
                Buttons = new TaskDialogButtonCollection() { nowButton, laterButton },
                Text = "Selecione uma das opções abaixo."
            };

            var result = TaskDialog.ShowDialog(dialog);

            if (result == nowButton)
            {
                var applied = await _workflow.ApplyAsync(path).ConfigureAwait(false);
                if (!applied)
                {
                    CoreLogger.Log("MainForm: workflow retornou falha ao aplicar wallpaper.");
                    Invoke(() => MessageBox.Show("Não foi possível aplicar o wallpaper.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("MainForm: wallpaper aplicado com sucesso.");
                }
            }
            else if (result == laterButton)
            {
                _backupService.CreateBackupIfExists(_env.TranscodedWallpaper);
                var applied = _applier.ApplyViaTranscodedWallpaper(path);
                if (!applied)
                {
                    CoreLogger.Log("MainForm: ApplyViaTranscodedWallpaper retornou falha ao copiar wallpaper.");
                    Invoke(() => MessageBox.Show("Não foi possível aplicar o wallpaper.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("MainForm: wallpaper copiado com sucesso.");
                }
            }
        }

        private async void btnUseUrl_Click(object sender, EventArgs e)
        {
            var url = Microsoft.VisualBasic.Interaction.InputBox(
                "Digite a URL da imagem:",
                "Aplicar wallpaper da URL",
                "");

            if (string.IsNullOrWhiteSpace(url))
                return;

            var saved = await _cache.SaveCustomAsync(url);

            TaskDialogButton nowButton = new("Aplicar agora");
            TaskDialogButton laterButton = new("Aplicar ao reiniciar");

            var dialog = new TaskDialogPage
            {
                Caption = "Escolher ação",
                Heading = "O que você deseja fazer com a imagem selecionada?",
                Buttons = new TaskDialogButtonCollection() { nowButton, laterButton },
                Text = "Selecione uma das opções abaixo."
            };

            var result = TaskDialog.ShowDialog(dialog);

            if (result == nowButton)
            {
                var applied = await _workflow.ApplyAsync(saved).ConfigureAwait(false);
                if (!applied)
                {
                    CoreLogger.Log("MainForm: workflow retornou falha ao aplicar wallpaper.");
                    Invoke(() => MessageBox.Show("Não foi possível aplicar o wallpaper.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("MainForm: wallpaper aplicado com sucesso.");
                }
            }
            else if (result == laterButton)
            {
                _backupService.CreateBackupIfExists(_env.TranscodedWallpaper);
                var applied = _applier.ApplyViaTranscodedWallpaper(saved);
                if (!applied)
                {
                    CoreLogger.Log("MainForm: ApplyViaTranscodedWallpaper retornou falha ao copiar wallpaper.");
                    Invoke(() => MessageBox.Show("Não foi possível aplicar o wallpaper.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("MainForm: wallpaper copiado com sucesso.");
                }
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            var restore = new RestoreForm(_undoManager, _env.TranscodedWallpaper);
            restore.Show(this);
        }
    }
}
