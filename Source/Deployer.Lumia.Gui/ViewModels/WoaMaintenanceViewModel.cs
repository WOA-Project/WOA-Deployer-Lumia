using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Lumia.Gui.Properties;
using Deployer.Tasks;
using Deployer.UI;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class WoaMaintenanceViewModel : ReactiveObject, IBusy
    {
        private readonly IWindowsDeployer deployer;
        private readonly UIServices uiServices;
        private readonly IDeploymentContext context;
        private readonly IOperationProgress progress;
        private readonly ILumiaSettingsService lumiaSettingsService;

        public WoaMaintenanceViewModel(IWindowsDeployer deployer,
            UIServices uiServices,
            IDeploymentContext context, 
            IOperationProgress progress,
            ILumiaSettingsService lumiaSettingsService)
        {
            this.deployer = deployer;
            this.uiServices = uiServices;
            this.context = context;
            this.progress = progress;
            this.lumiaSettingsService = lumiaSettingsService;

            BackupProgressViewModel = new ProgressViewModel(ReactiveCommand.CreateFromTask(Backup), progress);
            RestoreProgressViewModel = new ProgressViewModel(ReactiveCommand.CreateFromTask(Restore), progress);

            IsBusyObservable = Observable.Merge(new[]
            {
                BackupProgressViewModel.Command.IsExecuting,
                RestoreProgressViewModel.Command.IsExecuting,
            });
        }

        public ProgressViewModel RestoreProgressViewModel { get; set; }

        public ProgressViewModel BackupProgressViewModel { get; set; }

        public IObservable<bool> IsBusyObservable { get; }

        private async Task Backup()
        {
            uiServices.SaveFilePicker.DefaultExt = "*.wim";
            uiServices.SaveFilePicker.Filter = "Windows Images (.wim)|*.wim";
            var imagePath = uiServices.SaveFilePicker.PickFile();
            if (imagePath == null)
            {
                return;
            }

            context.DeploymentOptions = new WindowsDeploymentOptions
            {
                ImageIndex = 1,
                ImagePath = imagePath,
                UseCompact = lumiaSettingsService.UseCompactDeployment
            };

            await deployer.Backup(await context.Device.GetWindowsPartition(), imagePath, progress);

            await uiServices.ContextDialog.ShowAlert(this, Deployer.Lumia.Gui.Properties.Resources.Done, Deployer.Lumia.Gui.Properties.Resources.ImageCaptured);
        }

        private async Task Restore()
        {
            var filters = new List<(string, IEnumerable<string>)>
            {
                ("install.wim", new[]
                {
                    "install.wim"
                }),
                ("Windows Images", new[]
                {
                    "*.wim",
                    "*.esd"
                }),
                ("All files", new[]
                {
                    "*.*"
                })
            };

            var fileName = uiServices.OpenFilePicker.Pick(filters, () => lumiaSettingsService.WimFolder,
                x => { lumiaSettingsService.WimFolder = x; });

            if (fileName == null)
            {
                return;
            }

            context.DeploymentOptions = new WindowsDeploymentOptions
            {
                ImageIndex = 1,
                ImagePath = fileName,
                UseCompact = lumiaSettingsService.UseCompactDeployment
            };

            await context.DiskLayoutPreparer.Prepare(await context.Device.GetDeviceDisk());
            await deployer.Deploy(context.DeploymentOptions, context.Device, progress);

            await uiServices.ContextDialog.ShowAlert(this, Resources.Done, Resources.ImageRestored);
        }
    }
}