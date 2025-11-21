using Microsoft.Win32;
using System.Drawing;
using System.Windows.Forms;

namespace WallpaperSync.UI.Components
{
    public static class ThemeManager
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

        public static void ApplyTheme(Form form)
        {
            bool dark = IsDarkMode();

            form.BackColor = dark ? Color.FromArgb(28, 32, 40) : Color.FromArgb(239, 244, 249);
            SetControlsBackColor(form.Controls, dark);
        }

        private static void SetControlsBackColor(Control.ControlCollection controls, bool dark)
        {
            foreach (Control ctrl in controls)
            {
                switch (ctrl)
                {
                    case Label:
                    case CheckBox:
                    case ListBox:
                    case ListView:
                    case FlowLayoutPanel:
                    case Panel:
                        if (dark)
                        {
                            ctrl.BackColor =  Color.FromArgb(28, 32, 40);
                            ctrl.ForeColor = Color.White;
                        }
                        else
                        {
                            ctrl.BackColor = Color.FromArgb(239, 244, 249);
                            ctrl.ForeColor = Color.Black;
                        }
                        break;
                    default:
                        ctrl.ForeColor = Color.Black;
                        break;
                }

                if (ctrl.HasChildren)
                    SetControlsBackColor(ctrl.Controls, dark);
            }
        }
    }
}
