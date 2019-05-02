using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.Exceptions;
using Deployer.FileSystem;
using Deployer.Services;
using Serilog;

namespace Deployer.Lumia
{
    public class Phone : Device, IPhone
    {
        private const string WindowsSystem32BootWinloadEfi = @"windows\system32\boot\winload.efi";
        private static readonly Guid WinPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");
        private static readonly Guid WoaBcdGuid = Guid.Parse("7619dcca-fafe-11d9-b411-000476eba25f");

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

            Log.Verbose("WoA Present: {Value}", isWoaPresent);
            Log.Verbose("Windows 10 Mobile Present: {Value}", isWPhonePresent);
            Log.Verbose("OOBE Finished: {Value}", isOobeFinished);

            Log.Verbose("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        public override async Task<Partition> GetSystemPartition()
        {
            var disk = await GetDeviceDisk();
            return await disk.GetPartition(PartitionName.System);
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

        public Task<Volume> GetDataVolume()
        {
            return GetVolumeByPartitionName(PartitionName.Data);
        }

        public Task<Volume> GetMainOsVolume()
        {
            return GetVolumeByPartitionName(PartitionName.MainOs);
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
            foreach (var disk in disks.Where(x => x.Number != 0))
            {
                var hasCorrectSize = HasCorrectSize(disk);

                if (hasCorrectSize)
                {
                    var mainOs = await disk.GetPartition(PartitionName.MainOs);
                    if (mainOs != null)
                    {
                        return disk;
                    }
                }
            }

            throw new PhoneDiskNotFoundException(
                "Cannot get the Phone Disk. Please, verify that the Phone is in Mass Storage Mode.");
        }

        public override Task<Volume> GetWindowsVolume()
        {
            return GetVolumeByPartitionName(PartitionName.Windows);
        }

        public override async Task<Volume> GetSystemVolume()
        {
            return await GetVolumeByPartitionName(PartitionName.System);
        }

        private async Task<bool> IsWindowsPhonePresent()
        {
            try
            {
                await GetWindowsVolume();
                await GetDataVolume();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get Windows Phone's volumes");
                return false;
            }

            return true;
        }

        private async Task<IBcdInvoker> GetBcdInvoker()
        {
            var volume = await GetMainOsVolume();
            var bcdFullFilename = Path.Combine(volume.Root, PartitionName.EfiEsp.CombineRelativeBcdPath());
            return bcdInvokerFactory.Create(bcdFullFilename);
        }

        private async Task<bool> IsWindowsPhoneBcdEntryPresent()
        {
            var invoker = await GetBcdInvoker();
            var result = invoker.Invoke();

            var containsWinLoad = result.Contains(WindowsSystem32BootWinloadEfi, StringComparison.CurrentCultureIgnoreCase);
            var containsWinPhoneBcdGuid = result.Contains(WinPhoneBcdGuid.ToString(), StringComparison.InvariantCultureIgnoreCase);

            return containsWinLoad && containsWinPhoneBcdGuid;
        }

        private async Task EnableDualBoot()
        {
            Log.Verbose("Enabling Dual Boot...");

            var systemPartition = await GetSystemPartition();
            await systemPartition.SetGptType(PartitionType.Basic);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} description ""Windows 10 Phone""");
            invoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} path ""\windows\system32\boot\winload.efi""");
            invoker.Invoke($@"/default {{{WinPhoneBcdGuid}}}");

            Log.Verbose("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            Log.Verbose("Disabling Dual Boot...");

            var systemPartition = await GetSystemPartition();
            await systemPartition.SetGptType(PartitionType.Esp);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} description ""Dummy, please ignore""");
            invoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} path ""dummy""");
            invoker.Invoke($@"/default {{{WoaBcdGuid}}}");
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
