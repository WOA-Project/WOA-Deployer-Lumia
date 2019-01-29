using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.ArtifactModel
{
    public class Properties
    {

        [JsonProperty("localpath")]
        public string Localpath { get; set; }
    }
}
