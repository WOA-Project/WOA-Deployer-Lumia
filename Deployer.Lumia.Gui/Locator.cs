using System;
using System.Reactive.Subjects;
using Deployer.Gui.Core;
using Deployer.Lumia.Gui.ViewModels;
using Deployer.Lumia.NetFx;
using Grace.DependencyInjection;
using Installer.Wpf.Core;
using Installer.Wpf.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Serilog.Events;

namespace Deployer.Lumia.Gui
{
    public class Locator
    {
        private readonly DependencyInjectionContainer container;

        public Locator()
        {
            container = new DependencyInjectionContainer();

            IObservable<LogEvent> logEvents = null;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(@"Logs\Log-{Date}.txt")
                .WriteTo.Observers(x => logEvents = x)
                .MinimumLevel.Verbose()
                .CreateLogger();

            container.Configure(x =>
            {
                x.ExportFactory(() => new BehaviorSubject<double>(double.NaN))
                    .As<IObserver<double>>()
                    .As<IObservable<double>>()
                    .Lifestyle.Singleton();
                x.ExportFactory(() => logEvents).As<IObservable<LogEvent>>();
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