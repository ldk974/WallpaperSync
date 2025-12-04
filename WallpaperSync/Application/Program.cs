using System.Globalization;
using WallpaperSync.Application.Startup;
using WallpaperSync.Infrastructure.Logging;
using WallpaperSync.Infrastructure.Services;
using WinFormsApplication = System.Windows.Forms.Application;

namespace WallpaperSync 
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

            ApplicationConfiguration.Initialize();
            WinFormsApplication.Run(new StartupForm());
        }
    }
}