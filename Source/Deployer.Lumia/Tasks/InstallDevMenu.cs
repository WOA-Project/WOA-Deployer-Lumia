using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.FileSystem;
using Deployer.Services;
using Deployer.Utils;

namespace Deployer.Lumia.Tasks
{
    [TaskDescription("Installing Development Menu")]
    public class InstallDevMenu : IDeploymentTask
    {
        private const string DevMenuName = "developermenu.efi";
        private readonly string rootFilesPath;
        private readonly IPhone phone;
        private readonly IBcdInvokerFactory bcdInvokerFactory;
        private readonly IFileSystemOperations fileSystemOperations;
        private readonly IPrompt prompt;
        private string destinationFolder;
        private string bcdPath;
        private string efiEspPath;
        private IBcdInvoker bcdInvoker;

        public InstallDevMenu(string rootFilesPath, IPhone phone, IBcdInvokerFactory bcdInvokerFactory, IFileSystemOperations fileSystemOperations, IPrompt prompt)
        {
            this.rootFilesPath = rootFilesPath;
            this.phone = phone;
            this.bcdInvokerFactory = bcdInvokerFactory;
            this.fileSystemOperations = fileSystemOperations;
            this.prompt = prompt;
        }

        public async Task Execute()
        {
            var efiEspVolume = await phone.GetVolumeByPartitionName(PartitionName.EfiEsp);
            efiEspPath = efiEspVolume.Root;
            destinationFolder = Path.Combine(efiEspVolume.Root, "Windows", "System32", "BOOT");
            bcdPath = efiEspVolume.Root.CombineRelativeBcdPath();
            bcdInvoker = bcdInvokerFactory.Create(bcdPath);

            var shouldInstall = !IsAlreadyInstalled();

            if (shouldInstall)
            {
                await CopyDevMenuFiles();                
            }

            ConfigureBcd();

            if (shouldInstall)
            {
                await prompt.PickOptions(Resources.DeveloperMenuInstalled, new List<Option>()
                {
                    new Option("Continue", DialogValue.OK),
                });
            }
        }

        private bool IsAlreadyInstalled()
        {
            var existingFile = Path.Combine(destinationFolder, DevMenuName);
            if (!fileSystemOperations.FileExists(existingFile))
            {
                return false;
            }

            var newFile = Path.Combine("Core", "Developer Menu", DevMenuName);
            return string.Equals(Checksum(existingFile), Checksum(newFile));
        }

        private async Task CopyDevMenuFiles()
        {            
            await fileSystemOperations.CopyDirectory(Path.Combine(rootFilesPath), destinationFolder);
        }

        private void ConfigureBcd()
        {
            bcdInvoker.SafeCreate(BcdGuids.DevMenu,@"/d ""Developer Menu"" /application BOOTAPP");
            bcdInvoker.Invoke($@"/set {{{BcdGuids.DevMenu}}} path \Windows\System32\BOOT\developermenu.efi");
            bcdInvoker.Invoke($@"/set {{{BcdGuids.DevMenu}}} device partition={efiEspPath}");
            bcdInvoker.Invoke($@"/set {{{BcdGuids.DevMenu}}} testsigning on");
            bcdInvoker.Invoke($@"/set {{{BcdGuids.DevMenu}}} nointegritychecks on");
            bcdInvoker.Invoke($@"/displayorder {{{BcdGuids.DevMenu}}} /addlast");
        }

        private static string Checksum(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }
    }
}