using System.Threading.Tasks;

namespace Deployer.Execution
{
    public interface IRunner
    {
        Task Run(Script script);
    }
}