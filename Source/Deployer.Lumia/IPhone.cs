using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IPhone : IDevice
    {
        Task<PhoneModelInfo> GetModel();
        Task<DualBootStatus> GetDualBootStatus();
        Task ToogleDualBoot(bool isEnabled, bool force = false);
    }
}