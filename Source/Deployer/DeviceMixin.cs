using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Deployer.FileSystem;

namespace Deployer
{
    public static class DeviceMixin
    {
        public static async Task EnsureBootPartitionIs(this Device device, PartitionType partitionType)
        {
            Partition partition = await GetBootPartition(device);
            if (partition == null)
            {
                partition = (await device.GetBootVolume())?.Partition;
            }

            if (partition == null)
            {
                throw new InvalidOperationException("Cannot get the boot partition");
            }

            await partition.SetGptType(partitionType);            
        }

        public static async Task<Partition> GetBootPartition(this Device device)
        {
            var partitions = await (await device.GetDisk()).GetPartitions();
            var bootPartition = partitions.FirstOrDefault(x => Equals(x.PartitionType, PartitionType.Esp));
            if (bootPartition != null)
            {
                return bootPartition;
            }

            var bootVolume = await device.GetBootVolume();
            return bootVolume?.Partition;
        }
    }
}