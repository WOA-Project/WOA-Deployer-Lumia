using System.Collections.Generic;
using System.Threading.Tasks;
using Deployer.FileSystem;

namespace Deployer
{
    public interface IDevice
    {
        Task<Disk> GetDisk();
        Task<ICollection<Disk>> GetDisks();
        Task<Volume> GetWindowsVolume();
        Task<Volume> GetBootVolume();
        Task<bool> IsOobeFinished();
        Task RemoveExistingWindowsPartitions();
        Task<ICollection<DriverMetadata>> GetDrivers();
    }
}