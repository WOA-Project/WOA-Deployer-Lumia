using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Services;
using Grace.DependencyInjection;
using Serilog;

namespace Deployer.Lumia.NetFx
{
    public class DeploymentScriptRunner
    {
        private readonly IEnumerable<Type> deploymentTaskTypes;

        public DeploymentScriptRunner(IEnumerable<Type> deploymentTaskTypes)
        {
            this.deploymentTaskTypes = deploymentTaskTypes;
        }

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

        private DependencyInjectionContainer GetContainer(WindowsDeploymentOptions windowsDeploymentCmdOptions, IObserver<double> observer)
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

        private void ConfigureShared(IExportRegistrationBlock x)
        {
            x.Export<BootCreator>().As<IBootCreator>();
            x.Export<LowLevelApi>().As<ILowLevelApi>();
            x.Export<PhonePathBuilder>().As<IPathBuilder>();
            x.ExportInstance(deploymentTaskTypes).As<IEnumerable<Type>>();
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

        private DependencyInjectionContainer GetContainer()
        {
            var container = new DependencyInjectionContainer();
            container.Configure(ConfigureShared);

            return container;
        }
    }
}