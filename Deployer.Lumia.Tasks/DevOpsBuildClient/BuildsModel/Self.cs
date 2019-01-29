using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class Self
    {

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
