using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperSync.Application.Startup;
using WallpaperSync.Domain.Models;
using WallpaperSync.Domain.Workflows;
using WallpaperSync.Infrastructure.Environment;
using WallpaperSync.Infrastructure.Http;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.Infrastructure.SystemIntegration;
using WallpaperSync.UI.Components;
using WallpaperSync.UI.Dialogs;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace WallpaperSync.Application.Shell
{
    public partial class MainForm : Form
    {
        private readonly AppEnvironment _env;
        private readonly CatalogService _catalogService;
        private readonly ImageCacheService _imageCache;
        private readonly ThumbnailService _thumbnailService;
        private readonly GridRenderer _gridRenderer;
        private readonly UiService _ui;
        private readonly WallpaperTransformer _transformer;
        private readonly BackupService _backupService;
        private readonly WallpaperApplier _applier;
        private readonly WallpaperWorkflow _workflow;

        private IReadOnlyList<WallpaperItem> _catalog = Array.Empty<WallpaperItem>();
        private IReadOnlyList<WallpaperItem> _visible = Array.Empty<WallpaperItem>();
        private string _currentCategory = "All";
        private bool _hamburgerOpen = true;
        private readonly WinFormsTimer _slideTimer;
        private CancellationTokenSource? _viewCts;
        private bool _isPopulatingCategories;

        private Form _startup;

        private Image refreshIconDark;
        private Image refreshIconLight;
        private Image arrowDark;
        private Image arrowLight;
        private Image arrow_flipDark;
        private Image arrow_flipLight;
        private Image home_iconDark;
        private Image home_iconLight;

        public MainForm(Form startup)
        {
            InitializeComponent();

            _startup = startup;

            _env = AppEnvironment.CreateDefault();
            _env.Ensure();

            var http = HttpClientProvider.Shared;
            _catalogService = new CatalogService(http);
            _imageCache = new ImageCacheService(http, _env.CacheRoot);
            _thumbnailService = new ThumbnailService(_imageCache, _env.CacheRoot, concurrency: 6);
            _gridRenderer = new GridRenderer(flpGrid, _thumbnailService, HandleThumbnailClickAsync);
            _ui = new UiService(this, chkShowPreviews, btnRefresh, btnUndo, lblStatus, pageStatus);
            _transformer = new WallpaperTransformer();
            _backupService = new BackupService(_env.BackupRoot);
            _applier = new WallpaperApplier(_env.TranscodedWallpaper);
            _workflow = new WallpaperWorkflow(_transformer, _backupService, _applier, _env.TranscodedWallpaper);

            listWallpapers.DisplayMember = nameof(WallpaperItem.Name);
            chkShowPreviews.Checked = false;

            _slideTimer = new WinFormsTimer { Interval = 15 };
            _slideTimer.Tick += (_, __) => AnimateHamburger();

            refreshIconDark = ThemeManager.RecolorImage(Properties.Resources.refresh_icon, Color.White);
            refreshIconLight = ThemeManager.RecolorImage(Properties.Resources.refresh_icon, Color.Black);
            arrowDark = ThemeManager.RecolorImage(Properties.Resources.arrow, Color.White);
            arrowLight = ThemeManager.RecolorImage(Properties.Resources.arrow, Color.Black);
            arrow_flipDark = ThemeManager.RecolorImage(Properties.Resources.arrow_flip, Color.White);
            arrow_flipLight = ThemeManager.RecolorImage(Properties.Resources.arrow_flip, Color.Black);
            home_iconDark = ThemeManager.RecolorImage(Properties.Resources.home_icon, Color.White);
            home_iconLight = ThemeManager.RecolorImage(Properties.Resources.home_icon, Color.Black);

            Load += OnFormLoad;
            FormClosing += OnFormClosing;
        }

        private async void OnFormLoad(object? sender, EventArgs e)
        {
            try
            {
                await OnLoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao iniciar: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnCategoryChanged(object? sender, EventArgs e)
            => await RunSafelyAsync(ApplyFilterAsync, "Filtro de categoria");

        private async void OnPreviewToggleChanged(object? sender, EventArgs e)
            => await RunSafelyAsync(RefreshViewAsync, "Alternar previews");

        private async void OnListDoubleClick(object? sender, EventArgs e)
            => await RunSafelyAsync(HandleListDoubleClickAsync, "Duplo clique lista");

        private async void OnRefreshClicked(object? sender, EventArgs e)
            => await RunSafelyAsync(ReloadCatalogAsync, "Atualizar catálogo");
        private async void NextClick(object? sender, EventArgs e)
        {
            _viewCts?.Cancel();
            _viewCts?.Dispose();
            _viewCts = new CancellationTokenSource();
            var token = _viewCts.Token;
            await _gridRenderer.NextPageAsync(lblStatus, pageStatus, token);
        }

        private async void PrevClick(object? sender, EventArgs e)
        {
            _viewCts?.Cancel();
            _viewCts?.Dispose();
            _viewCts = new CancellationTokenSource();
            var token = _viewCts.Token;
            await _gridRenderer.PreviousPageAsync(lblStatus, pageStatus, token);
        }

        private async void HomeClick(object? sender, EventArgs e)
        {
            _startup.Show();
            Close();
        }

        private static async Task RunSafelyAsync(Func<Task> action, string context)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"{context} falhou: {ex.Message}");
            }
        }

        private async Task OnLoadAsync()
        {
            bool dark = ThemeManager.IsDarkMode();

            ThemeManager.ApplyTheme(this);
            btnRefresh.Image = dark ? refreshIconDark : refreshIconLight;
            btnNext.Image = dark ? arrowDark : arrowLight;
            btnPrev.Image = dark ? arrow_flipDark : arrow_flipLight;
            btnHome.Image = dark ? home_iconDark : home_iconLight;

            await ReloadCatalogAsync();
        }

        private async Task ReloadCatalogAsync()
        {
            _ui.ToggleControls(false);
            _ui.SetStatus("Carregando catálogo...");
            CoreLogger.Log("MainForm: iniciando recarregamento de catálogo.");

            var loaded = false;

            try
            {
                _catalog = await _catalogService.LoadAsync();
                _visible = _catalog;
                PopulateCategories();
                loaded = true;
                CoreLogger.Log($"MainForm: catálogo carregado com {_catalog.Count} itens.");
            }
            catch (Exception ex)
            {
                _ui.SetStatus("Falha ao carregar catálogo");
                CoreLogger.Log($"MainForm: falha ao carregar catálogo: {ex.Message}");
                MessageBox.Show($"Erro ao carregar catálogo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _ui.ToggleControls(true);
            }

            if (!loaded)
                return;

            _ui.SetStatus($"Catálogo carregado ({_catalog.Count} imagens)");
            await RefreshViewAsync();
        }

        private void PopulateCategories()
        {
            _isPopulatingCategories = true;
            try
            {
                var cats = _catalogService.BuildCategories(_catalog);
                listCategories.BeginUpdate();
                listCategories.Items.Clear();
                foreach (var cat in cats)
                {
                    listCategories.Items.Add(cat);
                }
                listCategories.EndUpdate();
                if (listCategories.Items.Count > 0)
                {
                    listCategories.SelectedIndex = 0;
                }
            }
            finally
            {
                _isPopulatingCategories = false;
            }
        }

        private async Task ApplyFilterAsync()
        {
            if (_isPopulatingCategories)
                return;

            if (listCategories.SelectedItem is not string category)
                return;

            _currentCategory = category;

            _visible = category == "All"
                ? _catalog
                : _catalog.Where(i => string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

            await RefreshViewAsync();
            ToggleHamburger();
        }

        private async Task RefreshViewAsync()
        {
            _viewCts?.Cancel();
            _viewCts?.Dispose();
            _viewCts = new CancellationTokenSource();
            var token = _viewCts.Token;

            flpGrid.Visible = chkShowPreviews.Checked;
            listWallpapers.Visible = !chkShowPreviews.Checked;

            if (chkShowPreviews.Checked)
            {
                await _gridRenderer.SetItemsAsync(_visible, lblStatus, pageStatus, token);
            }
            else
            {
                PopulateListBox();
                _ui.SetPage("");
            }
        }

        private void PopulateListBox()
        {
            listWallpapers.BeginUpdate();
            listWallpapers.Items.Clear();
            foreach (var item in _visible)
            {
                listWallpapers.Items.Add(item);
            }
            listWallpapers.EndUpdate();
            _ui.SetStatus($"Catálogo carregado ({_visible.Count} imagens)");
        }

        private async Task HandleThumbnailClickAsync(WallpaperItem item)
        {
            await HandleSelectionAsync(item);
        }

        private async Task HandleListDoubleClickAsync()
        {
            if (listWallpapers.SelectedItem is WallpaperItem item)
            {
                await HandleSelectionAsync(item);
            }
        }

        private async Task HandleSelectionAsync(WallpaperItem item)
        {
            _ui.ToggleControls(false);
            _ui.SetStatus($"Preparando preview: {item.Name}");

            try
            {
                var path = await _imageCache.EnsureOriginalAsync(item);
                CoreLogger.Log($"MainForm: preview solicitado para {item.Name}");

                using var preview = new PreviewForm(item.Name, path);
                var choice = preview.ShowDialog(this);

                if (choice == DialogResult.Yes)
                {
                    CoreLogger.Log($"MainForm: aplicando {item.Name} imediatamente.");
                    await ApplyWallpaperAsync(path);
                }
                else if (choice == DialogResult.OK)
                {
                    CoreLogger.Log($"MainForm: aplicando {item.Name} depois (via TranscodedWallpaper).");
                    await ApplyLaterAsync(path);
                }
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"MainForm: erro ao preparar wallpaper {item.Name}: {ex.Message}");
                MessageBox.Show($"Erro ao preparar wallpaper: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _ui.ToggleControls(true);
                _ui.SetStatus($"Catálogo carregado ({_catalog.Count} imagens)");
            }
        }

        private async Task ApplyWallpaperAsync(string path)
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

        private async Task ApplyLaterAsync(string path)
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

        private void ToggleHamburger()
        {
            _hamburgerOpen = !_hamburgerOpen;
            if (_hamburgerOpen)
            {
                panelHamburguer.Visible = true;
            }
            _slideTimer.Start();
        }

        private void AnimateHamburger()
        {
            int targetX = _hamburgerOpen ? 0 : -240;
            int distance = targetX - panelHamburguer.Left;
            int step = (int)(distance * 0.25);
            if (Math.Abs(step) < 2)
                step = Math.Sign(distance) * 2;

            panelHamburguer.Left += step;
            if (Math.Abs(panelHamburguer.Left - targetX) <= 1)
            {
                panelHamburguer.Left = targetX;
                if (!_hamburgerOpen)
                    panelHamburguer.Visible = false;
                _slideTimer.Stop();
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            _viewCts?.Cancel();
            _viewCts?.Dispose();
            _thumbnailService.Dispose();
            _imageCache.Dispose();
            _transformer.Dispose();
            _env.CleanupCache();
            _startup.Show();
        }
    }
}
