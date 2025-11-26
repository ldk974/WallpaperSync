using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WallpaperSync.Infrastructure.Environment;

namespace WallpaperSync.Infrastructure.Services
{
    public partial class SettingsManager
    {
        private static readonly string SettingsPath =
            Path.Combine(AppEnvironment.Instance.AppDataRoot, "settings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
            {
                // Criar padrão
                var defaultSettings = new AppSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json);
        }

        public static void Save(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}
