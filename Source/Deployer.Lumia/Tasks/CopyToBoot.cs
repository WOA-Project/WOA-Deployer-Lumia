using Deployer.Tasks;

namespace Deployer.Lumia.Tasks
{
    public class CopyToBoot : CopyToBootBase
    {
        public CopyToBoot(string origin, string destination, IFileSystemOperations fileSystemOperations, IDeviceProvider deviceProvider) : base(origin, destination, fileSystemOperations, deviceProvider)
        {
        }

        public override string SystemPartitionName => PartitionName.System;
    }
}