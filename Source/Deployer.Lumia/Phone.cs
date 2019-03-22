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
        private static readonly ByteSize MinimumPhoneDiskSize = ByteSize.FromGigaBytes(28);
        private static readonly ByteSize MaximumPhoneDiskSize = ByteSize.FromGigaBytes(34);

        private static readonly Guid WinPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");
        private static readonly string DefaultBcdText = "{Default}";
        private readonly BcdInvokerFactory bcdInvokerFactory;
        private readonly IPhoneModelReader phoneModelReader;
        private IBcdInvoker bcdInvoker;
        private Volume efiEspVolume;

        public Phone(IDiskApi diskApi, IPhoneModelReader phoneModelReader, BcdInvokerFactory bcdInvokerFactory) :
            base(diskApi)
        {
            this.phoneModelReader = phoneModelReader;
            this.bcdInvokerFactory = bcdInvokerFactory;
        }

        public async Task<PhoneModel> GetModel()
        {
            return phoneModelReader.GetPhoneModel((await GetDeviceDisk()).Number);
        }

        public async Task<DualBootStatus> GetDualBootStatus()
        {
            Log.Verbose("Getting Dual Boot Status...");

            var isWoaPresent = await IsWoAPresent();
            var isWPhonePresent = await IsWindowsPhonePresent();
            var isOobeFinished = await IsOobeFinished();
            var isWinPhoneEntryPresent = await LookupStringInBcd(WinPhoneBcdGuid.ToString());
            var isWinDefaultEntryPresent = await LookupStringInBcd(DefaultBcdText);
            var isPresentInBcd = isWinPhoneEntryPresent || isWinDefaultEntryPresent;

            var bootPartition = await this.GetBootPartition();

            var isEnabled = bootPartition != null && Equals(bootPartition.PartitionType, PartitionType.Basic) &&
                            isPresentInBcd;

            var isCapable = isWoaPresent && isWPhonePresent && isOobeFinished;
            var status = new DualBootStatus(isCapable, isEnabled);

            Log.Verbose("WoA Present: {Value}", isWoaPresent);
            Log.Verbose("Windows 10 Mobile Present: {Value}", isWPhonePresent);
            Log.Verbose("OOBE Finished: {Value}", isOobeFinished);

            Log.Verbose("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        public async Task ToogleDualBoot(bool isEnabled)
        {
            var status = await GetDualBootStatus();
            if (!status.CanDualBoot)
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
            return GetVolumeByLabel(VolumeName.Data);
        }

        public Task<Volume> GetMainOsVolume()
        {
            return GetVolumeByLabel(VolumeName.MainOs);
        }

        public Task<Volume> GetEfiEspVolume()
        {
            return GetVolumeByLabel(VolumeName.EfiEsp);
        }

        public override async Task<Disk> GetDeviceDisk()
        {
            var disks = await DiskApi.GetDisks();
            foreach (var disk in disks.Where(x => x.Number != 0))
            {
                var hasCorrectSize = HasCorrectSize(disk);

                if (hasCorrectSize)
                {
                    var mainOs = await disk.GetVolumeByLabel(VolumeName.MainOs);
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

        public async Task<Volume> GetEfiespVolume()
        {
            return efiEspVolume ?? (efiEspVolume = await GetVolumeByLabel(VolumeName.EfiEsp));
        }

        public async Task<IBcdInvoker> GetBcdInvoker()
        {
            if (bcdInvoker != null)
            {
                return bcdInvoker;
            }

            var volume = await GetMainOsVolume();
            var bcdFullFilename = Path.Combine(volume.Root, "EFIESP", "EFI", "Microsoft", "Boot", "BCD");
            bcdInvoker = bcdInvokerFactory.Create(bcdFullFilename);
            return bcdInvoker;
        }

        private async Task<bool> LookupStringInBcd(string str)
        {
            var invoker = await GetBcdInvoker();
            var result = invoker.Invoke();
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(result, str, CompareOptions.IgnoreCase) >= 0;
        }

        private async Task EnableDualBoot()
        {
            Log.Verbose("Enabling Dual Boot...");

            await this.EnsureBootPartitionIs(PartitionType.Basic);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} description ""Windows 10 Phone""");
            invoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /addfirst");
            invoker.Invoke($@"/default {{{WinPhoneBcdGuid}}}");

            Log.Verbose("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            Log.Verbose("Disabling Dual Boot...");

            await this.EnsureBootPartitionIs(PartitionType.Esp);

            var invoker = await GetBcdInvoker();
            invoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /remove");

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