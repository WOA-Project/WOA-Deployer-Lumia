using Deployer.Gui.ViewModels;
using Deployer.Lumia.Gui.ViewModels;
using Grace.DependencyInjection;

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

        public StatusViewModel StatusViewModel => container.Locate<StatusViewModel>();
    }
}