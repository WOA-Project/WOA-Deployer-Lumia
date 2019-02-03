namespace Registry
{
    public class DirtyPageInfo
    {
        public DirtyPageInfo(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }

        public int Offset { get; }
        public int Size { get; }

        /// <summary>
        ///     Contains the bytes to be overwritten at Offset in the original hive (from the start of hbins)
        /// </summary>
        public byte[] PageBytes { get; private set; }

        /// <summary>
        ///     Updates PageBytes to contain the data to be used when overwriting part of an original hive
        /// </summary>
        /// <param name="bytes"></param>
        public void UpdatePageBytes(byte[] bytes)
        {
            PageBytes = bytes;
        }

        public override string ToString()
        {
            return $"Offset: 0x{Offset:X4}, Size: 0x{Size:X4}";
        }
    }
}