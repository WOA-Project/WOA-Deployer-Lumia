using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IExistingDeploymentCleaner
    {
        Task Clean(IPhone toClean);
    }
}