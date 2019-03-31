using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private Partition dataPartition;
        private Disk disk;

        public async Task Clean(IPhone toClean)
        {
            Log.Information("Performing partition cleanup");

            disk = await toClean.GetDeviceDisk();
            dataPartition = await disk.GetPartitionByName(PartitionName.Data);

            await RemoveAnyPartitionsAfterData();
            await EnsureDataIsLastPartition();

            Log.Information("Cleanup done");

            await disk.Refresh();
        }

        private async Task EnsureDataIsLastPartition()
        {
            Log.Verbose("Ensuring that Data partition is the last partition");
            using (var c = await GptContextFactory.Create(disk.Number, FileAccess.Read))
            {
                var last = c.Partitions.Last();

                var asCommon = last.AsCommon(disk);
                var volume = await asCommon.GetVolume();

                if (volume == null)
                {
                    throw new PartitioningException("Cannot get the volume of the last partition to check its label.");
                }

                if (!string.Equals(volume.Label, PartitionName.Data, StringComparison.InvariantCultureIgnoreCase))
                {                   
                    throw new PartitioningException($"The label of the last partition should be '{PartitionName.Data}' and it's '{volume.Label}'");
                }
            }
        }

        private async Task RemoveAnyPartitionsAfterData()
        {
            Log.Verbose("Removing all the partitions after the Data partition");


            if (dataPartition == null)
            {
                Log.Verbose("Data partition not found. The removal of partitions after Data won't be performed");
                return;
            }

            using (var c = await GptContextFactory.Create(disk.Number, FileAccess.ReadWrite))
            {
                var toRemove = GetPartitionsAfterData(c);

                foreach (var partition in toRemove)
                {
                    c.Delete(partition);
                }
            }
        }

        private IEnumerable<FileSystem.Gpt.Partition> GetPartitionsAfterData(GptContext c)
        {
            var gptPartition = c.Find(dataPartition.Guid);
            var indexOfData = c.Partitions.IndexOf(gptPartition);

            var toRemove = c.Partitions
                .Skip(indexOfData + 1)
                .ToList();

            return toRemove;
        }
    }
}
