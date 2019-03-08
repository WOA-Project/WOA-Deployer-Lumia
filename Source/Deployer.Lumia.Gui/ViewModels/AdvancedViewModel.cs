using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Gui;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class AdvancedViewModel : ReactiveObject, IBusy
    {
        private const string DownloadedFolderName = "Downloaded";
        private readonly ISettingsService settingsService;

        private readonly ObservableAsPropertyHelper<ByteSize> sizeReservedForWindows;

        public AdvancedViewModel(ISettingsService settingsService, IFileSystemOperations fileSystemOperations,
            UIServices uiServices)
        {
            this.settingsService = settingsService;

            sizeReservedForWindows =
                this.WhenAnyValue(x => x.GbsReservedForWindows, ByteSize.FromGigaBytes)
                    .ToProperty(this, x => x.SizeReservedForWindows);

            var deleteCommand = ReactiveCommand.CreateFromTask(() => DeleteDownloaded(fileSystemOperations, uiServices));
            DeleteDownloadedWrapper = new CommandWrapper<Unit, Unit>(this, deleteCommand, uiServices.Dialog);

            IsBusyObservable = Observable.Merge(deleteCommand.IsExecuting);
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

        public IObservable<bool> IsBusyObservable { get; }

        private async Task DeleteDownloaded(IFileSystemOperations fileSystemOperations, UIServices uiServices)
        {
            if (fileSystemOperations.DirectoryExists(DownloadedFolderName))
            {
                await fileSystemOperations.DeleteDirectory(DownloadedFolderName);
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