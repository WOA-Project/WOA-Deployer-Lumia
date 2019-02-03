using ByteSizeLib;

namespace Deployer
{
    public class InstallOptions
    {
        public string ImagePath { get; set; }
        public int ImageIndex { get; set; }
        public ByteSize SizeReservedForWindows { get; set; }
    }
}