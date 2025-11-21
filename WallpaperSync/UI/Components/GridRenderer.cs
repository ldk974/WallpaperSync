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

        public GridRenderer(
            FlowLayoutPanel panel,
            ThumbnailService thumbnails,
            Func<WallpaperItem, Task> onClick)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
            _onClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
        }

        public async Task RenderAsync(IReadOnlyList<WallpaperItem> items, Label statusLabel, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            statusLabel.InvokeIfRequired(() => statusLabel.Text = "Carregando thumbnails...");

            _panel.InvokeIfRequired(() =>
            {
                foreach (Control c in _panel.Controls)
                {
                    if (c is Panel panel)
                    {
                        foreach (var pic in panel.Controls.OfType<PictureBox>())
                        {
                            pic.Image?.Dispose();
                        }
                    }

                    c.Dispose();
                }

                _panel.Controls.Clear();
                _panel.SuspendLayout();
            });

            var placeholders = items.Select(CreatePlaceholder).ToArray();
            _panel.InvokeIfRequired(() =>
            {
                _panel.Controls.AddRange(placeholders);
                _panel.ResumeLayout();
            });

            int completed = 0;
            int total = items.Count;

            var loadTasks = items.Select((item, index) => LoadThumbnailAsync(item, placeholders[index], () =>
            {
                Interlocked.Increment(ref completed);
                statusLabel.InvokeIfRequired(() =>
                    statusLabel.Text = $"Carregando thumbnails... {completed}/{total}");
            }, token));

            try
            {
                await Task.WhenAll(loadTasks);
                statusLabel.InvokeIfRequired(() =>
                    statusLabel.Text = $"Catálogo carregado ({items.Count} imagens)");
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

                try
                {
                    placeholder.InvokeIfRequired(() =>
                    {
                        if (placeholder.IsDisposed)
                            return;

                        if (placeholder.Controls.OfType<PictureBox>().FirstOrDefault() is PictureBox pb)
                        {
                            pb.Image?.Dispose();
                            pb.Image = clone;
                            applied = true;
                        }

                        if (placeholder.Controls.OfType<Label>().FirstOrDefault() is Label lbl)
                        {
                            lbl.Text = item.Name;
                        }
                    });
                }
                catch (ObjectDisposedException)
                {
                }

                if (!applied)
                {
                    clone.Dispose();
                }
            }
            catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
            {
                CoreLogger.Log($"Thumbnail não disponível ainda: {item.Name} ({ex.Message})");
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"GridRenderer thumbnail falhou: {ex.Message}");
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
                Height = 140,
                Margin = new Padding(8),
                BackColor = Color.FromArgb(32, 34, 38),
                Tag = item
            };

            var picture = new PictureBox
            {
                Width = 150,
                Height = 90,
                Left = 5,
                Top = 5,
                SizeMode = PictureBoxSizeMode.Zoom,
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
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f)
            };

            picture.Click += async (_, __) => await _onClick(item);
            label.Click += async (_, __) => await _onClick(item);

            panel.Controls.Add(picture);
            panel.Controls.Add(label);
            return panel;
        }

        public void Dispose()
        {
        }
    }
}
