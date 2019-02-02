using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia.NetFx.PhoneInfo;
using Deployer.Services;
using Grace.DependencyInjection;

namespace Deployer.Lumia.NetFx
{
    public static class DeployerComposition
    {
        public static DependencyInjectionContainer Configure(DependencyInjectionContainer container)
        {
            var taskTypes = AssemblyUtils.FindTypes(x => x.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask)));

            container.Configure(x =>
            {
                x.Export<PhoneInfoReader>().As<IPhoneInfoReader>();
                x.Export<PhoneModelReader>().As<IPhoneModelReader>();
                x.Export<AutoDeployer>().As<IAutoDeployer>();
                x.Export<DeploymentScriptRunner>().As<IDeploymentScriptRunner>();
                x.Export<Tooling>().As<ITooling>();
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
            });

            return container;
        }
    }
}