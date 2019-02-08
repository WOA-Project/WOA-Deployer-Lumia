using System.Threading.Tasks;
using ByteSizeLib;

namespace Deployer.Lumia
{
    public interface ISpaceAllocator
    {
        Task<bool> TryAllocate(IPhone phone, ByteSize requiredSpace);
    }
}