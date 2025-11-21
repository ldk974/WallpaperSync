using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WallpaperSync.Infrastructure.Services;
using WallpaperSync.Infrastructure.SystemIntegration;
using WallpaperSync.UI.Components;
using WallpaperSync.UI.Extensions;
using BackupInfo = WallpaperSync.Infrastructure.Services.BackupService.BackupInfo;

namespace WallpaperSync.UI.Dialogs
{
    public partial class RestoreForm : Form
    {

        private readonly string transcodedPath;
        private readonly UndoManager undoManager;

        private CancellationTokenSource? thumbCts;
        public RestoreForm(UndoManager undoManager, string transcodedPath)
        {
            this.undoManager = undoManager ?? throw new ArgumentNullException(nameof(undoManager));
            this.transcodedPath = transcodedPath;

            InitializeComponent();

            Load += RestoreForm_Load;
            FormClosing += RestoreForm_Closing;

            typeof(ListView).GetProperty("DoubleBuffered",
                 System.Reflection.BindingFlags.Instance |
                 System.Reflection.BindingFlags.NonPublic)
                 .SetValue(lv, true, null);

        }
        private void Lv_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lv.SelectedItems.Count == 0)
            {
                previewBox.Image?.Dispose();
                previewBox.Image = null;
                lblInfo.Text = "";
                return;
            }

            var tag = lv.SelectedItems[0].Tag as BackupInfo;

            lblInfo.Text = $"{tag.FileName} — {FormatSize(tag.SizeBytes)} — {tag.Created}";

            // carrega async
            Task.Run(() =>
            {
                var img = GeneratePreview(tag.Path);

                this.InvokeIfRequired(() =>
                {
                    previewBox.Image?.Dispose();
                    previewBox.Image = img != null ? new Bitmap(img) : null;
                });
            });
        }

        private string FormatSize(long bytes)
        {
            if (bytes > 1024 * 1024) return $"{bytes / (1024 * 1024.0):0.##} MB";
            if (bytes > 1024) return $"{bytes / 1024.0:0.##} KB";
            return $"{bytes} B";
        }

        private void RestoreForm_Load(object sender, EventArgs e)
        {
            ThemeManager.ApplyTheme(this);
            RefreshList();
        }

        private void RefreshList()
        {
            // cancela load anterior, se existir
            thumbCts?.Cancel();
            thumbCts?.Dispose();
            thumbCts = new CancellationTokenSource();
            var token = thumbCts.Token;

            lv.Items.Clear();
            var backups = undoManager.GetBackups().ToArray();

            int idx = 0;
            foreach (var b in backups)
            {
                var li = new ListViewItem(new[] { b.FileName, b.Created.ToString("g"), FormatSize(b.SizeBytes) })
                {
                    Tag = b,
                    ImageIndex = idx
                };
                lv.Items.Add(li);
                idx++;
            }

            this.InvokeIfRequired(() =>
            {
                if (lv.SmallImageList == null)
                    lv.SmallImageList = new ImageList();

                lv.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;
                lv.SmallImageList.ImageSize = new Size(120, 70);
                lv.SmallImageList.Images.Clear();

                // coloca placeholders
                foreach (var b in backups)
                {
                    using var placeholder = new Bitmap(lv.SmallImageList.ImageSize.Width, lv.SmallImageList.ImageSize.Height);
                    using (var g = Graphics.FromImage(placeholder))
                    {
                        g.Clear(Color.Gray);
                        g.DrawString("…", SystemFonts.DefaultFont, Brushes.White, 40, 15);
                    }
                    lv.SmallImageList.Images.Add(new Bitmap(placeholder));
                }
            });

            Size imageSize = Size.Empty;
            int total = backups.Length;
            this.InvokeIfRequired(() =>
            {
                imageSize = lv.SmallImageList?.ImageSize ?? new Size(120, 70);
            });

            // gera no background
            Task.Run(() =>
            {
                for (int i = 0; i < total; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var b = backups[i];
                    try
                    {
                        // gera o smallthumb no tamanho final pra evitar reescalar
                        using var thumb = GenerateSmallThumb(b.Path, imageSize.Width, imageSize.Height);
                        if (thumb != null && !token.IsCancellationRequested)
                        {
                            var bmpCopy = new Bitmap(thumb);

                            this.InvokeIfRequired(() =>
                            {
                                if (this.IsDisposed || !this.IsHandleCreated)
                                {
                                    bmpCopy.Dispose();
                                    return;
                                }

                                if (lv.SmallImageList == null)
                                {
                                    lv.SmallImageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = imageSize };
                                }

                                if (i < lv.SmallImageList.Images.Count)
                                {
                                    // libera imagem antiga pra evitar leak
                                    try { var old = lv.SmallImageList.Images[i]; old?.Dispose(); } catch { }
                                    lv.SmallImageList.Images[i] = bmpCopy;
                                }
                                else
                                {
                                    lv.SmallImageList.Images.Add(bmpCopy);
                                }

                                if (i < lv.Items.Count)
                                {
                                    lv.Refresh();
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        if (token.IsCancellationRequested) break;
                    }
                }
            }, token);
        }

        private Image GenerateSmallThumb(string path, int targetW, int targetH)
        {
            try
            {
                using var img = Image.FromFile(path); // carrega sem travar UI
                var thumb = new Bitmap(targetW, targetH);
                using var g = Graphics.FromImage(thumb);
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                // calcula srcRect mantendo aspecto
                float srcRatio = (float)img.Width / img.Height;
                float dstRatio = (float)targetW / targetH;
                Rectangle srcRect;
                if (srcRatio > dstRatio)
                {
                    int newW = (int)(img.Height * dstRatio);
                    int x = (img.Width - newW) / 2;
                    srcRect = new Rectangle(x, 0, newW, img.Height);
                }
                else
                {
                    int newH = (int)(img.Width / dstRatio);
                    int y = (img.Height - newH) / 2;
                    srcRect = new Rectangle(0, y, img.Width, newH);
                }

                g.DrawImage(img, new Rectangle(0, 0, targetW, targetH), srcRect, GraphicsUnit.Pixel);
                return thumb;
            }
            catch
            {
                return null;
            }
        }

        private Image GeneratePreview(string path)
        {
            try
            {
                using var img = Image.FromFile(path);

                int w = 1024;  // maior qualidade
                int h = 576;

                var bmp = new Bitmap(w, h);
                using var g = Graphics.FromImage(bmp);

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.DrawImage(img, 0, 0, w, h);

                return bmp;
            }
            catch
            {
                return null;
            }
        }


        private async void BtnRestore_Click(object sender, EventArgs e)
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Selecione um backup."); return; }
            var b = (BackupInfo)lv.SelectedItems[0].Tag;

            var ok = MessageBox.Show($"Restaurar {b.FileName}?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes;
            if (!ok) return;

            if (undoManager.Restore(b.Path, transcodedPath))
            {
                var apiOk = WallpaperManager.SetWallpaper(transcodedPath);
                MessageBox.Show(apiOk ? "Wallpaper restaurado com sucesso." : "Restaurado mas API retornou falha.");
            }
            else
            {
                MessageBox.Show($"Falha ao restaurar:");
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Selecione um backup."); return; }
            var b = (BackupInfo)lv.SelectedItems[0].Tag;

            if (MessageBox.Show($"Excluir {b.FileName}?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            if (undoManager.Delete(b.Path, out var err))
            {
                MessageBox.Show("Backup excluído.");
                RefreshList();
            }
            else
            {
                MessageBox.Show($"Falha ao excluir: {err}");
            }
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            string folder = Path.GetDirectoryName(undoManager.GetBackups().FirstOrDefault()?.Path ?? transcodedPath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }

        private void BtnDeleteAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Excluir TODOS os backups? Esta ação não pode ser desfeita.", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            var count = undoManager.DeleteAll(out var err);
            if (!string.IsNullOrEmpty(err))
                MessageBox.Show($"Erro ao excluir: {err}");
            else
                MessageBox.Show($"Excluídos: {count} arquivos.");
            RefreshList();
        }

        private void BtnRestoreLast_Click(object sender, EventArgs e)
        {
            var last = undoManager.GetBackups().FirstOrDefault();
            if (last == null) { MessageBox.Show("Nenhum backup."); return; }

            if (undoManager.Restore(last.Path, transcodedPath))
            {
                var apiOk = WallpaperManager.SetWallpaper(transcodedPath);
                MessageBox.Show(apiOk ? "Wallpaper restaurado com sucesso." : "Restaurado mas API retornou falha.");
            }
            else
            {
                MessageBox.Show($"Falha ao restaurar:");
            }
        }

        private void RestoreForm_Closing(object sender, FormClosingEventArgs e)
        {
            thumbCts?.Cancel();
            thumbCts?.Dispose();
            thumbCts = null;
        }
    }
}