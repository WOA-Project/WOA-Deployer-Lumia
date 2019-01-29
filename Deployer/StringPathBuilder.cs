using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Deployer
{
    public class StringPathBuilder : IPathBuilder
    {
        private readonly IDictionary<string, string> mappings;

        public StringPathBuilder(IDictionary<string, string> mappings)
        {
            this.mappings = mappings;
        }

        public string Replace(string str)
        {
            var matching = mappings.Keys.FirstOrDefault(s => str.StartsWith(s, StringComparison.OrdinalIgnoreCase));
            if (matching !=null)
            {
                return Regex.Replace(str, $"^{matching}", mappings[matching], RegexOptions.IgnoreCase);
            }

            return str;
        }
    }
}