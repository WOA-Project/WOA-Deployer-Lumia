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
            SetupDummy();
            SetupBootMgr();
            SetDisplayOptions();
        }

        private void SetupDummy()
        {
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} path dummy");
            invoker.Invoke($@"/set {{{BcdGuids.WinMobile}}} description ""Dummy, please ignore""");
        }

        private void SetDisplayOptions()
        {
            invoker.Invoke($@"/displayorder {{{BcdGuids.Woa}}}");
            invoker.Invoke($@"/displayorder {{{BcdGuids.WinMobile}}} /addlast");
            invoker.Invoke($@"/default {{{BcdGuids.Woa}}}");
            invoker.Invoke($@"/timeout 30");
        }

        private async Task SetupBootShim()
        {
            await EnsureBootShim();

            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} path \EFI\boot\BootShim.efi");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} device partition={efiEspVolume.Root}");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} testsigning on");
            await invoker.Invoke($@"/set {{{BcdGuids.Woa}}} nointegritychecks on");
        }
        
        private void SetupBootMgr()
        {
            invoker.Invoke($@"/set {{bootmgr}} displaybootmenu on");
            invoker.Invoke($@"/deletevalue {{bootmgr}} customactions");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000001");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000002");
            invoker.Invoke($@"/deletevalue {{bootmgr}} processcustomactionsfirst");
        }
        
        private async Task EnsureBootShim()
        {
            await invoker.SafeCreate(BcdGuids.Woa, $@"/d ""Windows 10"" /application BOOTAPP");                       
        }
    }
}
