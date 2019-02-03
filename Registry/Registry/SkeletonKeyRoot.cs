namespace Registry
{
    public class SkeletonKeyRoot
    {
        public SkeletonKeyRoot(string keyPath, bool addValues, bool recursive)
        {
            KeyPath = keyPath;
            AddValues = addValues;
            Recursive = recursive;
        }

        public string KeyPath { get; }
        public bool AddValues { get; }
        public bool Recursive { get; }
    }
}