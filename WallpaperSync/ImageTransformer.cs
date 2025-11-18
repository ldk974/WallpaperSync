using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace WallpaperSync
{
    public class ImageTransformer : IDisposable
    {
        private bool disposed = false;

        public Bitmap LoadBitmapUnlocked(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path inválido");

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }

        public static bool IsAspect16by9(Image img, double tolerance = 0.015)
        {
            if (img == null) return false;

            double ratio = (double)img.Width / img.Height;
            double target = 16.0 / 9.0;
            return Math.Abs(ratio - target) <= tolerance;
        }

        public Bitmap EnsureImageIs16by9(Bitmap img)
        {
            if (img == null)
                throw new ArgumentNullException(nameof(img));

            if (IsAspect16by9(img))
                return new Bitmap(img);

            double targetRatio = 16.0 / 9.0;
            int newW = img.Width;
            int newH = (int)(img.Width / targetRatio);

            if (newH > img.Height)
            {
                newH = img.Height;
                newW = (int)(img.Height * targetRatio);
            }

            int x = Math.Max(0, (img.Width - newW) / 2);
            int y = Math.Max(0, (img.Height - newH) / 2);

            Rectangle cropRect = new(x, y, newW, newH);

            var cropped = new Bitmap(newW, newH, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(img, new Rectangle(0, 0, newW, newH), cropRect, GraphicsUnit.Pixel);
            }

            return cropped;
        }

        public Bitmap ResizeIfNeeded(Bitmap img)
        {
            if (img == null)
                throw new ArgumentNullException(nameof(img));

            const int maxW = 3840;
            const int maxH = 2160;

            int w = img.Width;
            int h = img.Height;

            if (w <= maxW && h <= maxH)
                return new Bitmap(img);

            return ResizeToMax(img, maxW, maxH);
        }

        public Bitmap ResizeToMax(Bitmap img, int maxW, int maxH)
        {
            if (img == null)
                throw new ArgumentNullException(nameof(img));

            double ratio = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);

            int newW = Math.Max(1, (int)Math.Round(img.Width * ratio));
            int newH = Math.Max(1, (int)Math.Round(img.Height * ratio));

            var resized = new Bitmap(newW, newH, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(resized))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(img, 0, 0, newW, newH);
            }

            return resized;
        }

        public string SaveBitmapToTempJpeg(Bitmap bmp)
        {
            if (bmp == null)
                throw new ArgumentNullException(nameof(bmp));

            string dir = Path.GetTempPath();
            Directory.CreateDirectory(dir);

            string tempPath = Path.Combine(dir, $"wallpaper_{Guid.NewGuid():N}.jpg");

            try
            {
                ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
                    .FirstOrDefault(c => c.MimeType == "image/jpeg");

                if (jpegCodec != null)
                {
                    using var eps = new EncoderParameters(1);
                    eps.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                    bmp.Save(tempPath, jpegCodec, eps);
                }
                else
                {
                    bmp.Save(tempPath, ImageFormat.Jpeg);
                }
            }
            catch
            {
                // fallback evita perder imagem em caso de codec quebrado
                bmp.Save(tempPath, ImageFormat.Jpeg);
            }

            return tempPath;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
        }
    }
}
