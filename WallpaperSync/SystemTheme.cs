using Microsoft.Win32;

namespace WallpaperSync
{
    public static class SystemTheme
    {
        // retorna true se Windows tiver no modo escuro.
        public static bool IsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    false
                );

                if (key == null) return false;

                var value = key.GetValue("AppsUseLightTheme");

                // se Windows ta em Dark Mode, AppsUseLightTheme = 0
                return value is int v && v == 0;
            }
            catch
            {
                // fallback pra claro
                return false;
            }
        }
    }
}
