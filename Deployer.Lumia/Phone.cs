using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Deployer.FileSystem;
using Deployer.Services;
using Serilog;

namespace Deployer.Lumia
{
    public class Phone : Device
    {
        private const string MainOsLabel = "MainOS";

        private static readonly Guid WinPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");
        private Volume efiEspVolume;
        private Volume mainOs;

        public Phone(ILowLevelApi lowLevelApi) : base(lowLevelApi)
        {
        }

        public async Task<Volume> GetEfiespVolume()
        {
            return efiEspVolume ?? (efiEspVolume = await GetVolume("EFIESP"));
        }

        public async Task<Volume> GetMainOsVolume()
        {
            return mainOs ?? (mainOs = await GetVolume(MainOsLabel));
        }

        public async Task<PhoneModel> GetModel()
        {
            var dict = new Dictionary<string, PhoneModel>()
            {
                {"RM-1104", PhoneModel.Lumia950 },
                {"RM-1105", PhoneModel.Lumia950 },
                {"RM-1118", PhoneModel.Lumia950 },
                {"RM-1085", PhoneModel.Lumia950XL },
                {"RM-1116", PhoneModel.Lumia950XL },
            };

            var mainOs = await GetMainOsVolume();
            var dir = Path.Combine(mainOs.RootDir.Name, "DDP", "MMO", "product.dat");
            var rmLine = File.ReadLines(dir).First();
            var parts = rmLine.Split(':');
            var rm = parts[1].ToUpper();

            return dict[rm];
        }

        public async Task<DualBootStatus> GetDualBootStatus()
        {
            Log.Verbose("Getting Dual Boot Status...");

            var isWoaPresent = await IsWoAPresent();
            var isWPhonePresent = await IsWindowsPhonePresent();
            var isOobeFinished = await IsOobeFinished();

            var bootPartition = await GetBootPartition();
            
            var isEnabled = bootPartition != null && Equals(bootPartition.PartitionType, PartitionType.Basic);

            var isCapable = isWoaPresent && isWPhonePresent && isOobeFinished;
            var status = new DualBootStatus(isCapable, isEnabled);

            Log.Verbose("WoA Present: {Value}", isWoaPresent);
            Log.Verbose("Windows 10 Mobile Present: {Value}", isWPhonePresent);
            Log.Verbose("OOBE Finished: {Value}", isOobeFinished);

            Log.Verbose("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        public async Task EnableDualBoot(bool enable)
        {
            var status = await GetDualBootStatus();
            if (!status.CanDualBoot)
            {
                throw new InvalidOperationException("Cannot enable Dual Boot");
            }

            if (status.IsEnabled != enable)
            {
                if (enable)
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

        private async Task EnableDualBoot()
        {
            Log.Verbose("Enabling Dual Boot...");

            var bootPartition = await GetBootPartition();
            await bootPartition.SetGptType(PartitionType.Basic);
            var volume = await GetEfiespVolume();
            var bcdInvoker = new BcdInvoker(volume.GetBcdFullFilename());
            bcdInvoker.Invoke($@"/set {{{WinPhoneBcdGuid}}} description ""Windows 10 Phone""");
            bcdInvoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /addfirst");

            Log.Verbose("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            Log.Verbose("Disabling Dual Boot...");

            var bootVolume = await GetBootVolume();
            await bootVolume.Partition.SetGptType(PartitionType.Esp);
            var bcdInvoker = new BcdInvoker((await GetEfiespVolume()).GetBcdFullFilename());
            bcdInvoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /remove");

            Log.Verbose("Dual Boot disabled");
        }

        public Task<Volume> GetDataVolume()
        {
            return GetVolume("Data");
        }

        public override async Task RemoveExistingWindowsPartitions()
        {
            Log.Verbose("Cleanup of possible previous Windows 10 ARM64 installation...");

            await RemovePartition("Reserved", await (await GetDisk()).GetReservedPartition());
            await RemovePartition("WoA ESP", await GetBootPartition());
            var winVol = await GetWindowsVolume();
            await RemovePartition("WoA", winVol?.Partition);
        }

        public override async Task<Volume> GetBootVolume()
        {
            return await GetVolume("BOOT");
        }
    }
}
