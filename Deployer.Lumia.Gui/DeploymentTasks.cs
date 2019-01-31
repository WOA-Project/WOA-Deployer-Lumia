using System.Threading.Tasks;
using Deployer.Lumia.NetFx;
using Installer.Wpf.Core;

namespace Deployer.Lumia.Gui
{
    public class DeploymentTasks : IDeploymentTasks
    {
        private readonly DeploymentScriptRunner runner;
        private readonly ISettingsService settingsService;

        public DeploymentTasks(DeploymentScriptRunner runner, ISettingsService settingsService)
        {
            this.runner = runner;
            this.settingsService = settingsService;
        }

        public async Task Deploy(string wimPath)
        {
            await runner.ExecuteWindowsScript("950xl.txt", new WindowsDeploymentOptions()
            {
                Index = 1,
                WimImage = wimPath,
                ReservedSizeForWindowsInGb = settingsService.SizeReservedForWindows,
            });
        }
    }
}