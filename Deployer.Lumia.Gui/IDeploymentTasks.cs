using System.Threading.Tasks;

namespace Deployer.Lumia.Gui
{
    public interface IDeploymentTasks
    {
        Task Deploy(string wimPath);
    }
}