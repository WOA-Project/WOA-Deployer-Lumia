using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia;
using Deployer.Lumia.NetFx;
using Deployer.Services;
using Deployer.Tasks;
using Deployment.Console.Options;
using Grace.DependencyInjection;

namespace Deployment.Console
{
    internal class ConsoleDeployer : IAutoDeployer
    {
        private readonly IEnumerable<Type> types;

        public ConsoleDeployer(IEnumerable<Type> types)
        {
            this.types = types;
        }

        public async Task ExecuteWindowsScript(WindowsDeploymentCmdOptions opts)
        {
            var progressObserver = new Subject<double>();
            progressObserver.Subscribe(x => System.Console.WriteLine("{0:P0}"));
            var winDeploymentOpts = new WindowsDeploymentOptions
            {
                Index = opts.Index,
                ReservedSizeForWindowsInGb = opts.ReservedSizeForWindowsInGb,
                WimImage = opts.WimImage,
            };

            var container = GetContainer(winDeploymentOpts, types, progressObserver);
            var autoDeployer = container.Locate<IAutoDeployer>();
            await autoDeployer.Deploy(winDeploymentOpts, progressObserver);
            progressObserver.Dispose();
        }

        public Task Deploy(WindowsDeploymentOptions opts, IObserver<double> progressObserver = null)
        {
            var container = GetContainer(opts, types, progressObserver);
            var deployer = container.Locate<IAutoDeployer>();
            return deployer.Deploy(opts, progressObserver);
        }

        public Task ExecuteNonWindowsScript(string path)
        {
            var container = GetContainer();
            var autoDeployer = container.Locate<IAutoDeployer>();
            return autoDeployer.ExecuteNonWindowsScript(path);
        }

        public Task ToogleDualBoot(bool isEnabled)
        {
            var container = GetContainer();
            var autoDeployer = container.Locate<IAutoDeployer>();

            return autoDeployer.ToogleDualBoot(isEnabled);
        }

        public async Task InstallGpu()
        {
            var container = GetContainer();
            var autoDeployer = container.Locate<IAutoDeployer>();

            await autoDeployer.InstallGpu();
            System.Console.WriteLine(Resources.InstallGpuManualStep);
        }    

        private DependencyInjectionContainer GetContainer()
        {
            var container = new DependencyInjectionContainer();
            container.Configure(x =>
            {
                x.Export<BootCreator>().As<IBootCreator>();
                x.Export<LowLevelApi>().As<ILowLevelApi>();
                x.ExportInstance(typeof(Copy).Assembly.ExportedTypes).As<IEnumerable<Type>>();
                x.Export<Runner>().As<IRunner>();
                x.Export<InstanceBuilder>().As<IInstanceBuilder>();
                x.Export<Phone>();
                x.Export<FileSystemOperations>().As<IFileSystemOperations>();
                x.Export<BcdInvokerFactory>().As<IBcdInvokerFactory>();
                x.Export<WindowsDeployer>().As<IWindowsDeployer>();
                x.Export<DismImageService>().As<IWindowsImageService>();
                x.Export<GitHubDownloader>().As<IGitHubDownloader>();
                x.Export<PhonePathBuilder>().As<IPathBuilder>();
                x.ExportFactory(() => AzureDevOpsClient.Create(new Uri("https://dev.azure.com"))).As<IAzureDevOpsBuildClient>();
            });

            return container;
        }

        private static DependencyInjectionContainer GetContainer(WindowsDeploymentOptions windowsDeploymentCmdOptions, IEnumerable<Type> taskTypes, IObserver<double> observer)
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

                x.ExportInstance(observer ?? new Subject<double>()).As<IObserver<double>>();

                ConfigureShared(x, taskTypes);
            });

            return container;
        }

        private static void ConfigureShared(IExportRegistrationBlock x, IEnumerable<Type> taskTypes)
        {
            x.Export<AutoDeployer>().As<IAutoDeployer>();
            x.Export<DeploymentScriptRunner>().As<IDeploymentScriptRunner>();
            x.Export<AdditionalOperations>().As<IAdditionalOperations>();
            x.Export<BootCreator>().As<IBootCreator>();
            x.Export<LowLevelApi>().As<ILowLevelApi>();
            x.Export<PhonePathBuilder>().As<IPathBuilder>();
            x.ExportInstance(taskTypes).As<IEnumerable<Type>>();
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
    }
}