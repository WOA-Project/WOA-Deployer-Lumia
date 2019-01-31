using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Services;

namespace Deployer.Tasks
{
    [TaskDescription("Injecting drivers")]
    public class InjectDrivers : IDeploymentTask
    {
        private readonly string origin;
        private readonly Device phone;
        private readonly IWindowsImageService imageService;

        public InjectDrivers(string origin, Device phone, IWindowsImageService imageService)
        {
            this.origin = origin;
            this.phone = phone;
            this.imageService = imageService;
        }

        public async Task Execute()
        {
            var windowsPartition = await phone.GetWindowsVolume();
            await imageService.InjectDrivers(origin, windowsPartition);
        }
    }
}