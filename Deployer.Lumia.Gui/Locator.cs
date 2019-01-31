using Deployer.Gui.Core;
using Deployer.Lumia.Gui.ViewModels;
using Deployer.Lumia.NetFx;
using Grace.DependencyInjection;
using Installer.Wpf.Core;
using Installer.Wpf.Core.Services;
using MahApps.Metro.Controls.Dialogs;

namespace Deployer.Lumia.Gui
{
    public class Locator
    {
        private readonly DependencyInjectionContainer container;

        public Locator()
        {
            container = new DependencyInjectionContainer();

            container.Configure(x =>
            {
                x.Export<WimPickViewModel>().Lifestyle.Singleton();
                x.Export<UIServices>();
                x.Export<ViewService>().As<IViewService>();
                x.Export<DialogService>().As<IDialogService>();
                x.Export<FilePicker>().As<IFilePicker>();
                x.Export<SettingsService>().As<ISettingsService>();
                x.ExportFactory(() => DialogCoordinator.Instance).As<IDialogCoordinator>();
            });

            DeployerComposition.Configure(container);
        }

        public MainViewModel MainViewModel => container.Locate<MainViewModel>();

        public WimPickViewModel WimPickViewModel => container.Locate<WimPickViewModel>();

        public DeploymentViewModel DeploymentViewModel => container.Locate<DeploymentViewModel>();
    }
}