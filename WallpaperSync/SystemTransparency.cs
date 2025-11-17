using Microsoft.Win32;

namespace WallpaperSync
{
    public static class SystemTransparency
    {
        public static bool IsTransparencyEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                if (key == null)
                    return false;

                var value = key.GetValue("EnableTransparency");
                if (value is int intVal)
                    return intVal == 1;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
