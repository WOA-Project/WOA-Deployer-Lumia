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
        private readonly ITooling tooling;
        private readonly IDeploymentContext context;
        private readonly IFileSystemOperations fileSystemOperations;
        private readonly IScriptRunner scriptRunner;
        private readonly IScriptParser parser;

        public WoaDeployer(IScriptRunner scriptRunner, IScriptParser parser, ITooling tooling,
            IDeploymentContext context,
            IFileSystemOperations fileSystemOperations)
        {
            this.scriptRunner = scriptRunner;
            this.parser = parser;
            this.tooling = tooling;
            this.context = context;
            this.fileSystemOperations = fileSystemOperations;
        }

        public IPhone Phone => (IPhone) context.Device;

        public async Task Deploy()
        {
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
            await SaveMetadata();
            await PreparePhoneDiskForSafeRemoval();
        }

        private async Task EnsureFullyUnlocked()
        {
            var backUpEfiEsp = await context.Device.GetOptionalPartition(PartitionName.BackupEfiesp);
            if (backUpEfiEsp != null)
            {
                throw new InvalidOperationException("Your phone isn't fully unlocked! Please, return to WPInternals and complete the unlock process.");
            }
        }

        private async Task SaveMetadata()
        {
            var dirInfo = new DirectoryInfo("Downloaded");
            var metadatas = dirInfo.GetFiles("Info.json", SearchOption.AllDirectories);
            foreach (var metadata in metadatas)
            {
                var windowsVolume = await context.Device.GetWindowsVolume();
                if (metadata.Directory != null)
                {
                    var name = metadata.Directory.Name;
                    var destination = Path.Combine(windowsVolume.Root, "Windows", "Logs", "WOA-Deployer", $"{name}.json");
                    await fileSystemOperations.Copy(metadata.FullName, destination);
                }
                else
                {
                    Log.Debug("Cannot write metadata on the phone");
                }
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
                        var dest = Path.Combine((await Phone.GetSystemVolume()).Root, "EFI", "Boot") + Path.DirectorySeparatorChar;
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
            await disk.Refresh();
        }

        public Task ToggleDualBoot(bool isEnabled)
        {
            return tooling.ToogleDualBoot(isEnabled);
        }
    }
}