using System.ComponentModel;
using Registry.Other;

namespace Registry
{
    public interface IRegistry
    {
        byte[] FileBytes { get; }

        HiveTypeEnum HiveType { get; }

        string HivePath { get; }

        RegistryHeader Header { get; set; }

        byte[] ReadBytesFromHive(long offset, int length);
    }

    public enum HiveTypeEnum
    {
        [Description("Other")] Other = 0,
        [Description("NTUSER")] NtUser = 1,
        [Description("SAM")] Sam = 2,
        [Description("SECURITY")] Security = 3,
        [Description("SOFTWARE")] Software = 4,
        [Description("SYSTEM")] System = 5,
        [Description("USRCLASS")] UsrClass = 6,
        [Description("COMPONENTS")] Components = 7,
        [Description("DRIVERS")] Drivers = 8,
        [Description("BCD")] Bcd = 8,
        [Description("AMCACHE")] Amcache = 9,
        [Description("SYSCACHE")] Syscache = 10
    }
}