using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.FileSystem;
using Deployer.Lumia.NetFx.PhoneMetadata;
using Deployer.NetFx;
using Deployer.Services;
using Deployer.Tasks;
using Grace.DependencyInjection;
using Octokit;
using Superpower;

namespace Deployer.Lumia.NetFx
{
    public static class ContainerConfigurator
    {
        public static IExportRegistrationBlock Configure(this IExportRegistrationBlock block,
            WindowsDeploymentOptionsProvider installOptionsProvider)
        {
            return WithCommon(block, installOptionsProvider).WithRealPhone();
        }

        public static IExportRegistrationBlock ConfigureForTesting(this IExportRegistrationBlock block,
            WindowsDeploymentOptionsProvider installOptionsProvider)
        {
            return WithCommon(block, installOptionsProvider).WithTestingPhone();
        }

        public static IExportRegistrationBlock WithCommon(this IExportRegistrationBlock block,
            WindowsDeploymentOptionsProvider installOptionsProvider)
        {
            var taskTypes = from a in Assemblies.AppDomainAssemblies
                            from type in a.ExportedTypes
                            where type.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask))
                            select type;
            block.ExportAssemblies(Assemblies.AppDomainAssemblies).ByInterface<ISpaceAllocator<IPhone>>();
            block.Export<ZipExtractor>().As<IZipExtractor>();
            block.ExportFactory(Tokenizer.Create).As<Tokenizer<LangToken>>().Lifestyle.Singleton();
            block.Export<ScriptParser>().As<IScriptParser>().Lifestyle.Singleton();
            block.ExportFactory(() => installOptionsProvider).As<IWindowsOptionsProvider>().Lifestyle.Singleton();
            block.Export<LumiaDiskLayoutPreparer>().As<IDiskLayoutPreparer>().Lifestyle.Singleton();
            block.Export<PhoneInfoReader>().As<IPhoneInfoReader>().Lifestyle.Singleton();
            block.Export<WoaDeployer>().As<IWoaDeployer>().Lifestyle.Singleton();
            block.Export<Tooling>().As<ITooling>().Lifestyle.Singleton();
            block.Export<BootCreator>().As<IBootCreator>().Lifestyle.Singleton();
            block.Export<DiskApi>().As<IDiskApi>().Lifestyle.Singleton();
            block.Export<PhonePathBuilder>().As<IPathBuilder>().Lifestyle.Singleton();
            block.ExportInstance(taskTypes).As<IEnumerable<Type>>();
            block.Export<ScriptRunner>().As<IScriptRunner>().Lifestyle.Singleton();
            block.Export<InstanceBuilder>().As<IInstanceBuilder>().Lifestyle.Singleton();
            block.ExportFactory((IPhone p) => new DeviceProvider { Device = p }).As<IDeviceProvider>();
            block.Export<FileSystemOperations>().As<IFileSystemOperations>().Lifestyle.Singleton();
            block.Export<BcdInvokerFactory>().As<IBcdInvokerFactory>().Lifestyle.Singleton();
            block.Export<WindowsDeployer>().As<IWindowsDeployer>().Lifestyle.Singleton();
            block.ExportFactory(() => new HttpClient {Timeout = TimeSpan.FromMinutes(30)}).Lifestyle.Singleton();
            block.ExportFactory(() => new GitHubClient(new ProductHeaderValue("WOADeployer"))).As<IGitHubClient>();
            block.Export<Downloader>().As<IDownloader>().Lifestyle.Singleton();
            block.Export<ProviderBasedWindowsDeployer>().As<IProviderBasedWindowsDeployer>();
            block.Export<PartitionCleaner>().As<IPartitionCleaner>();
            block.ExportFactory(() => AzureDevOpsClient.Create(new Uri("https://dev.azure.com"))).As<IAzureDevOpsBuildClient>();

            return block;
        }

        private static IExportRegistrationBlock WithRealPhone(this IExportRegistrationBlock block)
        {
            block.Export<PhoneModelInfoInfoReader>().As<IPhoneModelInfoReader>();
            block.Export<Phone>().As<IPhone>().As<IDevice>();
            block.Export<DismImageService>().As<IWindowsImageService>();
            return block;
        }

        private static IExportRegistrationBlock WithTestingPhone(this IExportRegistrationBlock block)
        {
            block.Export<TestPhoneModelInfoReader>().As<IPhoneModelInfoReader>();
            block.Export<TestPhone>().As<IPhone>().As<IDevice>();
            block.Export<TestImageService>().As<IWindowsImageService>();

            return block;
        }
    }
}