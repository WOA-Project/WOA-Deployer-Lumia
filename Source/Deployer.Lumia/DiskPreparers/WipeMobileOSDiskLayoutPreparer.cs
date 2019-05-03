using System.IO;
using System.Threading.Tasks;
using Deployer.FileSystem.Gpt;
using Deployer.Tasks;
using Grace.DependencyInjection.Attributes;

namespace Deployer.Lumia.DiskPreparers
{
    [Metadata("Name", "Overwrite (Wipe) Windows 10 Mobile")]
    [Metadata("Order", 1)]
    public class WipeMobileOSDiskLayoutPreparer : LumiaDiskLayoutPreparer
    {
        public WipeMobileOSDiskLayoutPreparer(IDeploymentContext context, IPartitionCleaner cleaner) : base(context, cleaner)
        {
        }

        protected override async Task AllocateSpace()
        {
            var deviceDisk = await Phone.GetDeviceDisk();
            using (var context = await GptContextFactory.Create(deviceDisk.Number, FileAccess.ReadWrite))
            {
                context.RemoveExisting(PartitionName.Data);
                context.RemoveExisting(PartitionName.MainOs);
            }            
        }
    }
}