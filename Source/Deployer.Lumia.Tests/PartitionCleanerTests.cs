using System.Linq;
using System.Threading.Tasks;
using Deployer.Lumia.NetFx;
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
            var phone = new TestPhone(new DiskApi(), null, null);

            var partitionCleaner = new PartitionCleaner();
            await partitionCleaner.Clean(phone);

            var disk = await phone.GetDeviceDisk();
            var partitions = await disk.GetPartitions();
            var volume = await partitions.Last().GetVolume();
            volume.Should().NotBeNull();
            volume.Label.Should().Be(PartitionName.Data);
        }
    }
}