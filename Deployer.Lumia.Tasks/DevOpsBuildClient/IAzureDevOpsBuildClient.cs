using System.Threading.Tasks;
using Deployer.Lumia.Tasks.DevOpsBuildClient.ArtifactModel;
using Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient
{
    public interface IAzureDevOpsBuildClient
    {
        Task<Artifact> GetArtifact(string org, string project, int buildId, string artifactsName);
        Task<Build> GetLatestBuild(string org, string project, int definition);
        Task<Artifact> LatestBuildArtifact(string org, string project, int definitionId, string artifactsName);
    }
}