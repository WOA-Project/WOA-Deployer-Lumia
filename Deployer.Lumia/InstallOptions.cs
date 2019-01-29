using ByteSizeLib;

namespace Deployer.Lumia
{
    public class InstallOptions
    {
        public string ImagePath { get; set; }
        public int ImageIndex { get; set; } = 1;
        public bool PatchBoot { get; set; }
        public ByteSize SizeReservedForWindows { get; set; } = ByteSize.FromGigaBytes(1.5);
    }
}