using System.Threading.Tasks;
using Deployer.Utils;

namespace Deployer
{
    public class FileSystemOperations : IFileSystemOperations
    {
        public Task Copy(string source, string destination)
        {
            return FileUtils.Copy(source, destination);
        }

        public Task CopyDirectory(string sourceDirectory, string destination)
        {
            return FileUtils.CopyDirectory(sourceDirectory, destination);
        }
    }
}