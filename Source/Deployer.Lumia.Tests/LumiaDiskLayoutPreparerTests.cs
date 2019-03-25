using System.Threading.Tasks;
using Deployer.Lumia.NetFx.PhoneInfo;
using Deployer.NetFx;
using Xunit;

namespace Deployer.Lumia.Tests
{
    public class LumiaDiskLayoutPreparerTests
    {
        [Fact(Skip = "Don't run this!")]
        [Trait("Category", "Real")]
        public async Task Prepare()
        {
            var api = new DiskApi();

            var defaultSpaceAllocators = new []{ new DefaultSpaceAllocator()};
            var fileSystemOperations = new FileSystemOperations();
            var windowsDeploymentOptionsProvider = new WindowsDeploymentOptionsProvider();

            var phone = new Phone(api, new PhoneModelReader(new PhoneInfoReader()), new BcdInvokerFactory());
            var preparer = new LumiaDiskLayoutPreparer(windowsDeploymentOptionsProvider, fileSystemOperations, defaultSpaceAllocators, phone);

            var disk = await api.GetDisk(3);
            await preparer.Prepare(disk);
        }
    }
}
