using System.IO.Compression;
using System.Threading.Tasks;

namespace Deployer
{
    public interface IGitHubDownloader
    {
        Task<ZipArchive> DownloadAsZipArchive(string url);
    }
}