using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class ConsoleTooling
    {
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

        public async Task ToogleDualBoot(bool isEnabled)
        {
            var container = GetContainer();
            var tooling = container.Locate<Tooling>();
            await tooling.ToogleDualBoot(isEnabled);
        }

        public async Task InstallGpu()
        {
            var container = GetContainer();
            var tooling = container.Locate<Tooling>();
            await tooling.InstallGpu();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Tooling
        {
            private readonly Phone phone;
            private readonly IRunner runner;

            public Tooling(Phone phone, IRunner runner)
            {
                this.phone = phone;
                this.runner = runner;
            }

            public async Task ToogleDualBoot(bool isEnabled)
            {
                var enabledStr = isEnabled ? "Enabling" : "Disabling";
                Log.Information($"{enabledStr} Dual Boot");
                await phone.EnableDualBoot(isEnabled);

                Log.Information("Done");
            }

            public async Task InstallGpu()
            {
                Log.Information("Installing GPU");
                await ToogleDualBoot(true);

                IList<Sentence> sentences = new List<Sentence>()
                {
                    new Sentence(new Command(nameof(GitHubUnpack), new[] {new Argument("https://github.com/gus33000/MSM8994-8992-NT-ARM64-Drivers"),})),
                    new Sentence(new Command(nameof(CopyDirectory), new[]
                    {
                        new Argument(@"Downloaded\MSM8994-8992-NT-ARM64-Drivers-master\Supplemental\GPU\Cityman"),
                        new Argument(@"WindowsARM\Users\Public\OEMPanel"),
                    })),
                };

                await runner.Run(new Script(sentences));                
            }
        }
    }
}