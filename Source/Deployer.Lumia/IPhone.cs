using System.Threading.Tasks;
using Deployer.FileSystem;

namespace Deployer.Lumia
{
    public interface IPhone : IDevice
    {
        Task<PhoneModel> GetModel();
        Task<DualBootStatus> GetDualBootStatus();
        Task EnableDualBoot(bool enable);
        Task<Volume> GetDataVolume();
        Task<Volume> GetMainOs();
    }
}