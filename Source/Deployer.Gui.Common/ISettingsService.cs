namespace Deployer.Gui.Common
{
    public interface ISettingsService
    {
        string DriverPackFolder { get; set; }
        string WimFolder { get; set; }
        double SizeReservedForWindows { get; set; }
        void Save();
    }
}