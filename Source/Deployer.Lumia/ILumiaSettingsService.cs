using ByteSizeLib;

namespace Deployer.Lumia
{
    public interface ILumiaSettingsService : ISettingsService
    {
        ByteSize SizeReservedForWindows { get; set; }
        bool UseCompactDeployment { get; set; }
        bool CleanDownloadedBeforeDeployment { get; set; }
        string DiskPreparer { get; set; }
    }
}