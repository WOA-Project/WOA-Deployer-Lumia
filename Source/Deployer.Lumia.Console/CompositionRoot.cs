using Deployer.Console;
using Deployer.Lumia.NetFx;
using Deployer.Tasks;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Console
{
    public static class CompositionRoot
    {
        public static DependencyInjectionContainer CreateContainer(WindowsDeploymentOptionsProvider op, IDownloadProgress progress)
        {
            var container = new DependencyInjectionContainer();

            container.Configure(x =>
            {
                x.Configure(op);
                x.Export<ConsoleMarkdownDialog>().As<IMarkdownDialog>();
                x.Export<ConsoleMarkdownDisplayer>().As<IMarkdownDisplayer>();
                x.ExportInstance(progress).As<IDownloadProgress>();
            });

            return container;
        }
    }  
}