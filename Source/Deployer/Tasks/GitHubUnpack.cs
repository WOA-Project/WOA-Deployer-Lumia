using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deployer.Execution;
using Serilog;

namespace Deployer.Tasks
{
    [TaskDescription("Unpacking from GitHub")]
    public class GitHubUnpack : IDeploymentTask
    {
        private readonly string downloadUrl;
        private readonly IGitHubDownloader downloader;
        private string repository;
        private string branch;
        private string folderName;
        private string folderPath;
        private const string SubFolder = "Downloaded";

        public GitHubUnpack(string downloadUrl, IGitHubDownloader downloader)
        {
            ParseUrl(downloadUrl);
            this.downloadUrl = downloadUrl;
            this.downloader = downloader;
        }

        private void ParseUrl(string url)
        {
            var matches = Regex.Match(url, "https://github\\.com/([\\w-]*)/([\\w-]*)");
            repository = matches.Groups[2].Value;
            branch = "master";
            folderName = repository + "-" + branch;
            folderPath = Path.Combine(SubFolder, folderName);
        }

        public async Task Execute()
        {
            if (Directory.Exists(folderPath))
            {
                Log.Warning("{Pack} was already downloaded. Skipping download.", repository);
                return;
            }

            using (var zip = await downloader.DownloadAsZipArchive(downloadUrl))
            {
                var temp = Path.Combine(SubFolder, Guid.NewGuid().ToString());
                zip.ExtractToDirectory(temp);
                
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }

                var firstChild = Path.Combine(temp, folderName);
                Directory.Move(firstChild, folderPath);
                Directory.Delete(temp);
            }
        }
    }
}