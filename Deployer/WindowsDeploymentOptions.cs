using ByteSizeLib;

namespace Deployer
{
    public class WindowsDeploymentOptions
    {
        public string WimImage { get; set; }
        public int Index { get; set; }
        public ByteSize ReservedSizeForWindowsInGb { get; set; }
    }
}