using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                { "EFIESP", async () =>
                    {
                        var volume = await phone.GetMainOsVolume();
                        return Path.Combine(volume.Root, "EFIESP");
                    }
                },
                { "WindowsARM", async () => (await phone.GetWindowsVolume()).Root },
            };

            var matching = mappings.Keys.FirstOrDefault(s => str.StartsWith(s, StringComparison.OrdinalIgnoreCase));
            if (matching !=null)
            {
                var replacement = await mappings[matching]();
                var replaced = Regex.Replace(str, $"^{matching}", replacement, RegexOptions.IgnoreCase);
                return Regex.Replace(replaced, $@"\\+", @"\", RegexOptions.IgnoreCase);
            }

            return str;            
        }

        private async Task<string> Replace(string str, string identifier, Func<Task<string>> func)
        {
            var matching = str.StartsWith(identifier, StringComparison.OrdinalIgnoreCase);
            if (matching)
            {
                var replacement = await func();
                return Regex.Replace(str, $"^{identifier}", replacement, RegexOptions.IgnoreCase);
            }

            return str;
        }
    }
}