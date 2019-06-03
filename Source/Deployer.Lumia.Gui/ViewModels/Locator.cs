using Deployer.UI.ViewModels;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class Locator
    {
        private readonly DependencyInjectionContainer container;
        
        public Locator()
        {
            container = CompositionRoot.CreateContainer();
        }

        public MainViewModel MainViewModelBase => container.Locate<MainViewModel>();

        public WimPickViewModel WimPickViewModel => container.Locate<WimPickViewModel>();

        public DeploymentViewModel DeploymentViewModel => container.Locate<DeploymentViewModel>();

        public AdvancedViewModel AdvancedViewModel => container.Locate<AdvancedViewModel>();

        public DualBootViewModel DualBootViewModel => container.Locate<DualBootViewModel>();

        public LogViewModel LogViewModel => container.Locate<LogViewModel>();

        public ScriptsViewModel ScriptsViewModel => container.Locate<ScriptsViewModel>();

        public OngoingOperationViewModel OngoingOperationViewModel => container.Locate<OngoingOperationViewModel>();

        public WoaMaintenanceViewModel WoaMaintenanceViewModel => container.Locate<WoaMaintenanceViewModel>();
    }
}