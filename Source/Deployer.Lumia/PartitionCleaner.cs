using System;
using System.Collections.Generic;
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

            if (dataPartition == null)
            {
                Log.Verbose("Data partition not found. Skipping cleanup.");
                return;
            }

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
             
                if (!string.Equals(last.Name, PartitionName.Data, StringComparison.InvariantCultureIgnoreCase))
                {                   
                    throw new PartitioningException($"The label of the last partition should be '{PartitionName.Data}' and it's '{last.Name}'");
                }
            }
        }

        private async Task RemoveAnyPartitionsAfterData()
        {
            Log.Verbose("Removing all the partitions after the Data partition");

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
