using System.Threading.Tasks;
using Deployer.Tasks;
using Serilog;

namespace Deployer.Lumia.NetFx
{
    public class Tooling : ITooling
    {
        private readonly IPhone phone;

        public Tooling(IDeploymentContext context)
        {
            phone = (IPhone) context.Device;
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