using System;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.FileSystem;
using Deployer.Services;
using Serilog;

namespace Deployer.Lumia
{
    public class WindowsDeployer : IWindowsDeployer
    {
        private readonly IWindowsOptionsProvider optionsProvider;
        private readonly Phone phone;
        private readonly IWindowsImageService imageService;
        private readonly IBootCreator bootCreator;
        private readonly IObserver<double> progressObserver;

        private static readonly ByteSize ReservedPartitionSize = ByteSize.FromMegaBytes(200);
        private static readonly ByteSize BootPartitionSize = ByteSize.FromMegaBytes(100);
        private const string BootPartitionLabel = "BOOT";
        private const string WindowsPartitonLabel = "WindowsARM";

        public WindowsDeployer(IWindowsOptionsProvider optionsProvider, Phone phone, IWindowsImageService imageService, IBootCreator bootCreator, IObserver<double> progressObserver)
        {
            this.optionsProvider = optionsProvider;
            this.phone = phone;
            this.imageService = imageService;
            this.bootCreator = bootCreator;
            this.progressObserver = progressObserver;
        }

        public async Task Deploy()
        {
            await phone.RemoveExistingWindowsPartitions();
            var options = optionsProvider.Options;
            await AllocateSpace(options.SizeReservedForWindows);
            var partitions = await CreatePartitions();
            await imageService.ApplyImage(await phone.GetWindowsVolume(), options.ImagePath, options.ImageIndex, progressObserver);
            await MakeBootable(partitions);
        }

        private async Task MakeBootable(WindowsVolumes volumes)
        {
            Log.Verbose("Making Windows installation bootable...");

            await bootCreator.MakeBootable(volumes.Boot, volumes.Windows);
            await volumes.Boot.Partition.SetGptType(PartitionType.Esp);
            var updatedBootVolume = await phone.GetBootVolume();

            if (updatedBootVolume != null)
            {
                Log.Verbose("We shouldn't be able to get a reference to the Boot volume.");
                Log.Verbose("Updated Boot Volume: {@Volume}", new { updatedBootVolume.Label, updatedBootVolume.Partition, });
                if (!Equals(updatedBootVolume.Partition.PartitionType, PartitionType.Esp))
                {
                    Log.Warning("The system partition should be {Esp}, but it's {ActualType}", PartitionType.Esp, updatedBootVolume.Partition.PartitionType);
                }
            }            
        }

        private async Task<WindowsVolumes> CreatePartitions()
        {
            Log.Verbose("Creating Windows partitions...");

            await (await phone.GetDisk()).CreateReservedPartition((ulong)ReservedPartitionSize.Bytes);

            var bootPartition = await (await phone.GetDisk()).CreatePartition((ulong)BootPartitionSize.Bytes);
            var bootVolume = await bootPartition.GetVolume();
            await bootVolume.Mount();
            await bootVolume.Format(FileSystemFormat.Fat32, BootPartitionLabel);

            var windowsPartition = await (await phone.GetDisk()).CreatePartition(ulong.MaxValue);
            var winVolume = await windowsPartition.GetVolume();
            await winVolume.Mount();
            await winVolume.Format(FileSystemFormat.Ntfs, WindowsPartitonLabel);

            Log.Verbose("Windows Partitions created successfully");

            return new WindowsVolumes(await phone.GetBootVolume(), await phone.GetWindowsVolume());
        }

        private async Task AllocateSpace(ByteSize sizeReservedForWindows)
        {
            Log.Verbose("Verifying the available space...");

            var refreshedDisk = await phone.GetDisk();
            var available = refreshedDisk.Size - refreshedDisk.AllocatedSize;

            Log.Verbose("We will need {Size} of free space for Windows", sizeReservedForWindows);

            if (available < sizeReservedForWindows)
            {
                Log.Warning("There's not enough space in the phone. Trying to take required space from the Data partition");

                await TakeSpaceFromDataPartition(sizeReservedForWindows);
                Log.Verbose("Data partition resized correctly");
            }
            else
            {
                Log.Verbose("We have enough available space to deploy Windows");
            }
        }

        private async Task TakeSpaceFromDataPartition(ByteSize spaceNeeded)
        {
            Log.Verbose("Shrinking Data partition...");

            var dataVolume = await phone.GetDataVolume();
            var phoneDisk = await phone.GetDisk();
            var data = dataVolume.Size;
            var allocated = phoneDisk.AllocatedSize;
            var available = phoneDisk.Size - allocated;
            var newData =  data - (spaceNeeded - available);

            Log.Verbose("Total size allocated: {Size}", allocated);
            Log.Verbose("Space available: {Size}", available);
            Log.Verbose("Space needed: {Size}", spaceNeeded);
            Log.Verbose("'Data' size: {Size}", data);
            Log.Verbose("Calculated new size for the 'Data' partition: {Size}", newData);
            
            Log.Verbose("Resizing 'Data' to {Size}", newData);

            await dataVolume.Partition.Resize(newData);
        }        
    }
}