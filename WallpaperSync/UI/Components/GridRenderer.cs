using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperSync.Domain.Models;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.UI.Extensions;

namespace WallpaperSync.UI.Components
{
    public sealed class GridRenderer : IDisposable
    {
        private readonly FlowLayoutPanel _panel;
        private readonly ThumbnailService _thumbnails;
        private readonly Func<WallpaperItem, Task> _onClick;

        private IReadOnlyList<WallpaperItem> _allItems = Array.Empty<WallpaperItem>();

        public int PageSize { get; set; } = 12;
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages => (_allItems.Count + PageSize - 1) / PageSize;

        public GridRenderer(
            FlowLayoutPanel panel,
            ThumbnailService thumbnails,
            Func<WallpaperItem, Task> onClick,
            Label pageLabel = null)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            _onClick = onClick ?? throw new ArgumentNullException(nameof(onClick));

        }

        public async Task SetItemsAsync(
            IReadOnlyList<WallpaperItem> items,
            Label statusLabel, Label pageLabel,
            CancellationToken token)
        {
            _allItems = items ?? Array.Empty<WallpaperItem>();
            CurrentPage = 1;

            await RenderPageAsync(statusLabel, pageLabel, token);
        }

        public async Task RenderPageAsync(Label statusLabel, Label pageLabel, CancellationToken token)
        {
            if (_allItems.Count == 0)
                return;

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            var pageItems = _allItems
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            await RenderAsync(pageItems, statusLabel, pageLabel, token);
        }

        public async Task NextPageAsync(Label statusLabel, Label pageLabel, CancellationToken token)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await RenderPageAsync(statusLabel, pageLabel, token);
            }
        }

        public async Task PreviousPageAsync(Label statusLabel, Label pageLabel, CancellationToken token)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await RenderPageAsync(statusLabel, pageLabel, token);
            }
        }

        public async Task RenderAsync(IReadOnlyList<WallpaperItem> items, Label statusLabel, Label pageLabel, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            statusLabel.InvokeIfRequired(() =>
                statusLabel.Text = "Carregando thumbnails...");

            // remove tudo sem flicker
            _panel.InvokeIfRequired(() =>
            {
                _panel.SuspendLayout();
                _panel.Visible = false;

                foreach (Control c in _panel.Controls)
                {
                    if (c is Panel p)
                    {
                        foreach (var pic in p.Controls.OfType<PictureBox>())
                            pic.Image?.Dispose();
                    }
                    c.Dispose();
                }

                _panel.Controls.Clear();
                _panel.ResumeLayout();
            });

            // placeholders fixos
            var placeholders = items.Select(CreatePlaceholder).ToArray();

            _panel.InvokeIfRequired(() =>
            {
                _panel.SuspendLayout();
                _panel.Controls.AddRange(placeholders);
                _panel.ResumeLayout();
                _panel.Visible = true;
            });

            int completed = 0;
            int total = items.Count;

            var loadTasks = items.Select((item, index) =>
                LoadThumbnailAsync(item, placeholders[index], () =>
                {
                    Interlocked.Increment(ref completed);
                    statusLabel.InvokeIfRequired(() =>
                        statusLabel.Text = $"Carregando thumbnails... {completed}/{total}");
                }, token)
            );

            try
            {
                await Task.WhenAll(loadTasks);

                statusLabel.InvokeIfRequired(() =>
                    statusLabel.Text = $"Catálogo carregado ({items.Count} imagens)");
                    pageLabel.Text = $"Página {CurrentPage} de {TotalPages}";
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task LoadThumbnailAsync(WallpaperItem item, Panel placeholder, Action reportProgress, CancellationToken token)
        {
            try
            {
                var path = await _thumbnails.GetOrCreateAsync(item, token: token).ConfigureAwait(false);
                if (!File.Exists(path))
                    return;

                using var image = new Bitmap(path);
                var clone = new Bitmap(image);
                var applied = false;

                placeholder.InvokeIfRequired(() =>
                {
                    if (placeholder.IsDisposed) return;

                    _panel.SuspendLayout();

                    if (placeholder.Controls.OfType<PictureBox>().FirstOrDefault() is PictureBox pb)
                    {
                        pb.Image?.Dispose();
                        pb.Image = clone;
                        applied = true;
                    }

                    if (placeholder.Controls.OfType<Label>().FirstOrDefault() is Label lbl)
                        lbl.Text = item.Name;

                    _panel.ResumeLayout(false);
                });

                if (!applied)
                    clone.Dispose();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                CoreLogger.Log($"GridRenderer: erro na thumbnail: {ex.Message}");
            }
            finally
            {
                reportProgress();
            }
        }

        private Panel CreatePlaceholder(WallpaperItem item)
        {
            var panel = new Panel
            {
                Width = 160,
                Height = 118,
                Margin = new Padding(5),
                Tag = item
            };

            var picture = new PictureBox
            {
                Width = 150,
                Height = 85,
                Left = 5,
                Top = 5,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(25, 27, 30),
                Cursor = Cursors.Hand
            };

            var label = new Label
            {
                Left = 5,
                Top = picture.Bottom + 4,
                Width = 150,
                Height = 40,
                AutoEllipsis = true,
                Text = item.Name,
                Font = new Font("Segoe UI", 9f)
            };;

            picture.Click += async (_, __) => await _onClick(item);
            label.Click += async (_, __) => await _onClick(item);


            panel.Controls.Add(picture);
            panel.Controls.Add(label);
            return panel;
        }

        public void Dispose() { }
    }
}
