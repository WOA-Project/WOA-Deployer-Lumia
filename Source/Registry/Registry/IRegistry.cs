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
}