using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Utils;
using Serilog;

namespace Deployer.Tasks
{
    [TaskDescription("Unpacking from Azure DevOps: {0}")]
    public class AzureDevOpsUnpack : IDeploymentTask
    {
        private string org;
        private string project;
        private int definitionId;
        private string artifactName;
        private readonly IAzureDevOpsBuildClient buildClient;
        private string folderPath;

        private const string SubFolder = "Downloaded";

        public AzureDevOpsUnpack(string descriptor, IAzureDevOpsBuildClient buildClient)
        {
            ParseDescriptor(descriptor);
            
            this.buildClient = buildClient;
        }

        private void ParseDescriptor(string descriptor)
        {
            var parts = descriptor.Split(new[] {";"}, StringSplitOptions.None);
            
            org = parts[0];
            project = parts[1];
            definitionId = int.Parse(parts[2]);
            artifactName = parts[3];
            folderPath = Path.Combine(SubFolder, artifactName);
        }

        public async Task Execute()
        {
            if (Directory.Exists(folderPath))
            {
                Log.Warning("{Pack} was already downloaded. Skipping download.", artifactName);
                return;
            }

            var artifact = await buildClient.LatestBuildArtifact(org, project, definitionId, artifactName);

            using (var httpClient = new HttpClient())
            {
                var byteArray = httpClient.GetByteArrayAsync(artifact.Resource.DownloadUrl);
                var stream = new MemoryStream(await byteArray);

                using (var zip = new ZipArchive(stream))
                {
                    var temp = FileUtils.GetTempDirectoryName();
                    zip.ExtractToDirectory(temp);

                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }

                    Directory.Move(Path.Combine(temp, artifactName), folderPath);
                    Directory.Delete(temp);
                };
            }
        }
    }
}