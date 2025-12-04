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
        private readonly PerformanceMonitor _performanceMonitor;

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
            _workflow = new WallpaperWorkflow(new WallpaperTransformer(), _backupService, _applier, _env.TranscodedWallpaper);
            _performanceMonitor = new PerformanceMonitor();

            AppEnvironment.ThumbnailService = new ThumbnailService(_cache, _env.CacheRoot);


            Load += StartupForm_Load;
            FormClosing += StartupForm_FormClosing;
        }

        private void StartupForm_Load(object? sender, EventArgs e)
        {
            var Settings = SettingsManager.Load();

            if (Settings.DebugMode)
            {
                var logService = new LogService();
                CoreLogger.LogService = logService;

                var logForm = new DebugLogForm(logService, _performanceMonitor);
                logForm.Show();
                CoreLogger.Log("WallpaperSync iniciado em DEBUG.", LogLevel.Debug);
            }

            CoreLogger.Log("StartupForm carregado e tema aplicado.", LogLevel.Info);
            ThemeManager.ApplyTheme(this);
        }

        private void StartupForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CoreLogger.Log("Fechando WallpaperSync. Limpando cache e liberando serviços...", LogLevel.Info);
            _cache.Dispose();
            _env.CleanupCache();
        }

        private void btnUseDefault_Click(object sender, EventArgs e)
        {
            CoreLogger.Log("StartupForm: Usuário selecionou: Repositório padrão.", LogLevel.Info);
            var main = new MainForm(this);
            Hide();
            main.Show();

            CoreLogger.Log("StartupForm: MainForm exibido com sucesso.", LogLevel.Debug);
        }

        private async void btnUseFile_Click(object sender, EventArgs e)
        {
            CoreLogger.Log("StartupForm: Usuário selecionou: Escolher arquivo.", LogLevel.Info);

            using var ofd = new OpenFileDialog
            {
                Title = "Escolher imagem",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.webp"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
            {
                CoreLogger.Log("StartupForm: Nenhum arquivo selecionado pelo usuário.", LogLevel.Warning);
                return;
            }

            string path = ofd.FileName;
            CoreLogger.Log($"StartupForm: Arquivo selecionado: {path}", LogLevel.Debug);

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
                CoreLogger.Log("StartupForm: Usuário escolheu: Aplicar agora.", LogLevel.Info);

                var applied = await _workflow.ApplyAsync(path).ConfigureAwait(false);
                if (!applied)
                {
                    Invoke(() => MessageBox.Show($"Erro ao aplicar o wallpaper: Código {ErrorCodes.START_WorkflowApplyFailed}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("StartupForm: wallpaper aplicado com sucesso.", LogLevel.Info);
                }
            }
            else if (result == laterButton)
            {
                CoreLogger.Log("StartupForm: Usuário escolheu: Aplicar ao reiniciar.", LogLevel.Info);

                _backupService.CreateBackupIfExists(_env.TranscodedWallpaper);
                var applied = _applier.ApplyViaTranscodedWallpaper(path);
                if (!applied)
                {
                    Invoke(() => MessageBox.Show($"Erro ao copiar o wallpaper: Código`{ErrorCodes.START_CopyViaTranscodedFailed}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("StartupForm: wallpaper copiado com sucesso.", LogLevel.Info);
                }
            }
        }

        private async void btnUseUrl_Click(object sender, EventArgs e)
        {
            CoreLogger.Log("StartupForm: Usuário selecionou: Aplicar wallpaper via URL.", LogLevel.Info);

            var url = Microsoft.VisualBasic.Interaction.InputBox(
                "Digite a URL da imagem:",
                "Aplicar wallpaper da URL",
                "");

            if (string.IsNullOrWhiteSpace(url))
            {
                CoreLogger.Log("StartupForm: URL vazia ou inválida informada pelo usuário.", LogLevel.Warning);
                return;
            }

            CoreLogger.Log($"StartupForm: URL informada: {url}", LogLevel.Debug);

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
                CoreLogger.Log("StartupForm: Usuário escolheu: Aplicar agora.", LogLevel.Info);

                var applied = await _workflow.ApplyAsync(saved).ConfigureAwait(false);
                if (!applied)
                {
                    Invoke(() => MessageBox.Show($"Erro ao aplicar o wallpaper: Código {ErrorCodes.START_WorkflowApplyFailed}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("StartupForm: wallpaper aplicado com sucesso.", LogLevel.Info);
                }
            }
            else if (result == laterButton)
            {
                CoreLogger.Log("StartupForm: Usuário escolheu: Aplicar ao reiniciar.", LogLevel.Info);

                _backupService.CreateBackupIfExists(_env.TranscodedWallpaper);
                var applied = _applier.ApplyViaTranscodedWallpaper(saved);
                if (!applied)
                {
                    Invoke(() => MessageBox.Show($"Erro ao copiar o wallpaper: Código {ErrorCodes.START_CopyViaTranscodedFailed}", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                }
                else
                {
                    CoreLogger.Log("StartupForm: wallpaper copiado com sucesso.", LogLevel.Info);
                }
            }
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            CoreLogger.Log("StartupForm: Usuário abriu o menu de restauração.", LogLevel.Info);

            var restore = new RestoreForm(_undoManager, _env.TranscodedWallpaper);
            restore.Show(this);

            CoreLogger.Log("StartupForm: RestoreForm exibido.", LogLevel.Debug);
        }
    }
}
