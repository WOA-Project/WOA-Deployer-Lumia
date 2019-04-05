using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Gui;
using Deployer.Gui.ViewModels;
using Deployer.Tasks;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class AdvancedViewModel : ReactiveObject, IBusy
    {
        public StatusViewModel StatusViewModel { get; }
        private readonly ISettingsService settingsService;
        private readonly UIServices uiServices;
        private readonly IProviderBasedWindowsDeployer deployer;
        private readonly IDiskLayoutPreparer preparer;
        private readonly IWindowsOptionsProvider optionsProvider;
        private readonly IDeviceProvider deviceProvider;
        private readonly IDownloadProgress progress;
        private readonly IPhone phone;

        private readonly ObservableAsPropertyHelper<ByteSize> sizeReservedForWindows;

        public AdvancedViewModel(ISettingsService settingsService, IFileSystemOperations fileSystemOperations,
            UIServices uiServices, IProviderBasedWindowsDeployer deployer, 
            IDiskLayoutPreparer preparer,
            IWindowsOptionsProvider optionsProvider, IDeviceProvider deviceProvider, IDownloadProgress progress, StatusViewModel statusViewModel, IPhone phone)
        {
            StatusViewModel = statusViewModel;
            this.settingsService = settingsService;
            this.uiServices = uiServices;
            this.deployer = deployer;
            this.preparer = preparer;
            this.optionsProvider = optionsProvider;
            this.deviceProvider = deviceProvider;
            this.progress = progress;
            this.phone = phone;

            sizeReservedForWindows =
                this.WhenAnyValue(x => x.GbsReservedForWindows, ByteSize.FromGigaBytes)
                    .ToProperty(this, x => x.SizeReservedForWindows);

            DeleteDownloadedWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(() => DeleteDownloaded(fileSystemOperations)), uiServices.Dialog);
            ForceDualBootWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(ForceDualBoot), uiServices.Dialog);

            BackupCommandWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Backup), uiServices.Dialog);
            RestoreCommandWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(Restore), uiServices.Dialog);

            IsBusyObservable = Observable.Merge(new []
            {
                DeleteDownloadedWrapper.Command.IsExecuting,
                BackupCommandWrapper.Command.IsExecuting,
                RestoreCommandWrapper.Command.IsExecuting,
                ForceDualBootWrapper.Command.IsExecuting,
            });
        }

        private async Task ForceDualBoot()
        {
            await phone.ToogleDualBoot(true, true);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.DualBootEnabled);
        }

        public CommandWrapper<Unit, Unit> RestoreCommandWrapper { get; set; }

        public CommandWrapper<Unit, Unit> BackupCommandWrapper { get; set; }

        private async Task Backup()
        {
            uiServices.SaveFilePicker.DefaultExt = "*.wim";
            uiServices.SaveFilePicker.Filter = "Windows Images (.wim)|*.wim";
            var imagePath = uiServices.SaveFilePicker.PickFile();
            if (imagePath == null)
            {
                return;
            }

            optionsProvider.Options = new WindowsDeploymentOptions()
            {
                ImageIndex = 1,
                ImagePath = imagePath,
                SizeReservedForWindows = ByteSize.FromGigaBytes(settingsService.SizeReservedForWindows),
                UseCompact = settingsService.UseCompactDeployment,
            };

            await deployer.Capture(imagePath, progress);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.ImageCaptured);
        }

        private async Task Restore()
        {
            var filters = new List<(string, IEnumerable<string>)>
            {
                ("install.wim", new[]
                {
                    "install.wim",
                }),
                ("Windows Images", new[]
                {
                    "*.wim",
                    "*.esd"
                }),
                ("All files", new[]
                {
                    "*.*",
                }),
            };

            var fileName = uiServices.OpenFilePicker.Pick(filters, () => settingsService.WimFolder, x =>
            {
                settingsService.WimFolder = x;
                settingsService.Save();
            });

            if (fileName == null)
            {
                return;
            }

            optionsProvider.Options = new WindowsDeploymentOptions
            {
                ImageIndex = 1,
                ImagePath = fileName,
                SizeReservedForWindows = ByteSize.FromGigaBytes(settingsService.SizeReservedForWindows),
                UseCompact = settingsService.UseCompactDeployment,
            };

            await preparer.Prepare(await deviceProvider.Device.GetDeviceDisk());
            await deployer.Deploy(progress);

            await uiServices.Dialog.ShowAlert(this, Resources.Done, Resources.ImageRestored);
        }
        
        public CommandWrapper<Unit, Unit> DeleteDownloadedWrapper { get; }

        public ByteSize SizeReservedForWindows => sizeReservedForWindows.Value;

        public double GbsReservedForWindows
        {
            get => settingsService.SizeReservedForWindows;
            set
            {
                settingsService.SizeReservedForWindows = value;
                settingsService.Save();
                this.RaisePropertyChanged(nameof(GbsReservedForWindows));
            }
        }

        public bool UseCompactDeployment
        {
            get => settingsService.UseCompactDeployment;
            set
            {
                settingsService.UseCompactDeployment = value;
                settingsService.Save();
                this.RaisePropertyChanged(nameof(UseCompactDeployment));
            }
        }

        public bool CleanDownloadedBeforeDeployment
        {
            get => settingsService.CleanDownloadedBeforeDeployment;
            set
            {
                settingsService.CleanDownloadedBeforeDeployment = value;
                settingsService.Save();
                this.RaisePropertyChanged(nameof(CleanDownloadedBeforeDeployment));
            }
        }

        public IObservable<bool> IsBusyObservable { get; }

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

        public CommandWrapper<Unit, Unit> ForceDualBootWrapper { get; }
    }
}