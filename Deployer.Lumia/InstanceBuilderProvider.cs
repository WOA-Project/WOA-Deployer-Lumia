using System.Threading.Tasks;
using Deployer.FileSystem;
using Grace.DependencyInjection;

namespace Deployer.Lumia
{
    public class InstanceBuilderProvider : IInstanceBuilderProvider
    {
        private readonly ILocatorService locatorService;
        private readonly Phone phone;
        private Volume efiEsp;

        public InstanceBuilderProvider(ILocatorService locatorService, Phone phone)
        {
            this.locatorService = locatorService;
            this.phone = phone;
        }

        public async Task<IInstanceBuilder> Create()
        {
            efiEsp = efiEsp ?? (efiEsp = await phone.GetEfiespVolume());
            var phoneStringMapBuilder = new PhoneStringMapBuilder(efiEsp.RootDir.Name);
            return new InstanceBuilder(locatorService, phoneStringMapBuilder);
        }
    }
}