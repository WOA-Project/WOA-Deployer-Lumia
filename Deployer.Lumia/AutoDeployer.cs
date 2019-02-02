using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Deployer.Lumia
{
    public class AutoDeployer : IAutoDeployer
    {
        private readonly IDeploymentScriptRunner runner;
        private readonly ITooling tooling;
        private readonly Phone phone;

        public AutoDeployer(IDeploymentScriptRunner runner, ITooling tooling, Phone phone)
        {
            this.runner = runner;
            this.tooling = tooling;
            this.phone = phone;
        }

        public async Task Deploy(WindowsDeploymentOptions opts, IObserver<double> progressObserver = null)
        {
            var dict = new Dictionary<PhoneModel, string>
            {
                {PhoneModel.Talkman, Path.Combine("Scripts", "950.txt")},
                {PhoneModel.Cityman, Path.Combine("Scripts", "950xl.txt")},
            };

            var phoneModel = await phone.GetModel();
            var script = dict[phoneModel];

            await runner.ExecuteWindowsScript(script, opts, progressObserver);
        }

        public async Task ExecuteNonWindowsScript(string path)
        {
            await runner.ExecuteNonWindowsScript(path);
        }

        public async Task InstallGpu()
        {
            await tooling.InstallGpu();
        }

        public Task ToogleDualBoot(bool isEnabled)
        {
            return tooling.ToogleDualBoot(isEnabled);
        }
    }
}