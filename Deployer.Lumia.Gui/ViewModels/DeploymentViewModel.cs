using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Gui.Core;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class DeploymentViewModel : ReactiveObject
    {
        private readonly IDeploymentTasks deploymentTasks;
        private readonly WimPickViewModel wimPickViewModel;

        public DeploymentViewModel(IDeploymentTasks deploymentTasks, UIServices uiServices, WimPickViewModel wimPickViewModel)
        {
            this.deploymentTasks = deploymentTasks;
            this.wimPickViewModel = wimPickViewModel;

            var isSelectedWim = wimPickViewModel.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployUefiAndWindows, isSelectedWim), uiServices.DialogService);            
        }

        private async Task DeployUefiAndWindows()
        {
            await deploymentTasks.Deploy(wimPickViewModel.WimMetadata.Path);
        }

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
    }
}