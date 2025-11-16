using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WallpaperSync
{
    public class ImageTransformer : IDisposable
    {
        private bool disposed = false;

        public Bitmap LoadBitmapUnlocked(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var tmp = Image.FromStream(fs, true, true);
            return new Bitmap(tmp);
        }

        public static bool IsAspect16by9(Image img, double tolerance = 0.015)
        {
            double ratio = (double)img.Width / img.Height;
            double target = 16.0 / 9.0;
            return Math.Abs(ratio - target) <= tolerance;
        }

        public Bitmap EnsureImageIs16by9(Bitmap img)
        {
            if (IsAspect16by9(img))
                return new Bitmap(img);

            int newWidth = img.Width;
            int newHeight = (int)(img.Width / (16.0 / 9.0));

            if (newHeight > img.Height)
            {
                newHeight = img.Height;
                newWidth = (int)(img.Height * (16.0 / 9.0));
            }

            int x = (img.Width - newWidth) / 2;
            int y = (img.Height - newHeight) / 2;

            var cropRect = new Rectangle(x, y, newWidth, newHeight);

            var cropped = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(cropped))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.DrawImage(img, new Rectangle(0, 0, newWidth, newHeight), cropRect, GraphicsUnit.Pixel);
            }

            return cropped;
        }
        public Bitmap ResizeIfNeeded(Bitmap img)
        {
            const int maxW = 3840;
            const int maxH = 2160;

            int w = img.Width;
            int h = img.Height;

            double mp = (w * h) / 1_000_000.0;

            if (w <= maxW && h <= maxH)
            {
                return new Bitmap(img);
            }

            if (mp <= 20.0)
            {
                double estimatedSizeMB = mp * 0.5;
                if (estimatedSizeMB < 20.0)
                    return new Bitmap(img);
            }

            return ResizeToMax(img, maxW, maxH);
        }
        public Bitmap ResizeToMax(Bitmap img, int maxW, int maxH)
        {
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
            string tempPath = Path.Combine(Path.GetTempPath(), $"wallpaper_{Guid.NewGuid():N}.jpg");

            var jpegCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
            if (jpegCodec != null)
            {
                var eps = new EncoderParameters(1);
                eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
                bmp.Save(tempPath, jpegCodec, eps);
            }
            else
            {
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
