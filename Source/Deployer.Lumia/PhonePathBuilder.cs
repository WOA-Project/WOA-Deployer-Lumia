using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Deployer.FileSystem;
using Deployer.Tasks;

namespace Deployer.Lumia
{
    public class PhonePathBuilder : IPathBuilder
    {
        private readonly IDeploymentContext deploymentContext;

        public PhonePathBuilder(IDeploymentContext deploymentContext)
        {
            this.deploymentContext = deploymentContext;
        }

        public async Task<string> Replace(string str)
        {
            IDictionary<string, Func<Task<IPartition>>> mappings = new Dictionary<string, Func<Task<IPartition>>>
            {
                { @"\[EFIESP\]", async () => await Device.GetPartitionByName(PartitionName.EfiEsp)},
                { @"\[DPP\]",async () => await Device.GetPartitionByName(PartitionName.Dpp) },
                { @"\[Windows\]", async () => await Device.GetWindowsPartition()},
                { @"\[System\]", async () => await Device.GetSystemPartition()}
            };

            foreach (var mapping in mappings)
            {
                if (Regex.IsMatch(str, mapping.Key))
                {
                    var partition = await mapping.Value();
                    if (partition == null)
                    {
                        throw new InvalidOperationException($"The path token '{str}' didn't return a partition");
                    }

                    await partition.EnsureWritable();
                    var root = partition.Root;
                    
                    str = Regex.Replace(str, $"^{mapping.Key}", root, RegexOptions.IgnoreCase);
                    str = Regex.Replace(str, $@"\\+", @"\", RegexOptions.IgnoreCase);
                }
            }

            return str;
        }

        private IDevice Device => deploymentContext.Device;
    }
}