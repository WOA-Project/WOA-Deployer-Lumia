using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
                { @"\[EFIESP\]", async () =>
                    {
                        var volume = await phone.GetMainOsVolume();
                        return Path.Combine(volume.Root, "EFIESP");
                    }
                },
                { @"\[Windows\]", async () => (await phone.GetWindowsVolume()).Root },
                { @"\[MainOS\]", async () => (await phone.GetMainOsVolume()).Root },
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

    public static class StringExtensions
    {

        [Pure]
        public static string Replace(this string source, string oldValue, string newValue,
            StringComparison comparisonType)
        {
            if (source.Length == 0 || oldValue.Length == 0)
                return source;

            var result = new System.Text.StringBuilder();
            int startingPos = 0;
            int nextMatch;
            while ((nextMatch = source.IndexOf(oldValue, startingPos, comparisonType)) > -1)
            {
                result.Append(source, startingPos, nextMatch - startingPos);
                result.Append(newValue);
                startingPos = nextMatch + oldValue.Length;
            }

            result.Append(source, startingPos, source.Length - startingPos);

            return result.ToString();
        }

        [Pure]
        public static bool Contains(this string source, string value, StringComparison comparisonType)
        {
            return source.IndexOf(value, comparisonType) >= 0;
        }
    }
}