namespace Deployer
{
    public class WindowsDeploymentOptions
    {
        public string WimImage { get; set; }
        public int Index { get; set; }
        public double ReservedSizeForWindowsInGb { get; set; }
    }
}