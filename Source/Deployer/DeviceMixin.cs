using System.Threading.Tasks;
using Deployer.FileSystem;

namespace Deployer
{
    public static class DeviceMixin
    {
        public static async Task EnsureBootPartitionIs(this Device device, PartitionType partitionType)
        {
            var boot = await device.GetBootVolume();
            if (!Equals(boot.Partition.PartitionType, partitionType))
            {
                await boot.Partition.SetGptType(partitionType);
            }
        }
    }
}