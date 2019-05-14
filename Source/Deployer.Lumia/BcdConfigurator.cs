using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.Services;

namespace Deployer.Lumia
{
    public class BcdConfigurator
    {
        private readonly IBcdInvoker invoker;
        private readonly Volume efiEspVolume;


        public BcdConfigurator(IBcdInvoker invoker, Volume efiEspVolume)
        {
            this.invoker = invoker;
            this.efiEspVolume = efiEspVolume;
        }

        public async Task SetupBcd()
        {
            await SetupBootShim();
            await SetupDummy();
            await SetupBootMgr();
            await SetDisplayOptions();
        }

        private async Task SetupDummy()
        {
            await invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} path dummy");
            await invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} description ""Dummy, please ignore""");
        }

        private async Task SetDisplayOptions()
        {
            await invoker.Invoke($@"/displayorder {{{BcdGuids.Woa}}}");
            await invoker.Invoke($@"/displayorder {{{BcdGuids.WinMobile}}} /addlast");
            await invoker.Invoke($@"/default {{{BcdGuids.Woa}}}");
            await invoker.Invoke($@"/timeout 30");
        }

        private async Task SetupBootShim()
        {
            await EnsureBootShim();

            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} path \EFI\boot\BootShim.efi");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} device partition={efiEspVolume.Root}");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} testsigning on");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} nointegritychecks on");
        }
        
        private async Task SetupBootMgr()
        {
            await invoker.Invoke($@"/set {{bootmgr}} displaybootmenu on");
            await invoker.Invoke($@"/deletevalue {{bootmgr}} customactions");
            await invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000001");
            await invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000002");
            await invoker.Invoke($@"/deletevalue {{bootmgr}} processcustomactionsfirst");
        }
        
        private async Task EnsureBootShim()
        {
            await invoker.SafeCreate(BcdGuids.Woa, $@"/d ""Windows 10"" /application BOOTAPP");                       
        }
    }
}
