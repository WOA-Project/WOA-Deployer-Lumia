using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia;
using Deployer.Lumia.Tasks;
using Deployer.Lumia.Tasks.DevOpsBuildClient;
using Deployer.Services;
using Grace.DependencyInjection;
using Serilog;

namespace Deployment.Console
{
    public class ConsoleDeployer
    {
        public async Task DeployWindows(WindowsDeploymentOptions windowsDeploymentOptions)
        {
            var subject = new Subject<double>();
            subject.Subscribe(x => System.Console.WriteLine("{0:P}", x));
            var container = GetContainer(windowsDeploymentOptions, subject);
            var deployer = container.Locate<ScriptDeployer>();
            await deployer.Deploy(windowsDeploymentOptions.Script);
            subject.Dispose();

            Log.Information("Deployment finished. Reboot and proceed with the Windows Setup.");
        }

        private static DependencyInjectionContainer GetContainer(WindowsDeploymentOptions windowsDeploymentOptions, IObserver<double> observer)
        {
            var container = new DependencyInjectionContainer();
            container.Configure(x =>
            {
                x.ExportFactory(() => new InstallOptions()
                {
                    ImagePath = windowsDeploymentOptions.WimImage,
                    ImageIndex = windowsDeploymentOptions.Index,                    
                    SizeReservedForWindows = ByteSize.FromGigaBytes(windowsDeploymentOptions.ReservedSizeForWindowsInGb)
                }).As<InstallOptions>();
                x.ExportInstance(observer).As<IObserver<double>>();
                x.Export<BootCreator>().As<IBootCreator>();
                x.Export<LowLevelApi>().As<ILowLevelApi>();
                x.Export<PhonePathBuilder>().As<IPathBuilder>();
                x.ExportInstance(typeof(Copy).Assembly.ExportedTypes).As<IEnumerable<Type>>();
                x.Export<Runner>().As<IRunner>();
                x.Export<InstanceBuilder>().As<IInstanceBuilder>();
                x.Export<Phone>();
                x.Export<FileSystemOperations>().As<IFileSystemOperations>();
                x.Export<BcdInvokerFactory>().As<IBcdInvokerFactory>();
                x.Export<WindowsDeployer>().As<IWindowsDeployer>();
                x.Export<DismImageService>().As<IWindowsImageService>();
                x.Export<GitHubDownloader>().As<IGitHubDownloader>();
                x.ExportFactory(() => AzureDevOpsClient.Create(new Uri("https://dev.azure.com"))).As<IAzureDevOpsBuildClient>();
            });

            return container;
        }
    }
}