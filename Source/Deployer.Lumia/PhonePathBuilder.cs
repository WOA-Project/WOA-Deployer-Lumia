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
            IDictionary<string, Func<Task<string>>> mappings = new Dictionary<string, Func<Task<string>>>()
            {
                { @"\[EFIESP\]", async () => (await Device.GetPartitionByName(PartitionName.EfiEsp)).Root},
                { @"\[DPP\]", async () => (await (await Device.GetPartitionByName(PartitionName.Dpp)).EnsureWritable()).Root},                
                { @"\[Windows\]", async () => (await (await Device.GetWindowsPartition()).EnsureWritable()).Root },                
                { @"\[System\]", async () => (await (await Device.GetSystemPartition()).EnsureWritable()).Root },
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