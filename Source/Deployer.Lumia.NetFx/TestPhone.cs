using System.Linq;
using System.Threading.Tasks;
using Deployer.Exceptions;
using Deployer.FileSystem;
using Deployer.Tasks;

namespace Deployer.Lumia.NetFx
{
    public class TestPhone : Phone
    {
        public TestPhone(IDiskApi diskApi, IPhoneModelReader phoneModelReader, BcdInvokerFactory bcdInvokerFactory) : base(diskApi, phoneModelReader, bcdInvokerFactory)
        {
        }

        public override async Task<Disk> GetDeviceDisk()
        {
            var disks = await DiskApi.GetDisks();
            foreach (var disk in disks.Where(x => x.Number != 0))
            {
                if (true)
                {
                    var mainOs = await disk.GetVolumeByLabel(VolumeName.MainOs);
                    if (mainOs != null)
                    {
                        return disk;
                    }
                }
            }

            throw new PhoneDiskNotFoundException("Cannot get the Phone Disk. Please, verify that the Phone is in Mass Storage Mode.");
        }        
    }
}