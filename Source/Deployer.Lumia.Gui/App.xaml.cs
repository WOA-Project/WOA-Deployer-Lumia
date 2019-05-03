using System.Linq;
using System.Windows;
using Deployer.Gui;
using Deployer.Lumia.Gui.Views;

namespace Deployer.Lumia.Gui
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MahApps.Metro.ThemeManager.IsAutomaticWindowsAppModeSettingSyncEnabled = true;
            MahApps.Metro.ThemeManager.SyncThemeWithWindowsAppModeSetting();

            UpdateChecker.CheckForUpdates(AppProperties.GitHubBaseUrl);
            Current.ShutdownMode = ShutdownMode.OnLastWindowClose;

            if (e.Args.Any())
            {
                LaunchConsole(e.Args);
            }
            else
            {
                LaunchGui();
            }
        }

        private void LaunchGui()
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();            
        }

        private void LaunchConsole(string[] args)
        {
            //ConsoleEmbedder.ExecuteInsideConsole(() => Task.Run(() => Program.Main(args)).Wait());
            //Shutdown();
        }
    }
}
