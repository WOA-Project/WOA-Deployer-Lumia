using System;
using System.Collections.Generic;

namespace Deployer.Lumia.NetFx.PhoneMetadata
{
    public class PhoneModelInfoInfoReader : IPhoneModelInfoReader
    {
        private readonly IPhoneInfoReader reader;

        public PhoneModelInfoInfoReader(IPhoneInfoReader reader)
        {
            this.reader = reader;
        }

        public PhoneModelInfo GetPhoneModel(uint diskNumber)
        {
            var info = reader.GetPhoneInfo(diskNumber);

            return new PhoneModelInfo(PhoneModel(info), GetVariant(info));
        }

        private static Variant GetVariant(PhoneInfo info)
        {
            var dict = new Dictionary<string, Variant>(StringComparer.OrdinalIgnoreCase)
            {
                {"RM-1085", Variant.SingleSim },
                {"RM-1104", Variant.SingleSim },
                {"RM-1105", Variant.SingleSim },
                {"RM-1116", Variant.DualSim },
                {"RM-1118", Variant.DualSim },
            };

            return dict[info.Ddp.PhoneType];
        }

        private static PhoneModel PhoneModel(PhoneInfo info)
        {
            var dict = new Dictionary<ModelIdentity, PhoneModel>()
            {
                {
                    new ModelIdentity("P6211", "42-7D-8F-D5-A7-F2-27-82-0D-5B-11-BF-8C-6F-76-70-C0-A0-62-2C-C6-1B-A9-5A-AE-E1-8F-75-17-FC-0B-77"),
                    Lumia.PhoneModel.Cityman
                },
                {
                    new ModelIdentity("P6170", "42-7D-8F-D5-A7-F2-27-82-0D-5B-11-BF-8C-6F-76-70-C0-A0-62-2C-C6-1B-A9-5A-AE-E1-8F-75-17-FC-0B-77"),
                    Lumia.PhoneModel.Hapanero
                },
                {
                    new ModelIdentity("P6218", "9C-FA-9A-DB-10-1C-E4-1E-C5-E0-B4-BF-58-6B-CD-37-A4-BA-93-1F-D9-75-F9-99-52-48-5F-EF-0E-7B-DF-A4"),
                    Lumia.PhoneModel.Talkman
                },
            };

            var rkhStr = BitConverter.ToString(info.Rkh);
            var name = info.Plat.Name.Replace("_ATT", "");

            var key = new ModelIdentity(name, rkhStr);
            return dict[key];
        }

        private class ModelIdentity
        {
            public string Plat { get; }
            public string Rkh { get; }

            public ModelIdentity(string plat, string rkh)
            {
                Plat = plat;
                Rkh = rkh;
            }

            protected bool Equals(ModelIdentity other)
            {
                return string.Equals(Plat, other.Plat) && string.Equals(Rkh, other.Rkh);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != GetType())
                {
                    return false;
                }

                return Equals((ModelIdentity) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Plat.GetHashCode() * 397) ^ Rkh.GetHashCode();
                }
            }
        }
    }
}