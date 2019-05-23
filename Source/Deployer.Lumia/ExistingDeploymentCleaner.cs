using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Serilog;

namespace Deployer.Lumia
{
    public class ExistingDeploymentCleaner : IExistingDeploymentCleaner
    {
        private IDisk disk;

        public async Task Clean(IPhone toClean)
        {
            Log.Information("Performing cleanup of possible existing deployments");

            disk = await toClean.GetDeviceDisk();

            using (var context = await GptContextFactory.Create(disk.Number, FileAccess.ReadWrite))
            {
                context.RemoveExisting(PartitionName.System);
                context.RemoveExisting(PartitionName.Reserved);
                context.RemoveExisting(PartitionName.Windows);

                RemovePartitionsAfterData(context);
            }
         
            Log.Information("Cleanup done");

            await disk.Refresh();
        }

        private static void RemovePartitionsAfterData(GptContext context)
        {
            var toRemove = GetPartitionsAfterData(PartitionName.Data, context);
            foreach (var partition in toRemove)
            {
                context.Delete(partition);
            }
        }

        private static IEnumerable<Partition> GetPartitionsAfterData(string partitionName, GptContext c)
        {
            var data = c.Get(partitionName);
            var indexOfData = c.Partitions.IndexOf(data);

            var toRemove = c.Partitions
                .Skip(indexOfData + 1)
                .ToList();

            return toRemove;
        }
    }
}
