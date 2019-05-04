using ByteSizeLib;

namespace Deployer.Lumia
{
    public interface ISettingsService
    {
        string WimFolder { get; set; }
        ByteSize SizeReservedForWindows { get; set; }
        bool UseCompactDeployment { get; set; }
        bool CleanDownloadedBeforeDeployment { get; set; }
        string DiskPreparer { get; set; }
    }
}