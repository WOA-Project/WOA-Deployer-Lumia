using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Deployer;
using Deployer.Lumia.NetFx;
using Deployment.Console.Options;

namespace Deployment.Console
{
    internal class ConsoleDeployer
    {
        public static async Task ExecuteWindowsScript(WindowsDeploymentCmdOptions opts)
        {
            var progressObserver = new Subject<double>();
            progressObserver.Subscribe(x => System.Console.WriteLine("{0:P0}"));
            var winDeploymentOpts = new WindowsDeploymentOptions
            {
                Index = opts.Index,
                ReservedSizeForWindowsInGb = opts.ReservedSizeForWindowsInGb,
                WimImage = opts.WimImage,
            };
            await new DeploymentScriptRunner().ExecuteWindowsScript(opts.Script, winDeploymentOpts, progressObserver);
            progressObserver.Dispose();
        }

        public static async Task ExecuteNonWindowsScript(string path)
        {
            await new DeploymentScriptRunner().ExecuteNonWindowsScript(path);
        }
    }
}