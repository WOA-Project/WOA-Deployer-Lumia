using System.Threading.Tasks;
using Deployment;

namespace Deployer.Test
{
    public class ParameterizedTask : IDeploymentTask
    {
        private readonly string argument;

        public ParameterizedTask(string argument)
        {
            this.argument = argument;
        }

        public Task Execute()
        {
            return Task.CompletedTask;
        }
    }
}