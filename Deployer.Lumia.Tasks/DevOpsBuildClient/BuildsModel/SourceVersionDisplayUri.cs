using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class SourceVersionDisplayUri
    {

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}