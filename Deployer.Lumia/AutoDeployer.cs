using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public class AutoDeployer : IAutoDeployer
    {
        private readonly IDeploymentScriptRunner runner;
        private readonly IAdditionalOperations additionalOperations;
        private readonly Phone phone;

        public AutoDeployer(IDeploymentScriptRunner runner, IAdditionalOperations additionalOperations, Phone phone)
        {
            this.runner = runner;
            this.additionalOperations = additionalOperations;
            this.phone = phone;
        }

        public async Task Deploy(WindowsDeploymentOptions opts, IObserver<double> progressObserver = null)
        {
            var dict = new Dictionary<PhoneModel, string>
            {
                {PhoneModel.Lumia950, Path.Combine("Scripts", "950.txt")},
                {PhoneModel.Lumia950XL, Path.Combine("Scripts", "950xl.txt")},
            };

            var phoneModel = await phone.GetModel();
            var script = dict[phoneModel];

            await runner.ExecuteWindowsScript(script, opts, progressObserver);
        }

        public async Task ExecuteNonWindowsScript(string path)
        {
            await runner.ExecuteNonWindowsScript(path);
        }

        public Task InstallGpu()
        {
            return additionalOperations.InstallGpu();
        }

        public Task ToogleDualBoot(bool isEnabled)
        {
            return additionalOperations.ToogleDualBoot(isEnabled);
        }
    }
}