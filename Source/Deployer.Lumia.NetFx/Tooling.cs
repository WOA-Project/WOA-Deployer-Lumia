using System.Threading.Tasks;
using Deployer.Execution;
using Serilog;

namespace Deployer.Lumia.NetFx
{
    // ReSharper disable once ClassNeverInstantiated.Local
    public class Tooling : ITooling
    {
        private readonly IPhone phone;
        private readonly IScriptRunner scriptRunner;

        public Tooling(IPhone phone, IScriptRunner scriptRunner)
        {
            this.phone = phone;
            this.scriptRunner = scriptRunner;
        }

        public async Task ToogleDualBoot(bool isEnabled)
        {
            var enabledStr = isEnabled ? "Enabling" : "Disabling";
            Log.Information($"{enabledStr} Dual Boot");
            await phone.ToogleDualBoot(isEnabled);

            Log.Information("Done");
        }
    }
}