using System.Diagnostics;
using System.Reactive;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class LogViewModel : ReactiveObject
    {
        private readonly IFileSystemOperations fileSystemOperations;

        public LogViewModel(IFileSystemOperations fileSystemOperations)
        {
            this.fileSystemOperations = fileSystemOperations;
            OpenLogFolder = ReactiveCommand.Create(OpenLogs);
        }

        public ReactiveCommand<Unit, Unit> OpenLogFolder { get; set; }

        private void OpenLogs()
        {
            fileSystemOperations.EnsureDirectoryExists("Logs");
            Process.Start("Logs");
        }
    }
}