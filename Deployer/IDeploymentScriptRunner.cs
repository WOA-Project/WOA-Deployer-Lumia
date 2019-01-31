using System;
using System.Threading.Tasks;

namespace Deployer
{
    public interface IDeploymentScriptRunner
    {
        Task ExecuteWindowsScript(string script, WindowsDeploymentOptions windowsDeploymentCmdOptions, IObserver<double> progressObserver = null);
        Task ExecuteNonWindowsScript(string path);
    }
}