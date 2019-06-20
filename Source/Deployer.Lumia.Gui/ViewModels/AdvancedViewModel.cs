using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Lumia.Gui.Properties;
using Deployer.Tasks;
using Deployer.UI;
using Grace.DependencyInjection;
using Grace.DependencyInjection.Attributes;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    [Metadata("Name", "Advanced")]
    [Metadata("Order", 3)]
    public class AdvancedViewModel : ReactiveObject, ISection, IDisposable
    {
        private const string LogsZipName = "PhoneLogs.zip";
        private readonly IDeploymentContext context;
        private readonly ILogCollector logCollector;
        private readonly IDisposable preparerUpdater;
        private readonly ILumiaSettingsService lumiaSettingsService;
        private readonly UIServices uiServices;

        private Meta<IDiskLayoutPreparer> selectedPreparer;

        public AdvancedViewModel(ILumiaSettingsService lumiaSettingsService, IFileSystemOperations fileSystemOperations,
            UIServices uiServices, IDeploymentContext context, IOperationContext operationContext, IOperationProgress progress,
            IList<Meta<IDiskLayoutPreparer>> diskPreparers,
            ILogCollector logCollector)
        {
            this.lumiaSettingsService = lumiaSettingsService;
            this.uiServices = uiServices;
            this.context = context;
            this.logCollector = logCollector;

            DiskPreparers = diskPreparers;

            DeleteDownloadedWrapper = new ProgressViewModel(ReactiveCommand.CreateFromTask(() => DeleteDownloaded(fileSystemOperations)), progress, this, uiServices.ContextDialog, operationContext);

            ForceDualBootWrapper = new ProgressViewModel(ReactiveCommand.CreateFromTask(ForceDualBoot), progress, this, uiServices.ContextDialog, operationContext);

            ForceSingleBootWrapper = new ProgressViewModel(ReactiveCommand.CreateFromTask(ForceDisableDualBoot), progress, this, uiServices.ContextDialog, operationContext);

            CollectLogsCommmandWrapper = new ProgressViewModel(ReactiveCommand.CreateFromTask(CollectLogs), progress, this, uiServices.ContextDialog, operationContext);

            IsBusyObservable = Observable.Merge(DeleteDownloadedWrapper.Command.IsExecuting,
                ForceDualBootWrapper.Command.IsExecuting, ForceSingleBootWrapper.Command.IsExecuting,
                CollectLogsCommmandWrapper.Command.IsExecuting);

            preparerUpdater = this.WhenAnyValue(x => x.SelectedPreparer)
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    context.DiskLayoutPreparer = x.Value;
                    lumiaSettingsService.DiskPreparer = (string)x.Metadata["Name"];
                });

            SelectedPreparer = GetInitialDiskPreparer();
        }

        private async Task CollectLogs()
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), LogsZipName);
                await logCollector.Collect(context.Device, path);
                var fileInfo = new FileInfo(path);
                ExploreFile(fileInfo.FullName);
            }
            catch (NothingToSaveException)
            {
                await uiServices.ContextDialog.ShowAlert(this, "Nothing to collect", "Sorry, no logs have been found in your phone");
            }
        }

        private static void ExploreFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }

        private Meta<IDiskLayoutPreparer> GetInitialDiskPreparer()
        {
            var fromSettings = DiskPreparers.FirstOrDefault(x => (string)x.Metadata["Name"] == lumiaSettingsService.DiskPreparer);
            return fromSettings ?? Default;
        }

        private Meta<IDiskLayoutPreparer> Default
        {
            get
            {
                return DiskPreparers
                    .OrderBy(x => (int)x.Metadata["Order"])
                    .First();
            }
        }

        public Meta<IDiskLayoutPreparer> SelectedPreparer
        {
            get => selectedPreparer;
            set => this.RaiseAndSetIfChanged(ref selectedPreparer, value);
        }

        public ProgressViewModel DeleteDownloadedWrapper { get; }

        public bool UseCompactDeployment
        {
            get => lumiaSettingsService.UseCompactDeployment;
            set
            {
                lumiaSettingsService.UseCompactDeployment = value;
                this.RaisePropertyChanged(nameof(UseCompactDeployment));
            }
        }

        public bool CleanDownloadedBeforeDeployment
        {
            get => lumiaSettingsService.CleanDownloadedBeforeDeployment;
            set
            {
                lumiaSettingsService.CleanDownloadedBeforeDeployment = value;
                this.RaisePropertyChanged(nameof(CleanDownloadedBeforeDeployment));
            }
        }

        public ProgressViewModel CollectLogsCommmandWrapper { get; }

        public ProgressViewModel ForceDualBootWrapper { get; }

        public ProgressViewModel ForceSingleBootWrapper { get; }

        public IEnumerable<Meta<IDiskLayoutPreparer>> DiskPreparers { get; set; }

        public void Dispose()
        {
            preparerUpdater?.Dispose();
        }

        public IObservable<bool> IsBusyObservable { get; }

        private async Task ForceDualBoot()
        {
            await ((IPhone)context.Device).ToogleDualBoot(true, true);

            await uiServices.ContextDialog.ShowAlert(this, Resources.Done, Resources.DualBootEnabled);
        }

        private async Task ForceDisableDualBoot()
        {
            await ((IPhone)context.Device).ToogleDualBoot(false, true);

            await uiServices.ContextDialog.ShowAlert(this, Resources.Done, Resources.DualBootDisabled);
        }

        private async Task DeleteDownloaded(IFileSystemOperations fileSystemOperations)
        {
            if (fileSystemOperations.DirectoryExists(AppPaths.ArtifactDownload))
            {
                await fileSystemOperations.DeleteDirectory(AppPaths.ArtifactDownload);
                await uiServices.ContextDialog.ShowAlert(this, Resources.Done, UI.Properties.Resources.DownloadedFolderDeleted);
            }
            else
            {
                await uiServices.ContextDialog.ShowAlert(this, UI.Properties.Resources.DownloadedFolderNotFoundTitle,
                    UI.Properties.Resources.DownloadedFolderNotFound);
            }
        }
    }
}