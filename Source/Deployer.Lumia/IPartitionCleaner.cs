using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public interface IPartitionCleaner
    {
        Task Clean(IPhone toClean);
    }
}