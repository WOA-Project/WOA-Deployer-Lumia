using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia.Tasks;
using Deployer.Lumia.Tasks.DevOpsBuildClient;
using Deployer.Services;
using Deployer.Tasks;
using Grace.DependencyInjection;
using Serilog;

namespace Deployer.Lumia.NetFx
{
    public class DeploymentScriptRunner
    {
        public async Task ExecuteWindowsScript(string script, WindowsDeploymentOptions windowsDeploymentCmdOptions, IObserver<double> progressObserver)
        {
            await Deploy(script, () => GetContainer(windowsDeploymentCmdOptions, progressObserver));
        }

        public async Task ExecuteNonWindowsScript(string path)
        {
            await Deploy(path, GetContainer);
        }

        private static async Task Deploy(string script, Func<DependencyInjectionContainer> getContainer)
        {
            Log.Information("Deployment Started");
            var container = getContainer();
            var deployer = container.Locate<ScriptRunner>();
            await deployer.Deploy(script);
            Log.Information("Deployment finished.");
        }

        private static DependencyInjectionContainer GetContainer(WindowsDeploymentOptions windowsDeploymentCmdOptions, IObserver<double> observer)
        {
            var container = new DependencyInjectionContainer();
            container.Configure(x =>
            {
                x.ExportFactory(() => new InstallOptions()
                {
                    ImagePath = windowsDeploymentCmdOptions.WimImage,
                    ImageIndex = windowsDeploymentCmdOptions.Index,                    
                    SizeReservedForWindows = ByteSize.FromGigaBytes(windowsDeploymentCmdOptions.ReservedSizeForWindowsInGb)
                }).As<InstallOptions>();
                x.ExportInstance(observer).As<IObserver<double>>();
                ConfigureShared(x);
            });

            return container;
        }

        private static void ConfigureShared(IExportRegistrationBlock x)
        {
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
        }

        private static DependencyInjectionContainer GetContainer()
        {
            var container = new DependencyInjectionContainer();
            container.Configure(ConfigureShared);

            return container;
        }
    }
}