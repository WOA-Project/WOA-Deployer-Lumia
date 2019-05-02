using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.FileSystem;
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
            var mainOsVolume = await phone.GetMainOsVolume();
            var mainOsPath = mainOsVolume.Root;
            destinationFolder = Path.Combine(mainOsPath, PartitionName.EfiEsp, "Windows", "System32", "BOOT");

            var shouldIinstall = !IsAlreadyInstalled();

            if (shouldIinstall)
            {
                await CopyDevMenuFiles(mainOsPath);                
            }

            ConfigureBcd(mainOsPath);

            if (shouldIinstall)
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

        private async Task CopyDevMenuFiles(string mainOsPath)
        {            
            await fileSystemOperations.CopyDirectory(Path.Combine(rootFilesPath), destinationFolder);
        }

        private void ConfigureBcd(string mainOsPath)
        {
            var bcdPath = Path.Combine(mainOsPath, PartitionName.EfiEsp.CombineRelativeBcdPath());
            var efiEspPath = Path.Combine(mainOsPath, PartitionName.EfiEsp);
            var bcdInvoker = bcdInvokerFactory.Create(bcdPath);
            var guid = FormattingUtils.GetGuid(bcdInvoker.Invoke(@"/create /d ""Developer Menu"" /application BOOTAPP"));
            bcdInvoker.Invoke($@"/set {{{guid}}} path \Windows\System32\BOOT\developermenu.efi");
            bcdInvoker.Invoke($@"/set {{{guid}}} device partition={efiEspPath}");
            bcdInvoker.Invoke($@"/set {{{guid}}} testsigning on");
            bcdInvoker.Invoke($@"/set {{{guid}}} nointegritychecks on");
            bcdInvoker.Invoke($@"/displayorder {{{guid}}} /addlast");
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