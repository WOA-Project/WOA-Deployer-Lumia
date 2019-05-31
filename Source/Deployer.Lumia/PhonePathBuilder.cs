using System;
using System.Collections.Generic;
using System.IO;
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
            IDictionary<string, Func<Task<string>>> mappings = new Dictionary<string, Func<Task<string>>>()
            {
                { @"\[EFIESP\]", async () => (await Device.GetPartitionByName(PartitionName.EfiEsp)).Root},
                { @"\[DPP\]", async () =>
                    {
                        var partitionByName = await Device.GetPartitionByName(PartitionName.Dpp);
                        await partitionByName.EnsureWritable();
                        return partitionByName.Root;
                    }
                },                
                { @"\[Windows\]", async () => (await Device.GetWindowsPartition()).Root },                
                { @"\[System\]", async () => (await Device.GetSystemPartition()).Root },
            };

            foreach (var mapping in mappings)
            {
                if (Regex.IsMatch(str, mapping.Key))
                {
                    var mappingValue = await mapping.Value();
                    str = Regex.Replace(str, $"^{mapping.Key}", mappingValue, RegexOptions.IgnoreCase);
                    str = Regex.Replace(str, $@"\\+", @"\", RegexOptions.IgnoreCase);
                }
            }
            
            return str;
        }

        private IDevice Device => deploymentContext.Device;
    }
}