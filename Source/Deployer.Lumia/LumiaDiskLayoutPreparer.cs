using System;
using System.IO;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Deployer.Tasks;
using Serilog;

namespace Deployer.Lumia
{
    public abstract class LumiaDiskLayoutPreparer : IDiskLayoutPreparer
    {
        private readonly IDeploymentContext context;
        private readonly IExistingDeploymentCleaner cleaner;
        private IDisk disk;

        private readonly ByteSize reservedSize = ByteSize.FromMegaBytes(16);
        private readonly ByteSize systemSize = ByteSize.FromMegaBytes(100);

        protected LumiaDiskLayoutPreparer(IDeploymentContext context, IExistingDeploymentCleaner cleaner)
        {
            this.context = context;
            this.cleaner = cleaner;
        }

        public async Task Prepare(IDisk diskToPrepare)
        {
            disk = diskToPrepare;

            try
            {
                await RemoveExistingPartitions();
                await AllocateSpace();
                await CreatePartitions();
                await FormatPartitions();
            }
            catch (Exception e)
            {
                Log.Error(e, "Phone disk preparation failed");
                throw new ApplicationException("Phone disk preparation failed. Cannot prepare the phone for the deployment", e);
            }
        }

        protected abstract Task AllocateSpace();

        protected IPhone Phone => (IPhone) context.Device;

        private async Task RemoveExistingPartitions()
        {
            await cleaner.Clean(Phone);
        }

        private async Task FormatPartitions()
        {
            Log.Information("Formatting partitions");

            await Format(PartitionName.System, FileSystemFormat.Fat32, PartitionName.System);
            await Format(PartitionName.Windows, FileSystemFormat.Ntfs, PartitionName.Windows);
        }

        private async Task Format(string partitionName, FileSystemFormat fileSystemFormat, string label)
        {
            var part = await disk.GetPartitionByName(partitionName);
            await part.EnsureWritable();
            var vol = await part.GetVolume();
            await vol.Format(fileSystemFormat, label);
        }

        private async Task CreatePartitions()
        {
            Log.Verbose("Creating partitions");

            using (var t = await GptContextFactory.Create(disk.Number, FileAccess.ReadWrite))
            {
                t.Add(new EntryBuilder(PartitionName.System, systemSize, PartitionType.Esp)
                    .NoAutoMount()
                    .Build());

                t.Add(new EntryBuilder(PartitionName.Reserved, reservedSize, PartitionType.Reserved)
                    .NoAutoMount()
                    .Build());

                var windowsSize = t.AvailableSize;
                t.Add(new EntryBuilder(PartitionName.Windows, windowsSize, PartitionType.Basic)
                    .NoAutoMount()
                    .Build());
            }

            await disk.Refresh();
        }
    }
}