using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class Badge
    {

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}