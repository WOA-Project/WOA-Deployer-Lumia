using System;
using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IAutoDeployer
    {
        Task Deploy();
        Task InstallGpu();
        Task ToogleDualBoot(bool isEnabled);
    }
}