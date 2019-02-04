using System;
using System.Text;
using Registry.Other;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    public class DbListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="DbListRecord" />  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public DbListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
        }

        /// <summary>
        ///     The relative offset to another data node that contains a list of relative offsets to data for a VK record
        /// </summary>
        public uint OffsetToOffsets => BitConverter.ToUInt32(RawBytes, 0x8);

        // public properties...

        public bool IsFree => _size > 0;

        public bool IsReferenced { get; set; }

        public int NumberOfEntries => BitConverter.ToUInt16(RawBytes, 0x06);

        public byte[] RawBytes { get; set; }
        public long RelativeOffset { get; set; }

        public string Signature => Encoding.ASCII.GetString(RawBytes, 4, 2);

        public int Size => Math.Abs(_size);

        // public properties...
        public long AbsoluteOffset => RelativeOffset + 4096;

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Size: 0x{Size:X}");
            sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
            sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");

            sb.AppendLine($"Signature: {Signature}");

            sb.AppendLine();

            sb.AppendLine($"Is Free: {IsFree}");

            sb.AppendLine();

            sb.AppendLine($"Number Of Entries: {NumberOfEntries:N0}");
            sb.AppendLine();

            sb.AppendLine($"Offset To Offsets: 0x{OffsetToOffsets:X}");

            sb.AppendLine();

            return sb.ToString();
        }
    }
}