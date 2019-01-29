using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class Plan
    {

        [JsonProperty("planId")]
        public string PlanId { get; set; }
    }
}