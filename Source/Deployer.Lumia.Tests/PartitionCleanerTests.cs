using System.Linq;
using System.Threading.Tasks;
using Deployer.Lumia.NetFx.PhoneInfo;
using Deployer.NetFx;
using FluentAssertions;
using Xunit;

namespace Deployer.Lumia.Tests
{
    public class PartitionCleanerTests
    {
        [Fact(Skip = "Don't run this!")]
        [Trait("Category", "Real")]
        public async Task Clean() 
        {
            var api = new DiskApi();

            var phone = new Phone(api, new PhoneModelReader(new PhoneInfoReader()), new BcdInvokerFactory());

            var partitionCleaner = new PartitionCleaner();
            await partitionCleaner.Clean(phone);

            var disk = await phone.GetDeviceDisk();
            var partitions = await disk.GetPartitions();
            var volume = await partitions.Last().GetVolume();
            volume.Should().NotBeNull();
            volume.Label.Should().Be("Data");
        }
    }
}