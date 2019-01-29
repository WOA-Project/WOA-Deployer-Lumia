using System.Threading.Tasks;
using Deployer;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia;
using Grace.DependencyInjection;

namespace Deployment.Console
{
    public class ConsoleTooling
    {
        private static DependencyInjectionContainer GetContainer()
        {
            var container = new DependencyInjectionContainer();
            container.Configure(x =>
            {
                x.Export<LowLevelApi>().As<ILowLevelApi>();
                x.Export<Phone>();
                x.Export<FileSystemOperations>().As<IFileSystemOperations>();
            });

            return container;
        }

        public static async Task ToogleDualBoot(bool isEnabled)
        {
            var container = GetContainer();
            var phone = container.Locate<Phone>();
            await phone.EnableDualBoot(isEnabled);
        }        
    }
}