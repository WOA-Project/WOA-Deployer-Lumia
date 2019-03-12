using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IWoaDeployer
    {
        Task Deploy();
        Task ToggleDualBoot(bool p0);
    }
}