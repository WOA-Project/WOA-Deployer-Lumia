using System.Threading.Tasks;
using Deployer.Tasks;

namespace Deployer.Lumia
{
    public interface IWoaDeployer 
    {
        Task ToggleDualBoot(bool isEnabled);
        Task Deploy(IDeploymentContext deploymentContext);
    }
}