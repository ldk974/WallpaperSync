using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using WallpaperSync.Infrastructure.Logging;

namespace WallpaperSync.Infrastructure.Services
{
    public sealed class WallpaperTransformer : IDisposable
    {
        private bool _disposed;

        public Bitmap LoadUnlocked(string path)
        {
            CoreLogger.Log($"Carregando bitmap (acesso desbloqueado) de: {path}", LogLevel.Info);

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var ms = new MemoryStream();

                fs.CopyTo(ms);
                ms.Position = 0;

                CoreLogger.Log($"Imagem carregada em memória. Tamanho: {ms.Length} bytes", LogLevel.Debug);

                return new Bitmap(ms);
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"Falha ao carregar imagem {path}: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public Bitmap EnsureAspect(Bitmap input)
        {
            CoreLogger.Log($"EnsureAspect: imagem {input.Width}x{input.Height}, ajustando para 16:9", LogLevel.Info);

            const double targetRatio = 16d / 9d;
            var ratio = (double)input.Width / input.Height;

            CoreLogger.Log($"Proporção atual: {ratio:0.000}", LogLevel.Debug);

            if (Math.Abs(ratio - targetRatio) <= 0.015)
            {
                CoreLogger.Log("Proporção já dentro da tolerância. Nenhum crop necessário.", LogLevel.Debug);
                return new Bitmap(input);
            }

            int newWidth = input.Width;
            int newHeight = (int)(input.Width / targetRatio);

            if (newHeight > input.Height)
            {
                newHeight = input.Height;
                newWidth = (int)(input.Height * targetRatio);
            }

            int x = Math.Max(0, (input.Width - newWidth) / 2);
            int y = Math.Max(0, (input.Height - newHeight) / 2);

            CoreLogger.Log($"Crop para {newWidth}x{newHeight} (x={x}, y={y})", LogLevel.Debug);

            var cropRect = new Rectangle(x, y, newWidth, newHeight);
            var result = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(result);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(input, new Rectangle(0, 0, newWidth, newHeight), cropRect, GraphicsUnit.Pixel);

            CoreLogger.Log("EnsureAspect concluído com sucesso.", LogLevel.Debug);

            return result;
        }

        public Bitmap ResizeIfNeeded(Bitmap input, int maxWidth = 3840, int maxHeight = 2160)
        {
            CoreLogger.Log($"ResizeIfNeeded: imagem {input.Width}x{input.Height}", LogLevel.Info);

            if (input.Width <= maxWidth && input.Height <= maxHeight)
            {
                CoreLogger.Log("Nenhum resize necessário (dentro dos limites).", LogLevel.Debug);
                return new Bitmap(input);
            }

            double ratio = Math.Min((double)maxWidth / input.Width, (double)maxHeight / input.Height);

            CoreLogger.Log($"Redimensionamento necessário. Ratio={ratio:0.000}", LogLevel.Debug);

            int newWidth = Math.Max(1, (int)Math.Round(input.Width * ratio));
            int newHeight = Math.Max(1, (int)Math.Round(input.Height * ratio));

            CoreLogger.Log($"Novo tamanho: {newWidth}x{newHeight}", LogLevel.Debug);

            var resized = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(resized);
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(input, 0, 0, newWidth, newHeight);

            CoreLogger.Log("Resize concluído com sucesso.", LogLevel.Debug);

            return resized;
        }

        public string SaveTemporaryJpeg(Bitmap bitmap)
        {
            var temp = Path.Combine(Path.GetTempPath(), $"wallpaper_{Guid.NewGuid():N}.jpg");

            CoreLogger.Log($"Salvando JPEG temporário em: {temp}", LogLevel.Info);
            CoreLogger.Log($"Dimensões: {bitmap.Width}x{bitmap.Height}", LogLevel.Debug);
            try
            {
                var codec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");

                if (codec != null)
                {
                    CoreLogger.Log("Encoder JPEG encontrado. Qualidade 90.", LogLevel.Debug);

                    using var eps = new EncoderParameters(1);
                    eps.Param[0] = new EncoderParameter(Encoder.Quality, 90L);
                    bitmap.Save(temp, codec, eps);
                }
                else
                {
                    CoreLogger.Log("Encoder JPEG não encontrado. Usando fallback ImageFormat.Jpeg.", LogLevel.Warning);
                    bitmap.Save(temp, ImageFormat.Jpeg);
                }

                CoreLogger.Log("Arquivo temporário salvo com sucesso.", LogLevel.Debug);
                return temp;
            }
            catch (Exception ex)
            {
                CoreLogger.Log($"Falha ao salvar JPEG temporário: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            CoreLogger.Log("Liberando WallpaperTransformer.", LogLevel.Debug);

            _disposed = true;

            CoreLogger.Log("WallpaperTransformer finalizado.", LogLevel.Debug);
        }
    }
}

