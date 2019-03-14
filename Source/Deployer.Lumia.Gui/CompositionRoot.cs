using System;
using Deployer.Gui;
using Deployer.Gui.Services;
using Deployer.Gui.ViewModels;
using Deployer.Lumia.Gui.Specifics;
using Deployer.Lumia.Gui.ViewModels;
using Deployer.Lumia.NetFx;
using Deployer.Tasks;
using Grace.DependencyInjection;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Serilog.Events;

namespace Deployer.Lumia.Gui
{
    public static class CompositionRoot
    {
        public static DependencyInjectionContainer CreateContainer()
        {
            var container = new DependencyInjectionContainer();

            IObservable<LogEvent> logEvents = null;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.RollingFile(@"Logs\Log-{Date}.txt")
                .WriteTo.Observers(x => logEvents = x)
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Verbose($"Started {AppProperties.AppTitle}");

            var optionsProvider = new WindowsDeploymentOptionsProvider();

            container.Configure(x =>
            {
                x.Configure(optionsProvider);
                x.ExportFactory(() => new DownloadProgress())
                    .As<IDownloadProgress>()
                    .Lifestyle.Singleton();
                x.ExportFactory(() => logEvents).As<IObservable<LogEvent>>();
                x.Export<WimPickViewModel>().ByInterfaces().As<WimPickViewModel>().Lifestyle.Singleton();
                x.Export<DualBootViewModel>().ByInterfaces().As<DualBootViewModel>().Lifestyle.Singleton();
                x.Export<AdvancedViewModel>().ByInterfaces().As<AdvancedViewModel>().Lifestyle.Singleton();
                x.Export<DeploymentViewModel>().ByInterfaces().As<DeploymentViewModel>().Lifestyle.Singleton();
                x.Export<UIServices>();
                x.Export<Dialog>().ByInterfaces();
                x.Export<OpenFilePicker>().As<IOpenFilePicker>();
                x.Export<SaveFilePicker>().As<ISaveFilePicker>();
                x.Export<SettingsService>().As<ISettingsService>();
                x.Export<SaveFilePicker>().As<ISaveFilePicker>();
                x.Export<ViewService>().As<IViewService>();
                x.ExportFactory(() => DialogCoordinator.Instance).As<IDialogCoordinator>();                
            });

            return container;
        }
    }
}