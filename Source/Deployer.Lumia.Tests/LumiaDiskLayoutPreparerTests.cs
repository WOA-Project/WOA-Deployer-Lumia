using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Lumia.NetFx;
using Deployer.NetFx;
using Xunit;

namespace Deployer.Lumia.Tests
{
    public class LumiaDiskLayoutPreparerTests
    {
        [Fact(Skip = "Don't run this")]
        [Trait("Category", "Real")]
        public async Task Prepare()
        {
            //var api = new DiskApi();

            //var allocators = new []{ new DefaultSpaceAllocator()};
            //var operations = new FileSystemOperations();
            //var optionsProvider = new WindowsDeploymentOptionsProvider
            //{
            //    Options = new WindowsDeploymentOptions
            //    {
            //        SizeReservedForWindows = ByteSize.FromMegaBytes(200),
            //    }
            //};

            //var phone = new TestPhone(api, null, null);
            //var preparer = new LumiaDiskLayoutPreparer(optionsProvider, operations, allocators, new PartitionCleaner(), phone);

            //var disk = await api.GetDisk(3);
            //await preparer.Prepare(disk);
        }       
    }
}
