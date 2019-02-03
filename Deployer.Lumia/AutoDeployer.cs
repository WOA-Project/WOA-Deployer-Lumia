using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Deployer.Execution;

namespace Deployer.Lumia
{
    public class AutoDeployer : IAutoDeployer
    {
        private readonly ITooling tooling;
        private readonly Phone phone;
        private readonly IScriptRunner runner;

        public AutoDeployer(IScriptRunner runner, ITooling tooling, Phone phone)
        {
            this.runner = runner;
            this.tooling = tooling;
            this.phone = phone;
        }

        public async Task Deploy()
        {
            var dict = new Dictionary<PhoneModel, string>
            {
                {PhoneModel.Talkman, Path.Combine("Scripts", "950.txt")},
                {PhoneModel.Cityman, Path.Combine("Scripts", "950xl.txt")},
            };

            var phoneModel = await phone.GetModel();
            var path = dict[phoneModel];

            await runner.RunScriptFrom(path);
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