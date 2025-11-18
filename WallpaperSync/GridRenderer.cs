using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace WallpaperSync
{
    public class GridRenderer : IDisposable
    {
        private readonly FlowLayoutPanel flp;
        private readonly ThumbnailService thumbService;
        private readonly Func<ImageEntry, Task> onClick;

        private CancellationTokenSource? renderCts;

        public GridRenderer(FlowLayoutPanel flpPanel, ThumbnailService thumbService, Func<ImageEntry, Task> onClickCallback)
        {
            this.flp = flpPanel ?? throw new ArgumentNullException(nameof(flpPanel));
            this.thumbService = thumbService ?? throw new ArgumentNullException(nameof(thumbService));
            this.onClick = onClickCallback ?? throw new ArgumentNullException(nameof(onClickCallback));
        }

        public async Task RenderAsync(List<ImageEntry> images, Label lblStatus)
        {
            // cancela render anterior, se existir
            renderCts?.Cancel();
            renderCts = new CancellationTokenSource();
            var ct = renderCts.Token;

            lblStatus.InvokeIfRequired(() => lblStatus.Text = "Carregando thumbnails...");

            try
            {
                // limpa UI no UI thread
                flp.InvokeIfRequired(() =>
                {
                    foreach (Control c in flp.Controls)
                    {
                        if (c is Panel p)
                        {
                            foreach (var pic in p.Controls.OfType<PictureBox>())
                                pic.Image?.Dispose();
                        }
                        c.Dispose();
                    }

                    flp.Controls.Clear();
                    flp.SuspendLayout();
                });

                // cria todos os placeholders no UI thread de uma vez
                Panel[] createdPanels = null!;
                flp.InvokeIfRequired(() =>
                {
                    createdPanels = images.Select(CreateThumbnailCardPlaceholder).ToArray();
                    flp.Controls.AddRange(createdPanels);
                    flp.ResumeLayout();
                });

                int total = images.Count;
                int done = 0;

                // throttle de status
                var statusUpdateTask = Task.Run(async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(120, ct);
                        int d = done;
                        lblStatus.InvokeIfRequired(() =>
                            lblStatus.Text = $"Carregando thumbnails... {d}/{total}");
                    }
                }, ct);

                // carrega thumbs em paralelo
                var loadTasks = images.Select(async (entry, idx) =>
                {
                    if (ct.IsCancellationRequested) return;

                    try
                    {
                        var thumbPath = await thumbService.GetOrCreateThumbPathAsync(entry);
                        if (!File.Exists(thumbPath)) return;

                        using var tmpImg = thumbService.LoadThumbIntoMemory(thumbPath);
                        var bmp = new Bitmap(tmpImg);

                        // atualizar UI
                        var panel = createdPanels[idx];
                        var pic = panel.Controls.OfType<PictureBox>().FirstOrDefault();
                        if (pic == null) return;

                        pic.InvokeIfRequired(() =>
                        {
                            pic.Image?.Dispose();
                            pic.Image = bmp;
                        });
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        DebugLogger.Log($"GridRenderer: erro ao carregar thumbnail: {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Increment(ref done);
                    }
                });

                await Task.WhenAll(loadTasks);

                // cancela status update
                renderCts.Cancel();

                lblStatus.InvokeIfRequired(() =>
                    lblStatus.Text = $"Catálogo carregado ({images.Count} imagens)");
            }
            catch (OperationCanceledException)
            {
            }
        }

        private Panel CreateThumbnailCardPlaceholder(ImageEntry entry)
        {
            var panel = new Panel
            {
                Width = 160,
                Height = 140,
                Margin = new Padding(8),
                Tag = entry
            };

            var pic = new PictureBox
            {
                Width = 150,
                Height = 90,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(0x2F, 0x31, 0x36),
                Cursor = Cursors.Hand,
                Left = 5,
                Top = 5
            };

            var lbl = new Label
            {
                Text = entry.Name,
                AutoEllipsis = true,
                Width = 150,
                Height = 40,
                Top = pic.Bottom + 4,
                Left = 5,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            pic.Click += async (s, e) => await onClick(entry);
            lbl.Click += async (s, e) => await onClick(entry);

            panel.Controls.Add(pic);
            panel.Controls.Add(lbl);

            return panel;
        }

        public void Dispose()
        {
            renderCts?.Cancel();
            renderCts?.Dispose();
        }
    }
}
