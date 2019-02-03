using System.Collections.Generic;

namespace Registry
{
    public class SkeletonKey
    {
        public SkeletonKey(string keyPath, string keyName, bool addValues)
        {
            KeyPath = keyPath;
            KeyName = keyName;
            AddValues = addValues;
            Subkeys = new List<SkeletonKey>();
        }

        public string KeyName { get; }
        public string KeyPath { get; }
        public bool AddValues { get; }
        public List<SkeletonKey> Subkeys { get; }
    }
}