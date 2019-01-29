namespace Registry.Abstractions
{
    public class ValueBySizeInfo
    {
        public ValueBySizeInfo(RegistryKey key, KeyValue value)
        {
            Key = key;
            Value = value;
        }

        public RegistryKey Key { get; }
        public KeyValue Value { get; }
    }
}