using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Deployer.Execution;
using Serilog;

namespace Deployer.Lumia
{
    public class WoaDeployer : IWoaDeployer
    {
        private readonly ITooling tooling;
        private readonly IPhone phone;
        private readonly IScriptRunner scriptRunner;
        private readonly IScriptParser parser;

        public WoaDeployer(IScriptRunner scriptRunner, IScriptParser parser, ITooling tooling, IPhone phone)
        {
            this.scriptRunner = scriptRunner;
            this.parser = parser;
            this.tooling = tooling;
            this.phone = phone;
        }

        public async Task Deploy()
        {
            var dict = new Dictionary<(PhoneModel, Variant), string>
            {
                {(PhoneModel.Talkman, Variant.SingleSim), Path.Combine("Scripts", "Talkman", "Single-Sim.txt")},
                {(PhoneModel.Cityman, Variant.SingleSim), Path.Combine("Scripts", "Cityman", "Single-Sim.txt")},
                {(PhoneModel.Talkman, Variant.DualSim), Path.Combine("Scripts", "Talkman", "Double-Sim.txt")},
                {(PhoneModel.Cityman, Variant.DualSim), Path.Combine("Scripts", "Cityman", "Double-Sim.txt")},
            };

            var phoneModel = await phone.GetModel();
            Log.Verbose("{Model} detected", phoneModel);
            var path = dict[(phoneModel.Model, phoneModel.Variant)];

            await scriptRunner.Run(parser.Parse(File.ReadAllText(path)));
            await PreparePhoneDiskForSafeRemoval();
        }

        private async Task PreparePhoneDiskForSafeRemoval()
        {
            Log.Information("# Preparing phone for safe removal");
            Log.Information("Please wait...");
            var disk = await phone.GetDeviceDisk();
            await disk.Refresh();
        }

        public Task ToggleDualBoot(bool isEnabled)
        {
            return tooling.ToogleDualBoot(isEnabled);
        }
    }
}