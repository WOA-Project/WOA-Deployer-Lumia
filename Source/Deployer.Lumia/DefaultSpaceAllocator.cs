using System;
using System.Threading.Tasks;
using ByteSizeLib;
using Serilog;

namespace Deployer.Lumia
{
    public class DefaultSpaceAllocator : ISpaceAllocator<IPhone>
    {
        public async Task<bool> TryAllocate(IPhone phone, ByteSize requiredSpace)
        {
            Log.Verbose("Trying to shrink Data partition...");

            var dataVolume = await phone.GetVolumeByPartitionName(PartitionName.Data);

            if (dataVolume == null)
            {
                Log.Verbose("Data partition doesn't exist. Skipping.");
                return false;
            }

            var phoneDisk = await phone.GetDeviceDisk();
            var data = dataVolume.Size;
            var allocated = phoneDisk.AllocatedSize;
            var available = phoneDisk.AvailableSize;
            var newData =  data - (requiredSpace - available);
            
            Log.Verbose("Total size allocated: {Size}", allocated);
            Log.Verbose("Space available: {Size}", available);
            Log.Verbose("Space needed: {Size}", requiredSpace);
            Log.Verbose("'Data' size: {Size}", data);
            Log.Verbose("Calculated new size for the 'Data' partition: {Size}", newData);
          
            Log.Verbose("Resizing 'Data' to {Size}", newData);

            await dataVolume.Partition.Resize(newData);

            Log.Verbose("Resize operation completed successfully");

            var isEnoughAlready = await phone.HasEnoughSpace(requiredSpace);
            return isEnoughAlready;
        }     
    }
}