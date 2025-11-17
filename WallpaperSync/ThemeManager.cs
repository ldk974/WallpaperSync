using System.Drawing;
using System.Windows.Forms;

namespace WallpaperSync
{
    public static class ThemeManager
    {
        public static event Action<bool> OnThemeChanged;
        public static bool IsDarkModeEnabled() => SystemTheme.IsDarkMode();

        public static void ApplyTheme(Form form)
        {
            bool dark = SystemTheme.IsDarkMode();
            bool acrylicSupported = AcrylicEffect.IsAcrylicSupported() && SystemTransparency.IsTransparencyEnabled();

            form.BackColor = dark ? (acrylicSupported ? Color.Black : Color.FromArgb(32, 32, 32)) : Color.FromArgb(239, 244, 249);

            SetControlsBackColor(form.Controls, dark, acrylicSupported);
        }

        private static void SetControlsBackColor(Control.ControlCollection controls, bool dark, bool acrylicEnabled)
        {
            foreach (Control ctrl in controls)
            {
                switch (ctrl)
                {
                    case ListBox:
                    case FlowLayoutPanel:
                    case Panel:
                        if (dark)
                        {
                            ctrl.BackColor = acrylicEnabled ? Color.Black : Color.FromArgb(32, 32, 32);
                            ctrl.ForeColor = Color.White;
                        }
                        else
                        {
                            ctrl.BackColor = Color.FromArgb(239, 244, 249);
                            ctrl.ForeColor = Color.Black;
                        }
                        break;
                    default:
                        ctrl.BackColor = dark ? (acrylicEnabled ? Color.Black : Color.FromArgb(32, 32, 32)) : Color.FromArgb(239, 244, 249);
                        ctrl.ForeColor = dark ? Color.White : Color.Black;
                        break;
                }

                if (ctrl.HasChildren)
                    SetControlsBackColor(ctrl.Controls, dark, acrylicEnabled);
            }
        }
    }
}
