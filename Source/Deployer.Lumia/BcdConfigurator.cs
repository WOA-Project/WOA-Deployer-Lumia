using System;
using Deployer.FileSystem;
using Deployer.Services;
using Deployer.Utils;

namespace Deployer.Lumia
{
    public class BcdConfigurator
    {
        private readonly IBcdInvoker invoker;
        private readonly Volume mainOsVolume;

        public BcdConfigurator(IBcdInvoker invoker, Volume mainOsVolume)
        {
            this.invoker = invoker;
            this.mainOsVolume = mainOsVolume;
        }

        public void SetupBcd()
        {
            var bootShimEntry = CreateBootShim();
            var dummyEntry = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");;
            SetupBootShim(bootShimEntry);
            SetupDummyLoader(dummyEntry);
            SetupBootMgr();
            SetDisplayOptions(bootShimEntry, dummyEntry);
        }

        private void SetDisplayOptions(Guid entry, Guid dummy)
        {
            invoker.Invoke($@"/displayorder {{{entry}}}");
            invoker.Invoke($@"/displayorder {{{dummy}}} /addfirst");
            invoker.Invoke($@"/default {{{entry}}}");
            invoker.Invoke($@"/timeout 30");
        }

        private void SetupBootShim(Guid guid)
        {
            invoker.Invoke($@"/set {{{guid}}} path \EFI\boot\BootShim.efi");
            invoker.Invoke($@"/set {{{guid}}} device partition={mainOsVolume.Root}\EFIESP");
            invoker.Invoke($@"/set {{{guid}}} testsigning on");
            invoker.Invoke($@"/set {{{guid}}} nointegritychecks on");
        }
        
        private void SetupDummyLoader(Guid guid)
        {
            invoker.Invoke($@"/set {{{guid}}} path dummy");
            invoker.Invoke($@"/set {{{guid}}} description ""Dummy, please ignore""");
        }
        
        private void SetupBootMgr()
        {
            invoker.Invoke($@"/set {{bootmgr}} displaybootmenu on");
            invoker.Invoke($@"/deletevalue {{bootmgr}} customactions");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000001");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000002");
            invoker.Invoke($@"/deletevalue {{bootmgr}} processcustomactionsfirst");
        }
        
        private Guid CreateBootShim()
        {
            var invokeText = invoker.Invoke($@"/create {{7619dcca-fafe-11d9-b411-000476eba25f}} /d ""Windows 10"" /application BOOTAPP");
            return FormattingUtils.GetGuid(invokeText);
        }
    }
}
