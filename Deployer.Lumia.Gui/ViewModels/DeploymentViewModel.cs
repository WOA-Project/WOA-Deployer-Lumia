using System;
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
        private readonly AdvancedViewModel advancedViewModel;
        private readonly WimPickViewModel wimPickViewModel;
        private readonly IObserver<double> progressObserver;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;

        public DeploymentViewModel(IAutoDeployer deploymentTasks, UIServices uiServices, AdvancedViewModel advancedViewModel, WimPickViewModel wimPickViewModel, IObserver<double> progressObserver)
        {
            this.deploymentTasks = deploymentTasks;
            this.advancedViewModel = advancedViewModel;
            this.wimPickViewModel = wimPickViewModel;
            this.progressObserver = progressObserver;

            var isSelectedWim = wimPickViewModel.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Deploy, isSelectedWim), uiServices.DialogService);            
            var isBusyObs = FullInstallWrapper.Command.IsExecuting;
            isBusyHelper = isBusyObs.ToProperty(this, model => model.IsBusy);
        }

        public bool IsBusy => isBusyHelper.Value;

        private async Task Deploy()
        {
            var windowsDeploymentOptions = new WindowsDeploymentOptions
            {
                WimImage = wimPickViewModel.WimMetadata.Path,
                Index = 1,
                ReservedSizeForWindowsInGb = advancedViewModel.SizeReservedForWindows,
            };

            await deploymentTasks.Deploy(windowsDeploymentOptions, progressObserver);
        }

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
    }
}