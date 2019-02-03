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
        public static IExportRegistrationBlock Configure(IExportRegistrationBlock block)
        {
            var taskTypes = AssemblyUtils.FindTypes(x =>
                x.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask)));
            block.Export<ScriptRunner>().As<IScriptRunner>();
            block.Export<PhoneModelReader>().As<IPhoneModelReader>();
            block.Export<PhoneInfoReader>().As<IPhoneInfoReader>();
            block.Export<AutoDeployer>().As<IAutoDeployer>();
            block.Export<Tooling>().As<ITooling>();
            block.Export<BootCreator>().As<IBootCreator>();
            block.Export<LowLevelApi>().As<ILowLevelApi>();
            block.Export<PhonePathBuilder>().As<IPathBuilder>();
            block.ExportInstance(taskTypes).As<IEnumerable<Type>>();
            block.Export<Runner>().As<IRunner>();
            block.Export<InstanceBuilder>().As<IInstanceBuilder>();
            block.Export<Phone>();
            block.Export<FileSystemOperations>().As<IFileSystemOperations>();
            block.Export<BcdInvokerFactory>().As<IBcdInvokerFactory>();
            block.Export<WindowsDeployer>().As<IWindowsDeployer>();
            block.Export<DismImageService>().As<IWindowsImageService>();
            block.Export<GitHubDownloader>().As<IGitHubDownloader>();
            block.ExportFactory(() => AzureDevOpsClient.Create(new Uri("https://dev.azure.com"))).As<IAzureDevOpsBuildClient>();

            return block;
        }
    }
}