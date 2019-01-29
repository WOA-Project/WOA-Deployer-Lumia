using System;
using System.Collections.Generic;

namespace Deployer.Lumia
{
    public class PhoneStringMapBuilder : IPathBuilder
    {
        private readonly string efiespVolume;
        private StringPathBuilder stringPathBuilder;

        public PhoneStringMapBuilder(string efiespVolume)
        {
            this.efiespVolume = efiespVolume;
        }

        public string Replace(string str)
        {
            return StringPathBuilder.Replace(str);
        }

        private StringPathBuilder StringPathBuilder
        {
            get
            {
                var builder = stringPathBuilder;
                if (builder != null)
                {
                    return builder;
                }

                var dict = new Dictionary<string, string>()
                {
                    {"EFIESP", efiespVolume },
                };

                return stringPathBuilder = new StringPathBuilder(dict);
            }
        }
    }
}