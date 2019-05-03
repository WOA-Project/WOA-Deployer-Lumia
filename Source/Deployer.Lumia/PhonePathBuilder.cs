using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Deployer.Lumia
{
    public class PhonePathBuilder : IPathBuilder
    {
        private readonly IPhone phone;

        public PhonePathBuilder(IPhone phone)
        {
            this.phone = phone;
        }

        public async Task<string> Replace(string str)
        {
            IDictionary<string, Func<Task<string>>> mappings = new Dictionary<string, Func<Task<string>>>()
            {
                { @"\[EFIESP\]", async () => (await phone.GetVolumeByPartitionName(PartitionName.EfiEsp)).Root},
                { @"\[DPP\]", async () => (await phone.GetVolumeByPartitionName(PartitionName.Dpp)).Root },                
                { @"\[Windows\]", async () => (await phone.GetWindowsVolume()).Root },                
                { @"\[System\]", async () => (await phone.GetSystemVolume()).Root },
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
    }
}