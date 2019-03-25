using System.Threading.Tasks;
using ByteSizeLib;
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

            var allocators = new []{ new DefaultSpaceAllocator()};
            var operations = new FileSystemOperations();
            var optionsProvider = new WindowsDeploymentOptionsProvider
            {
                Options = new WindowsDeploymentOptions
                {
                    SizeReservedForWindows = ByteSize.FromGigaBytes(20),
                }
            };

            var phone = new Phone(api, new PhoneModelReader(new PhoneInfoReader()), new BcdInvokerFactory());
            var preparer = new LumiaDiskLayoutPreparer(optionsProvider, operations, allocators, new PartitionCleaner(), phone);

            var disk = await api.GetDisk(3);
            await preparer.Prepare(disk);
        }       
    }
}
