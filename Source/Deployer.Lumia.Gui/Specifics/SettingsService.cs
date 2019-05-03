using System.Collections.Generic;
using System.Linq;
using Deployer.Gui;
using Deployer.Lumia.Gui.Properties;
using Deployer.Tasks;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui.Specifics
{
    public class SettingsService : ISettingsService
    {
        private readonly IEnumerable<Meta<IDiskLayoutPreparer>> diskPreparers;

        public SettingsService(IEnumerable<Meta<IDiskLayoutPreparer>> diskPreparers)
        {
            this.diskPreparers = diskPreparers;
        }

        public string WimFolder
        {
            get => Settings.Default.WimFolder;
            set => Settings.Default.WimFolder = value;
        }

        public double SizeReservedForWindows
        {
            get => Settings.Default.SizeReservedForWindows;
            set => Settings.Default.SizeReservedForWindows = value;
        }

        public bool UseCompactDeployment
        {
            get => Settings.Default.UseCompactDeployment;
            set => Settings.Default.UseCompactDeployment = value;
        }

        public bool CleanDownloadedBeforeDeployment
        {
            get => Settings.Default.CleanDownloadedBeforeDeployment;
            set => Settings.Default.CleanDownloadedBeforeDeployment = value;
        }

        public IDiskLayoutPreparer DiskPreparer
        {
            get
            {
                var key = Settings.Default.DiskPreparer;
                var entry = diskPreparers
                                .FirstOrDefault(x => (string)x.Metadata["Name"] == key) ?? Default;
                return entry.Value;
            }
            set
            {
                Settings.Default.DiskPreparer = (string) diskPreparers.FirstOrDefault(meta => meta.Value == value)?.Metadata["Name"];                
            }
        }

        private Meta<IDiskLayoutPreparer> Default
        {
            get
            {
                return diskPreparers
                    .OrderByDescending(x => (int) x.Metadata["Order"])
                    .First();
            }
        }

        public void Save()
        {
            Settings.Default.Save();
        }
    }
}