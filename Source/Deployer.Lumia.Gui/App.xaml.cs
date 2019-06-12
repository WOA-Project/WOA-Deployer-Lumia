using System.Linq;
using System.Windows;
using Deployer.Lumia.Gui.Views;
using Deployer.NetFx;
using Deployer.UI;

namespace Deployer.Lumia.Gui
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MahApps.Metro.ThemeManager.IsAutomaticWindowsAppModeSettingSyncEnabled = true;
            MahApps.Metro.ThemeManager.SyncThemeWithWindowsAppModeSetting();

            if (!OS.IsCompatibleWindowsBuild)
            {
                MessageBox.Show(UI.Properties.Resources.IncompatibleWindows10Build, UI.Properties.Resources.IncompatibleWindows10BuildTitle);
                Current.Shutdown();
                return;
            }

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
            // Console is disable for now
            Shutdown();
        }
    }
}
