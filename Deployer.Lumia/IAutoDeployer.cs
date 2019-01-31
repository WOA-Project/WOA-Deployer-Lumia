using System;
using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IAutoDeployer
    {
        Task Deploy(WindowsDeploymentOptions opts, IObserver<double> progressObserver = null);
        Task ExecuteNonWindowsScript(string path);
        Task InstallGpu();
        Task ToogleDualBoot(bool isEnabled);
    }
}