using System.IO.Compression;
using System.Threading.Tasks;

namespace Deployer.Lumia.Tasks
{
    public interface IGitHubDownloader
    {
        Task<ZipArchive> DownloadAsZipArchive(string url);
    }
}