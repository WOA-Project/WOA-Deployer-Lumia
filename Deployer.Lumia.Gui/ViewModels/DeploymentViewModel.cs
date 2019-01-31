using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Gui.Core;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class DeploymentViewModel : ReactiveObject
    {
        private readonly IAutoDeployer deploymentTasks;
        private readonly WimPickViewModel wimPickViewModel;

        public DeploymentViewModel(IAutoDeployer deploymentTasks, UIServices uiServices, WimPickViewModel wimPickViewModel)
        {
            this.deploymentTasks = deploymentTasks;
            this.wimPickViewModel = wimPickViewModel;

            var isSelectedWim = wimPickViewModel.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Deploy, isSelectedWim), uiServices.DialogService);            
        }

        private async Task Deploy()
        {
            var windowsDeploymentOptions = new WindowsDeploymentOptions
            {
                WimImage = wimPickViewModel.WimMetadata.Path,
                Index = 1,
                ReservedSizeForWindowsInGb = 18,
            };

            await deploymentTasks.Deploy(windowsDeploymentOptions);
        }

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
    }
}