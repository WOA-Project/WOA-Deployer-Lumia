using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Exceptions;
using Deployer.FileSystem;
using Deployer.FileSystem.Gpt;
using Deployer.Services;
using Serilog;
using Zafiro.Core;
using Partition = Deployer.FileSystem.Partition;

namespace Deployer.Lumia
{
    public class Phone : Device, IPhone
    {
        private const string WindowsSystem32BootWinloadEfi = @"windows\system32\boot\winload.efi";

        private static readonly ByteSize MinimumPhoneDiskSize = ByteSize.FromGigaBytes(28);
        private static readonly ByteSize MaximumPhoneDiskSize = ByteSize.FromGigaBytes(34);

        private readonly BcdInvokerFactory bcdInvokerFactory;
        private readonly IPhoneModelInfoReader phoneModelInfoReader;

        public Phone(IDiskApi diskApi, IPhoneModelInfoReader phoneModelInfoReader, BcdInvokerFactory bcdInvokerFactory) :
            base(diskApi)
        {
            this.phoneModelInfoReader = phoneModelInfoReader;
            this.bcdInvokerFactory = bcdInvokerFactory;
        }

        public async Task<PhoneModelInfo> GetModel()
        {
            return phoneModelInfoReader.GetPhoneModel((await GetDeviceDisk()).Number);
        }

        public async Task<DualBootStatus> GetDualBootStatus()
        {
            Log.Verbose("Getting Dual Boot Status...");

            var isWoaPresent = await IsWoAPresent();
            var isWPhonePresent = await IsWindowsPhonePresent();
            var isOobeFinished = await IsOobeFinished();
            var isWinPhoneEntryPresent = await IsWindowsPhoneBcdEntryPresent();

            var bootPartition = await GetSystemPartition();

            var isEnabled = bootPartition != null && Equals(bootPartition.PartitionType, PartitionType.Basic) &&
                            isWinPhoneEntryPresent;

            var isCapable = isWoaPresent && isWPhonePresent && isOobeFinished;
            var status = new DualBootStatus(isCapable, isEnabled);

            Log.Verbose("WOA Present: {Value}", isWoaPresent);
            Log.Verbose("Windows 10 Mobile Present: {Value}", isWPhonePresent);
            Log.Verbose("OOBE Finished: {Value}", isOobeFinished);

            Log.Verbose("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        public override async Task<Partition> GetSystemPartition()
        {
            var disk = await GetDeviceDisk();
            return await disk.GetRequiredPartition(PartitionName.System);
        }

        public async Task ToogleDualBoot(bool isEnabled, bool force = false)
        {
            var status = await GetDualBootStatus();

            if (!force && !status.CanDualBoot)
            {
                throw new InvalidOperationException("Cannot enable Dual Boot");
            }

            if (status.IsEnabled != isEnabled)
            {
                if (isEnabled)
                {
                    await EnableDualBoot();
                }
                else
                {
                    await DisableDualBoot();
                }
            }
            else
            {
                Log.Debug("Dual Boot status will not change");
            }
        }

        public override async Task<Disk> GetDeviceDisk()
        {
            var disk = await GetDeviceDiskCore();
            if (disk.IsOffline)
            {
                throw new ApplicationException("The phone disk is offline. Please, set it online with Disk Management or DISKPART.");
            }

            return disk;
        }

        private  async Task<Disk> GetDeviceDiskCore()
        {
            var disks = await DiskApi.GetDisks();

            var disk = await disks
                .ToObservable()
                .SelectMany(async x => new { IsDevice = await IsDeviceDisk(x), Disk = x})
                .Where(x => x.IsDevice)
                .Select(x => x.Disk)
                .FirstOrDefaultAsync();

            if (disk != null)
            {
                return disk;
            }

            throw new PhoneDiskNotFoundException(
                "Cannot get the Phone Disk. Please, verify that the Phone is in Mass Storage Mode.");
        }

        private static async Task<bool> IsDeviceDisk(Disk disk)
        {           
            var hasCorrectSize = HasCorrectSize(disk);

            if (!hasCorrectSize)
            {
                return false;
            }

            var diskNames = new[] {"VEN_QUALCOMM&PROD_MMC_STORAGE", "VEN_MSFT&PROD_PHONE_MMC_STOR"};
            var hasCorrectDiskName = diskNames.Any(name => disk.UniqueId.Contains(name));

            if (hasCorrectDiskName)
            {
                return true;
            }

            var partitions = await disk.GetPartitions();
            var names = partitions.Select(x => x.Name);
            var lookup = new[] {"EFIESP", "TZAPPS", "DPP"};

            return lookup.IsSubsetOf(names);
        }

        public override Task<Volume> GetWindowsVolume()
        {
            return this.GetVolumeByPartitionName(PartitionName.Windows);
        }

        protected override async Task<bool> IsWoAPresent()
        {
            var disk = await GetDeviceDisk();
            using (var context = await GptContextFactory.Create(disk.Number, FileAccess.Read))
            {
                return context.Get(PartitionName.Data) != null && context.Get(PartitionName.System) != null;
            }
        }

        public override async Task<Volume> GetSystemVolume()
        {
            return await (await GetDeviceDisk()).GetVolumeByPartitionName(PartitionName.System);
        }

        public async Task<bool> IsWindowsPhonePresent()
        {
            var disk = await GetDeviceDisk();
            using (var context = await GptContextFactory.Create(disk.Number, FileAccess.Read))
            {
                return context.Get(PartitionName.MainOs) != null && context.Get(PartitionName.Data) != null;
            }
        }

        private async Task<IBcdInvoker> GetBcdInvoker()
        {
            var volume = await this.GetVolumeByPartitionName(PartitionName.EfiEsp);
            var bcdFullFilename = volume.Root.CombineRelativeBcdPath();
            return bcdInvokerFactory.Create(bcdFullFilename);
        }

        private async Task<bool> IsWindowsPhoneBcdEntryPresent()
        {
            var invoker = await GetBcdInvoker();
            var result = invoker.Invoke();

            var containsWinLoad = result.Contains(WindowsSystem32BootWinloadEfi, StringComparison.CurrentCultureIgnoreCase);
            var containsWinPhoneBcdGuid = result.Contains(BcdGuids.WinMobile.ToString(), StringComparison.InvariantCultureIgnoreCase);

            return containsWinLoad && containsWinPhoneBcdGuid;

        }

        private async Task EnableDualBoot()
        {
            Log.Verbose("Enabling Dual Boot...");

            var systemPartition = await GetSystemPartition();
            await systemPartition.SetGptType(PartitionType.Basic);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} description ""Windows 10 Phone""");
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} path ""\windows\system32\boot\winload.efi""");
            invoker.Invoke($@"/default {{{BcdGuids.WinMobile}}}");

            Log.Verbose("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            Log.Verbose("Disabling Dual Boot...");

            var systemPartition = await GetSystemPartition();
            await systemPartition.SetGptType(PartitionType.Esp);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} description ""Dummy, please ignore""");
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} path ""dummy""");
            invoker.Invoke($@"/default {{{BcdGuids.Woa}}}");
            Log.Verbose("Dual Boot disabled");
        }

        private static bool HasCorrectSize(Disk disk)
        {
            var moreThanMinimum = disk.Size > MinimumPhoneDiskSize;
            var lessThanMaximum = disk.Size < MaximumPhoneDiskSize;
            return moreThanMinimum && lessThanMaximum;
        }
    }
}