using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class Web
    {

        [JsonProperty("href")]
        public string Href { get; set; }
    }
}