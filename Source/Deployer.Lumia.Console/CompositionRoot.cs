using Deployer.Console;
using Deployer.Lumia.NetFx;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Console
{
    public static class CompositionRoot
    {
        public static DependencyInjectionContainer CreateContainer(WindowsDeploymentOptionsProvider op, IOperationProgress progress)
        {
            var container = new DependencyInjectionContainer();

            container.Configure(x =>
            {
                x.Configure(op);
                x.Export<ConsolePrompt>().As<IPrompt>();
                x.ExportInstance(progress).As<IOperationProgress>();
            });

            return container;
        }
    }  
}