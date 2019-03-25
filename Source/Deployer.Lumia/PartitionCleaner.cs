using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Serilog;
using Partition = Deployer.FileSystem.Partition;

namespace Deployer.Lumia
{
    public class PartitionCleaner : IPartitionCleaner
    {
        private IPhone phone;
        private Partition dataPartition;
        private Disk disk;

        public async Task Clean(IPhone toClean)
        {
            Log.Information("Performing partition cleanup");

            phone = toClean;

            disk = await toClean.GetDeviceDisk();
            var dataVolume = await phone.GetDataVolume();
            dataPartition = dataVolume?.Partition;
            
            
            RemoveExistingPartitionsByName();
            await RemovePartitionsAfterData();

            Log.Verbose("Refreshing disk");
            await disk.Refresh();
        }

        private void RemoveExistingPartitionsByName()
        {
            Log.Verbose("Removing existing partitions by name");

            using (var t = new GptContext(disk.Number, FileAccess.ReadWrite))
            {
                t.RemoveExisting(PartitionName.System);
                t.RemoveExisting(PartitionName.Reserved);
                t.RemoveExisting(PartitionName.Windows);                
            }
        }

        private async Task RemovePartitionsAfterData()
        {
            if (dataPartition == null)
            {
                Log.Verbose("Data partition not found. The removal of partitions after Data won't be performed");
                return;
            }

            Log.Verbose("Trying to remove partitions created by previous versions of WOA Deployer");

            var partNumber = (int) dataPartition.Number;
            var partitions = await disk.GetPartitions();

            var toRemove = partitions
                .Skip(partNumber);

            Log.Verbose("Removing legacy partitions");
            foreach (var partition in toRemove)
            {
                await partition.Remove();
            }
        }
    }
}