using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Deployer;
using Deployer.Lumia.NetFx;
using Deployment.Console.Options;

namespace Deployment.Console
{
    internal class ConsoleDeployer
    {
        private readonly IEnumerable<Type> taskTypes;

        public ConsoleDeployer(IEnumerable<Type> taskTypes)
        {
            this.taskTypes = taskTypes;
        }

        public async Task ExecuteWindowsScript(WindowsDeploymentCmdOptions opts)
        {
            var progressObserver = new Subject<double>();
            progressObserver.Subscribe(x => System.Console.WriteLine("{0:P0}"));
            var winDeploymentOpts = new WindowsDeploymentOptions
            {
                Index = opts.Index,
                ReservedSizeForWindowsInGb = opts.ReservedSizeForWindowsInGb,
                WimImage = opts.WimImage,
            };
            await new DeploymentScriptRunner(taskTypes).ExecuteWindowsScript(opts.Script, winDeploymentOpts, progressObserver);
            progressObserver.Dispose();
        }

        public async Task ExecuteNonWindowsScript(string path)
        {
            await new DeploymentScriptRunner(taskTypes).ExecuteNonWindowsScript(path);
        }

        public Task ToogleDualBoot(bool isEnabled)
        {
            return new AdditionalActions().ToogleDualBoot(isEnabled);
        }

        public async Task InstallGpu()
        {
            await new AdditionalActions().InstallGpu();
            System.Console.WriteLine(Resources.InstallGpuManualStep);
        }
    }
}