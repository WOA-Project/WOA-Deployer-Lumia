using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.FileSystem;
using Deployer.Services;

namespace Deployer.Lumia.Tasks
{
    [TaskDescription("Configuring BCD")]
    public class ConfigureBCD : IDeploymentTask
    {
        private readonly Phone phone;
        private readonly IBcdInvokerFactory bcdInvokerFactory;

        public ConfigureBCD(Phone phone, IBcdInvokerFactory bcdInvokerFactory)
        {
            this.phone = phone;
            this.bcdInvokerFactory = bcdInvokerFactory;
        }

        public async Task Execute()
        {
            var efiespVolume = await phone.GetEfiespVolume();

            var bcdInvoker = bcdInvokerFactory.Create(efiespVolume.GetBcdFullFilename());
            new BcdConfigurator(bcdInvoker, efiespVolume).SetupBcd();                       
        }
    }
}