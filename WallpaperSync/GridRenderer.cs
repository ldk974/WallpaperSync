using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WallpaperSync
{
    public class GridRenderer
    {
        private readonly FlowLayoutPanel flp;
        private readonly ThumbnailService thumbService;
        private readonly Func<ImageEntry, Task> onClick;

        public GridRenderer(FlowLayoutPanel flpPanel, ThumbnailService thumbService, Func<ImageEntry, Task> onClickCallback)
        {
            this.flp = flpPanel ?? throw new ArgumentNullException(nameof(flpPanel));
            this.thumbService = thumbService ?? throw new ArgumentNullException(nameof(thumbService));
            this.onClick = onClickCallback ?? throw new ArgumentNullException(nameof(onClickCallback));
        }

        public async Task RenderAsync(System.Collections.Generic.List<ImageEntry> images, Label lblStatus)
        {
            flp.SuspendLayout();
            flp.Controls.Clear();

            int total = images.Count;
            int done = 0;
            var tasks = images.Select(async entry =>
            {
                var panel = CreateThumbnailCardPlaceholder(entry);
                flp.InvokeIfRequired(() => flp.Controls.Add(panel));

                try
                {
                    var thumbPath = await thumbService.GetOrCreateThumbPathAsync(entry);
                    var img = thumbService.LoadThumbIntoMemory(thumbPath);
                    var pic = panel.Controls.OfType<PictureBox>().FirstOrDefault();
                    if (pic != null)
                    {
                        pic.InvokeIfRequired(() =>
                        {
                            pic.Image?.Dispose();
                            pic.Image = new Bitmap(img);
                        });
                    }
                    img.Dispose();
                }
                catch (Exception ex)
                {
                    DebugLogger.Log($"GridRenderer: erro ao carregar thumb: {ex.Message}");
                }
                finally
                {
                    System.Threading.Interlocked.Increment(ref done);
                    var s = $"Carregando thumbnails... {done}/{total}";
                    lblStatus.InvokeIfRequired(() => lblStatus.Text = s);
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            lblStatus.InvokeIfRequired(() => lblStatus.Text = $"Catálogo carregado ({images.Count} imagens)");
            flp.ResumeLayout();
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
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lbl = new Label
            {
                Text = entry.Name,
                AutoEllipsis = true,
                Width = 150,
                Height = 40,
                Top = pic.Bottom + 4,
                Left = 0,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
            };

            panel.Controls.Add(pic);
            panel.Controls.Add(lbl);

            pic.Click += async (s, e) => await onClick(entry);
            lbl.Click += async (s, e) => await onClick(entry);

            return panel;
        }
    }
}