using Newtonsoft.Json;

namespace Deployer.Lumia.Tasks.DevOpsBuildClient.BuildsModel
{
    public class OrchestrationPlan
    {

        [JsonProperty("planId")]
        public string PlanId { get; set; }
    }
}