using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Tasks;

namespace Deployer.Lumia.Gui.ViewModels
{
    public interface IScriptDependencyResolver
    {
        Task<IDeploymentContext> GetDeploymentContext(Script script);
    }
}