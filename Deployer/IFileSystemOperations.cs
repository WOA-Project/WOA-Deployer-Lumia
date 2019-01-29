using System.Threading.Tasks;

namespace Deployer
{
    public interface IFileSystemOperations
    {
        Task Copy(string source, string destination);
        Task CopyDirectory(string sourceDirectory, string destination);
    }
}