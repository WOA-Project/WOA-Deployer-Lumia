using System.IO;
using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Serilog;

namespace Deployer.Lumia
{
    public class PartitionCleaner : IPartitionCleaner
    {
        private Disk disk;

        public async Task Clean(IPhone toClean)
        {
            Log.Information("Performing partition cleanup");

            disk = await toClean.GetDeviceDisk();

            using (var context = await GptContextFactory.Create(disk.Number, FileAccess.ReadWrite))
            {
                context.RemoveExisting(PartitionName.System);
                context.RemoveExisting(PartitionName.Reserved);
                context.RemoveExisting(PartitionName.Windows);
            }
         
            Log.Information("Cleanup done");

            await disk.Refresh();
        }
    }
}
