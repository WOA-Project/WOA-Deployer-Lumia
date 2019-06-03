using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Services.Wim;
using Deployer.Tasks;
using Serilog;

namespace Deployer.Lumia
{
    public class WoaDeployer : IWoaDeployer
    {
        private readonly IScriptRunner scriptRunner;
        private readonly IScriptParser parser;
        private readonly ITooling tooling;
        private IDeploymentContext context;
        private readonly IFileSystemOperations fileSystemOperations;

        public WoaDeployer(IScriptRunner scriptRunner, IScriptParser parser, ITooling tooling,
            IFileSystemOperations fileSystemOperations)
        {
            this.scriptRunner = scriptRunner;
            this.parser = parser;
            this.tooling = tooling;
            this.fileSystemOperations = fileSystemOperations;
        }

        private IPhone Phone => (IPhone)context.Device;

        public async Task Deploy(IDeploymentContext deploymentContext)
        {
            this.context = deploymentContext;

            await EnsureFullyUnlocked();
            
            var dict = new Dictionary<(PhoneModel, Variant), string>
            {
                {(PhoneModel.Talkman, Variant.SingleSim), Path.Combine("Scripts", "Talkman", "SingleSim.txt")},
                {(PhoneModel.Cityman, Variant.SingleSim), Path.Combine("Scripts", "Cityman", "SingleSim.txt")},
                {(PhoneModel.Talkman, Variant.DualSim), Path.Combine("Scripts", "Talkman", "DualSim.txt")},
                {(PhoneModel.Cityman, Variant.DualSim), Path.Combine("Scripts", "Cityman", "DualSim.txt")},
            };

            var phoneModel = await Phone.GetModel();
            Log.Verbose("{Model} detected", phoneModel);
            var path = dict[(phoneModel.Model, phoneModel.Variant)];

            await scriptRunner.Run(parser.Parse(File.ReadAllText(path)));
            await PatchBootManagerIfNeeded();
            await MoveMetadataToPhone();
            await PreparePhoneDiskForSafeRemoval();
        }

        private async Task EnsureFullyUnlocked()
        {
            var backUpEfiEsp = await context.Device.GetPartitionByName(PartitionName.BackupEfiesp);
            if (backUpEfiEsp != null)
            {
                throw new InvalidOperationException("Your phone isn't fully unlocked! Please, return to WPInternals and complete the unlock process.");
            }
        }

        private async Task MoveMetadataToPhone()
        {
            try
            {
                var windowsVolume = await context.Device.GetWindowsPartition();
                var destination = Path.Combine(windowsVolume.Root, "Windows", "Logs", "WOA-Deployer");
                await fileSystemOperations.CopyDirectory(AppPaths.Metadata, destination);
                await fileSystemOperations.DeleteDirectory(Path.Combine(AppPaths.Metadata, "Injected Drivers"));
            }
            catch (Exception e)
            {
                Log.Error(e,"Cannot write metadata");
            }
        }

        private async Task PatchBootManagerIfNeeded()
        {
            Log.Debug("Checking if we have to patch WOA's Boot Manager");
            var options = context.DeploymentOptions;
            using (var file = File.OpenRead(options.ImagePath))
            {
                var imageReader = new WindowsImageMetadataReader();
                var windowsImageInfo = imageReader.Load(file);

                var selectedImage = options.ImageIndex - 1;
                if (int.TryParse(windowsImageInfo.Images[selectedImage].Build, out var buildNumber))
                {
                    if (buildNumber == 17763)
                    {
                        Log.Verbose("Build 17763 detected. Patching Boot Manager.");
                        var dest = Path.Combine((await Phone.GetSystemPartition()).Root, "EFI", "Boot") + Path.DirectorySeparatorChar;
                        await fileSystemOperations.Copy(@"Core\Boot\bootaa64.efi", dest);
                        Log.Verbose("Boot Manager Patched.");
                    }
                }
            }
        }

        private async Task PreparePhoneDiskForSafeRemoval()
        {
            Log.Information("# Preparing phone for safe removal");
            Log.Information("Please wait...");
            var disk = await Phone.GetDeviceDisk();
            await disk.PrepareForRemoval();
        }

        public Task ToggleDualBoot(bool isEnabled)
        {
            return tooling.ToogleDualBoot(isEnabled);
        }
    }
}