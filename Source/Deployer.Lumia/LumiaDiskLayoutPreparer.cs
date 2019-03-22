using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Exceptions;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Deployer.Tasks;
using Serilog;

namespace Deployer.Lumia
{
    public class LumiaDiskLayoutPreparer : IDiskLayoutPreparer
    {
        private readonly IWindowsOptionsProvider optionsProvider;
        private readonly IFileSystemOperations fileOperations;
        private readonly IEnumerable<ISpaceAllocator<IPhone>> spaceAllocators;
        private readonly IPhone phone;
        private Disk disk;

        private readonly ByteSize reservedSize = ByteSize.FromMegaBytes(16);
        private readonly ByteSize systemSize = ByteSize.FromMegaBytes(100);
        private readonly ByteSize recoverySize = ByteSize.FromMegaBytes(500);

        public LumiaDiskLayoutPreparer(IWindowsOptionsProvider optionsProvider, IFileSystemOperations fileOperations, IEnumerable<ISpaceAllocator<IPhone>> spaceAllocators, IPhone phone)
        {
            this.optionsProvider = optionsProvider;
            this.fileOperations = fileOperations;
            this.spaceAllocators = spaceAllocators;
            this.phone = phone;
        }

        public async Task Prepare(Disk diskToPrepare)
        {
            disk = diskToPrepare;

            try
            {
                await RemoveExistingPartitions();
                await AllocateSpace(optionsProvider.Options.SizeReservedForWindows);
                await CreatePartitions();
                await FormatPartitions();
                await PatchBoot();
            }
            catch (Exception e)
            {
                Log.Error(e, "Phone disk preparation failed");
                throw new ApplicationException("Phone disk preparation failed. Cannot prepare the phone for the deployment", e);
            }
        }

        private async Task AllocateSpace(ByteSize requiredSize)
        {
            Log.Verbose("Verifying the available space...");

            Log.Verbose("We will need {Size} of free space for Windows", requiredSize);

            var hasEnoughSpace = await phone.HasEnoughSpace(requiredSize);
            if (!hasEnoughSpace)
            {
                Log.Verbose("There's not enough space in the phone. We will try to allocate it automatically");

                var success = await spaceAllocators.ToObservable()
                    .Select(x => Observable.FromAsync(() => x.TryAllocate(phone, requiredSize)))
                    .Merge(1)
                    .Any(successful => successful);

                if (!success)
                {
                    Log.Verbose("Allocation attempt failed");
                    throw new NotEnoughSpaceException($"Could not allocate {requiredSize} on the phone. Please, try to allocate the necessary space manually and retry.");
                }
                
                Log.Verbose("Space allocated correctly");
            }
            else
            {
                Log.Verbose("We have enough available space to deploy Windows");
            }
        }

        private async Task RemoveExistingPartitions()
        {
            RemoveExistingPartitionsByName();
            await RemovePartitionsAfterData();
            await disk.Refresh();
        }

        private async Task RemovePartitionsAfterData()
        {
            Log.Verbose("Trying to remove partitions created by previous versions of WOA Deployer");

            var volume = await phone.GetDataVolume();

            if (volume == null)
            {
                Log.Verbose("Data partition not found. Partition cleanup will be performed.");
                return;
            }

            var dataPartition = volume.Partition;
            var windowsPartNumber = (int) dataPartition.Number;
            var partitions = await disk.GetPartitions();

            var toRemove = partitions
                .Skip(windowsPartNumber + 1);

            Log.Verbose("Removing legacy partitions");
            foreach (var partition in toRemove)
            {
                await partition.Remove();
            }
        }

        private async Task PatchBoot()
        {
            var efiEsp = await phone.GetEfiEspVolume();
            await fileOperations.Copy("Core\\Boot\\bootaa64.efi", Path.Combine(efiEsp.Root, "EFI", "Boot\\"));
        }

        private async Task FormatPartitions()
        {
            using (var transaction = new GptContext(disk.Number, FileAccess.Read))
            {
                await transaction.Get(PartitionName.System).AsCommon(disk).Format(FileSystemFormat.Fat32, PartitionName.System);
                await transaction.Get(PartitionName.Windows).AsCommon(disk).Format(FileSystemFormat.Ntfs, PartitionName.Windows);
                await transaction.Get(PartitionName.Recovery).AsCommon(disk).Format(FileSystemFormat.Fat32, PartitionName.Recovery);
            }
        }

        private async Task CreatePartitions()
        {
            using (var t = new GptContext(disk.Number, FileAccess.ReadWrite))
            {
                t.Add(new EntryBuilder(PartitionName.System, systemSize, PartitionType.Esp)
                    .NoAutoMount()
                    .Build());

                t.Add(new EntryBuilder(PartitionName.Reserved, reservedSize, PartitionType.Reserved)
                    .NoAutoMount()
                    .Build());

                var windowsSize = t.AvailableSize - recoverySize;
                t.Add(new EntryBuilder(PartitionName.Windows, windowsSize, PartitionType.Basic)
                    .Build());

                t.Add(new EntryBuilder(PartitionName.Recovery, recoverySize, PartitionType.Recovery)
                    .NoAutoMount()
                    .MarkAsCritical()
                    .Build());
            }

            await disk.Refresh();
        }

        private void RemoveExistingPartitionsByName()
        {
            using (var t = new GptContext(disk.Number, FileAccess.ReadWrite))
            {
                t.RemoveExisting(PartitionName.System);
                t.RemoveExisting(PartitionName.Reserved);
                t.RemoveExisting(PartitionName.Windows);
                t.RemoveExisting(PartitionName.Recovery);
            }
        }
    }
}