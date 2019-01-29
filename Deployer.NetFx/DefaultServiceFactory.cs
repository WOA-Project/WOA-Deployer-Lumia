using Deployer.Services;

namespace Deployer.Filesystem.FullFx
{
    public class DefaultServiceFactory : ServiceFactory
    {
        public DefaultServiceFactory()
        {
            DiskService = new DiskService(new LowLevelApi());
            ImageService = new DismImageService();
        }
    }
}