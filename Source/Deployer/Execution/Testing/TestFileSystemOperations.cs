using System.Threading.Tasks;
using Serilog;

namespace Deployer.Execution.Testing
{
    public class TestFileSystemOperations : IFileSystemOperations
    {
        public Task Copy(string source, string destination)
        {
            Log.Verbose("Copied {Source} to {Destination}", source, destination);
            return Task.CompletedTask;
        }

        public Task CopyDirectory(string sourceDirectory, string destination)
        {
            Log.Verbose("Copied folder {Source} to {Destination}", sourceDirectory, destination);
            return Task.CompletedTask;
        }
    }
}