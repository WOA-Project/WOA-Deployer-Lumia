using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Gui;
using Deployer.Lumia.Gui.Properties;
using Deployer.Tasks;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Attributes;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    [Metadata("Name", "Advanced")]
    [Metadata("Order", 2)]
    public class AdvancedViewModel : ReactiveObject, ISection, IDisposable
    {
        private readonly IDeploymentContext context;
        private readonly IWindowsDeployer deployer;
        private readonly IDisposable preparerUpdater;
        private readonly IOperationProgress progress;
        private readonly ISettingsService settingsService;
        private readonly UIServices uiServices;

        private DiskLayoutPreparerViewModel selectedPreparer;

        public AdvancedViewModel(ISettingsService settingsService, IFileSystemOperations fileSystemOperations,
            UIServices uiServices, IDeploymentContext context,
            IEnumerable<Meta<IDiskLayoutPreparer>> diskPreparers,
            IWindowsDeployer deployer,
            IOperationProgress progress)
        {
            this.settingsService = settingsService;
            this.uiServices = uiServices;
            this.context = context;
            this.deployer = deployer;
            this.progress = progress;

            DiskPreparers = diskPreparers
                .Where(x => !x.Metadata.Keys.Contains("IsNull"))
                .Select(x => new DiskLayoutPreparerViewModel((string) x.Metadata["Name"], x.Value))
                .ToList();

            DeleteDownloadedWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(() => DeleteDownloaded(fileSystemOperations)), uiServices.Dialog);
            ForceDualBootWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(ForceDualBoot),
                uiServices.Dialog);
            ForceSingleBootWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(ForceDisableDualBoot), uiServices.Dialog);

            BackupCommandWrapper =
                new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Backup), uiServices.Dialog);
            RestoreCommandWrapper =
                new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Restore), uiServices.Dialog);

            IsBusyObservable = Observable.Merge(DeleteDownloadedWrapper.Command.IsExecuting,
                BackupCommandWrapper.Command.IsExecuting, RestoreCommandWrapper.Command.IsExecuting,
                ForceDualBootWrapper.Command.IsExecuting, ForceSingleBootWrapper.Command.IsExecuting);

            preparerUpdater = this.WhenAnyValue(x => x.SelectedPreparer)
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    context.DiskLayoutPreparer = x.Preparer;
                    settingsService.DiskPreparer = x.Name;
                });

            SelectedPreparer = DiskPreparers.First(x => x.Name == settingsService.DiskPreparer);
        }

        public DiskLayoutPreparerViewModel SelectedPreparer
        {
            get => selectedPreparer;
            set => this.RaiseAndSetIfChanged(ref selectedPreparer, value);
        }

        public CommandWrapper<Unit, Unit> RestoreCommandWrapper { get; set; }

        public CommandWrapper<Unit, Unit> BackupCommandWrapper { get; set; }

        public CommandWrapper<Unit, Unit> DeleteDownloadedWrapper { get; }

        public bool UseCompactDeployment
        {
            get => settingsService.UseCompactDeployment;
            set
            {
                settingsService.UseCompactDeployment = value;
                this.RaisePropertyChanged(nameof(UseCompactDeployment));
            }
        }

        public bool CleanDownloadedBeforeDeployment
        {
            get => settingsService.CleanDownloadedBeforeDeployment;
            set
            {
                settingsService.CleanDownloadedBeforeDeployment = value;
                this.RaisePropertyChanged(nameof(CleanDownloadedBeforeDeployment));
            }
        }

        public CommandWrapper<Unit, Unit> ForceDualBootWrapper { get; }

        public CommandWrapper<Unit, Unit> ForceSingleBootWrapper { get; }

        public IEnumerable<DiskLayoutPreparerViewModel> DiskPreparers { get; set; }

        public void Dispose()
        {
            preparerUpdater?.Dispose();
        }

        public IObservable<bool> IsBusyObservable { get; }

        private async Task ForceDualBoot()
        {
            await ((IPhone) context.Device).ToogleDualBoot(true, true);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.DualBootEnabled);
        }

        private async Task ForceDisableDualBoot()
        {
            await ((IPhone) context.Device).ToogleDualBoot(false, true);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.DualBootDisabled);
        }

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
                UseCompact = settingsService.UseCompactDeployment
            };

            await deployer.Backup(await context.Device.GetWindowsVolume(), imagePath, progress);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.ImageCaptured);
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

            var fileName = uiServices.OpenFilePicker.Pick(filters, () => settingsService.WimFolder,
                x => { settingsService.WimFolder = x; });

            if (fileName == null)
            {
                return;
            }

            context.DeploymentOptions = new WindowsDeploymentOptions
            {
                ImageIndex = 1,
                ImagePath = fileName,
                UseCompact = settingsService.UseCompactDeployment
            };

            await context.DiskLayoutPreparer.Prepare(await context.Device.GetDeviceDisk());
            await deployer.Deploy(context.DeploymentOptions, context.Device, progress);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.ImageRestored);
        }

        private async Task DeleteDownloaded(IFileSystemOperations fileSystemOperations)
        {
            if (fileSystemOperations.DirectoryExists(AppPaths.DownloadedFolderName))
            {
                await fileSystemOperations.DeleteDirectory(AppPaths.DownloadedFolderName);
                await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.DownloadedFolderDeleted);
            }
            else
            {
                await uiServices.Dialog.ShowAlert(this, Resources.DownloadedFolderNotFoundTitle,
                    Resources.DownloadedFolderNotFound);
            }
        }
    }
}