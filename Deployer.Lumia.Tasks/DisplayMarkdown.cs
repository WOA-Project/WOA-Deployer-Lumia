using System.Threading.Tasks;
using Deployer.Execution;
using Serilog;

namespace Deployer.Lumia.Tasks
{
    [TaskDescription("Displaying Markdown document")]
    public class DisplayMarkdown : IDeploymentTask
    {
        private readonly string path;

        public DisplayMarkdown(string path)
        {
            this.path = path;
        }

        public Task Execute()
        {
            Log.Verbose("Displaying markdown from file {Path}", path);
            return Task.CompletedTask;
        }
    }
}