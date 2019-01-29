using System;
using System.Linq;
using System.Text;

// namespaces...

namespace Registry.Other
{
    // internal classes...
    // public classes...
    public class DataNode : IRecordBase
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="DataNode" /> class.
        /// </summary>
        public DataNode(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);
        }

        // public properties...
        public byte[] Data => new ArraySegment<byte>(RawBytes, 4, RawBytes.Length - 4).ToArray();

        public bool IsFree => _size > 0;

        public byte[] RawBytes { get; }

        /// <summary>
        ///     Set to true when a record is referenced by another referenced record.
        ///     <remarks>
        ///         This flag allows for determining records that are marked 'in use' by their size but never actually
        ///         referenced by another record in a hive
        ///     </remarks>
        /// </summary>
        public bool IsReferenced { get; internal set; }

        /// <summary>
        ///     The offset as stored in other records to a given record
        ///     <remarks>This value will be 4096 bytes (the size of the regf header) less than the AbsoluteOffset</remarks>
        /// </summary>
        public long RelativeOffset { get; }

        public int Size => Math.Abs(_size);

        // public properties...
        /// <summary>
        ///     The offset to this record from the beginning of the hive, in bytes
        /// </summary>
        public long AbsoluteOffset => RelativeOffset + 4096;

        public string Signature => string.Empty;

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Size: 0x{Size:X}");
            sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
            sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");

            sb.AppendLine();

            sb.AppendLine($"Is Free: {IsFree}");

            sb.AppendLine();

            sb.AppendLine($"Raw Bytes: {BitConverter.ToString(RawBytes)}");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}