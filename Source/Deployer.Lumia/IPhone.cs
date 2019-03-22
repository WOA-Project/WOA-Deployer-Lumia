using System.Threading.Tasks;
using Deployer.FileSystem;

namespace Deployer.Lumia
{
    public interface IPhone : IDevice
    {
        Task<PhoneModel> GetModel();
        Task<DualBootStatus> GetDualBootStatus();
        Task ToogleDualBoot(bool isEnabled);
        Task<Volume> GetDataVolume();
        Task<Volume> GetMainOsVolume();
        Task<Volume> GetEfiEspVolume();
    }
}