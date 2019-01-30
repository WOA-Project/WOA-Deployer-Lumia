using System.Threading.Tasks;
using Deployer.Execution;

namespace Deployer.Lumia.Tasks
{
    [TaskDescription("Copying folder")]
    public class CopyDirectory : IDeploymentTask
    {
        private readonly string origin;
        private readonly string destination;
        private readonly IFileSystemOperations fileSystemOperations;

        public CopyDirectory(string origin, string destination, IFileSystemOperations fileSystemOperations)
        {
            this.origin = origin;
            this.destination = destination;
            this.fileSystemOperations = fileSystemOperations;
        }

        public Task Execute()
        {
            return fileSystemOperations.CopyDirectory(origin, destination);
        }
    }
}