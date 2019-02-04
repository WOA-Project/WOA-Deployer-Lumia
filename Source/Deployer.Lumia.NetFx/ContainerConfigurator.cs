using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia.Gui;
using Deployer.Lumia.NetFx.PhoneInfo;
using Deployer.Services;
using Grace.DependencyInjection;
using Superpower;

namespace Deployer.Lumia.NetFx
{
    public static class ContainerConfigurator
    {
        public static IExportRegistrationBlock Configure(IExportRegistrationBlock block,
            WindowsDeploymentOptionsProvider installOptionsProvider)
        {
            var taskTypes = from a in Assemblies.AppDomainAssemblies
                from type in a.ExportedTypes
                where type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask))
                select type;

            block.ExportFactory(Tokenizer.Create).As<Tokenizer<LangToken>>();
            block.Export<ScriptParser>().As<IScriptParser>();
            block.ExportFactory(() => installOptionsProvider).As<IWindowsOptionsProvider>();
            block.Export<PhoneModelReader>().As<IPhoneModelReader>();
            block.Export<PhoneInfoReader>().As<IPhoneInfoReader>();
            block.Export<WoaDeployer>().As<IWoaDeployer>();
            block.Export<Tooling>().As<ITooling>();
            block.Export<BootCreator>().As<IBootCreator>();
            block.Export<LowLevelApi>().As<ILowLevelApi>();
            block.Export<PhonePathBuilder>().As<IPathBuilder>();
            block.ExportInstance(taskTypes).As<IEnumerable<Type>>();
            block.Export<ScriptRunner>().As<IScriptRunner>();
            block.Export<InstanceBuilder>().As<IInstanceBuilder>();
            block.Export<Phone>().As<Phone>().As<Device>();
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