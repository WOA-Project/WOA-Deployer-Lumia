using System.Diagnostics;
using System.Reactive;
using Deployer.Gui.ViewModels;
using Deployer.Lumia.Gui.ViewModels;
using Grace.DependencyInjection;
using ReactiveUI;

namespace Deployer.Lumia.Gui
{
    public class Locator
    {
        private readonly DependencyInjectionContainer container;
        
        public Locator()
        {
            container = CompositionRoot.CreateContainer();
        }

        public MainViewModel MainViewModel => container.Locate<MainViewModel>();

        public WimPickViewModel WimPickViewModel => container.Locate<WimPickViewModel>();

        public DeploymentViewModel DeploymentViewModel => container.Locate<DeploymentViewModel>();

        public AdvancedViewModel AdvancedViewModel => container.Locate<AdvancedViewModel>();

        public DualBootViewModel DualBootViewModel => container.Locate<DualBootViewModel>();

        public LogViewModel LogViewModel => container.Locate<LogViewModel>();

        public OngoingOperationViewModel OngoingOperationViewModel => container.Locate<OngoingOperationViewModel>();
    }

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