using System.Threading.Tasks;
using Deployment;

namespace Deployer.Test
{
    public class TestTask : IDeploymentTask
    {
        public Task Execute()
        {
            IsExecuted = true;
            return Task.CompletedTask;
        }

        public bool IsExecuted { get; set; }
    }
}