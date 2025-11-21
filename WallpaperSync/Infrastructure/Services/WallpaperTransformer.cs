using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class WallpaperTransformer : IDisposable
    {
        private bool _disposed;

        public Bitmap LoadUnlocked(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }

        public Bitmap EnsureAspect(Bitmap input)
        {
            const double targetRatio = 16d / 9d;
            var ratio = (double)input.Width / input.Height;

            if (Math.Abs(ratio - targetRatio) <= 0.015)
                return new Bitmap(input);

            int newWidth = input.Width;
            int newHeight = (int)(input.Width / targetRatio);

            if (newHeight > input.Height)
            {
                newHeight = input.Height;
                newWidth = (int)(input.Height * targetRatio);
            }

            int x = Math.Max(0, (input.Width - newWidth) / 2);
            int y = Math.Max(0, (input.Height - newHeight) / 2);

            var cropRect = new Rectangle(x, y, newWidth, newHeight);
            var result = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(result);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(input, new Rectangle(0, 0, newWidth, newHeight), cropRect, GraphicsUnit.Pixel);
            return result;
        }

        public Bitmap ResizeIfNeeded(Bitmap input, int maxWidth = 3840, int maxHeight = 2160)
        {
            if (input.Width <= maxWidth && input.Height <= maxHeight)
                return new Bitmap(input);

            double ratio = Math.Min((double)maxWidth / input.Width, (double)maxHeight / input.Height);

            int newWidth = Math.Max(1, (int)Math.Round(input.Width * ratio));
            int newHeight = Math.Max(1, (int)Math.Round(input.Height * ratio));

            var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(resized);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(input, 0, 0, newWidth, newHeight);
            return resized;
        }

        public string SaveTemporaryJpeg(Bitmap bitmap)
        {
            var temp = Path.Combine(Path.GetTempPath(), $"wallpaper_{Guid.NewGuid():N}.jpg");
            var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
            if (codec != null)
            {
                using var eps = new EncoderParameters(1);
                eps.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                bitmap.Save(temp, codec, eps);
            }
            else
            {
                bitmap.Save(temp, ImageFormat.Jpeg);
            }

            return temp;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}

