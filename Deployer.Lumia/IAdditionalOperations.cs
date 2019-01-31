using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IAdditionalOperations
    {
        Task InstallGpu();
        Task ToogleDualBoot(bool isEnabled);
    }
}