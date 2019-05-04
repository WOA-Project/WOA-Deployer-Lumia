using ByteSizeLib;
using Deployer.Lumia.Gui.Properties;

namespace Deployer.Lumia.Gui.Specifics
{
    public class SettingsService : ISettingsService
    {
        private readonly Settings settings = Settings.Default;

        public string WimFolder
        {
            get => settings.WimFolder;
            set
            {
                settings.WimFolder = value;
                settings.Save();
            }
        }

        public ByteSize SizeReservedForWindows
        {
            get => ByteSize.FromGigaBytes(settings.SizeReservedForWindows);
            set
            {
                settings.SizeReservedForWindows = value.GigaBytes;
                settings.Save();
            }
        }

        public bool UseCompactDeployment
        {
            get => settings.UseCompactDeployment;
            set
            {
                settings.UseCompactDeployment = value;
                settings.Save();
            }
        }

        public bool CleanDownloadedBeforeDeployment
        {
            get => settings.CleanDownloadedBeforeDeployment;
            set
            {
                settings.CleanDownloadedBeforeDeployment = value;
                settings.Save();
            }
        }

        public string DiskPreparer
        {
            get => settings.DiskPreparer;
            set
            {
                settings.DiskPreparer = value;
                settings.Save();
            }
        }
    }
}