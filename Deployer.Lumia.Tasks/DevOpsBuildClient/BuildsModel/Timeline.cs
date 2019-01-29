using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class Timeline
    {

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}