using System;
using System.Linq;
using System.Text;

using Registry.Other;

// namespaces...

namespace Registry.Cells
{
    // public classes...
    public class SkCellRecord : ICellTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;

        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="SkCellRecord" /> class.
        ///     <remarks>Represents a Key Security Record</remarks>
        /// </summary>
        protected internal SkCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            //this has to be a multiple of 8, so check for it
            var paddingOffset = 0x18 + DescriptorLength;
            var paddingLength = rawBytes.Length - paddingOffset;

            if (paddingLength > 0)
            {
                var padding = rawBytes.Skip((int) paddingOffset).Take((int) paddingLength).ToArray();

                //Check.That(Array.TrueForAll(padding, a => a == 0));
            }

            //Check that we have accounted for all bytes in this record. this ensures nothing is hidden in this record or there arent additional data structures we havent processed in the record.
            //   Check.That(0x18 + (int) DescriptorLength + paddingLength).IsEqualTo(rawBytes.Length);
        }

        // public properties...

        /// <summary>
        ///     A relative offset to the previous SK record
        /// </summary>
        public uint BLink => BitConverter.ToUInt32(RawBytes, 0x0c);

        public uint DescriptorLength => BitConverter.ToUInt32(RawBytes, 0x14);

        /// <summary>
        ///     A relative offset to the next SK record
        /// </summary>
        public uint FLink => BitConverter.ToUInt32(RawBytes, 0x08);

        /// <summary>
        ///     A count of how many keys this security record applies to
        /// </summary>
        public uint ReferenceCount => BitConverter.ToUInt32(RawBytes, 0x10);

        public ushort Reserved => BitConverter.ToUInt16(RawBytes, 0x6);

        /// <summary>
        ///     The security descriptor object for this record
        /// </summary>
        public SkSecurityDescriptor SecurityDescriptor
        {
            get
            {
                var rawDescriptor = RawBytes.Skip(0x18).Take((int) DescriptorLength).ToArray();

                if (rawDescriptor.Length > 0)
                {
                    // i have seen cases where there is no available security descriptor because the sk record doesn't contain the right data
                    return new SkSecurityDescriptor(rawDescriptor);
                }

                return null; //ncrunch: no coverage
            }
        }

        // public properties...
        public long AbsoluteOffset => RelativeOffset + 4096;

        public bool IsFree => _size > 0;

        public bool IsReferenced { get; internal set; }
        public byte[] RawBytes { get; }
        public long RelativeOffset { get; }

        public string Signature => Encoding.ASCII.GetString(RawBytes, 4, 2);

        public int Size => Math.Abs(_size);

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Size: 0x{Math.Abs(_size):X}");
            sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
            sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");
            sb.AppendLine($"Signature: {Signature}");

            sb.AppendLine($"Is Free: {IsFree}");

            sb.AppendLine();
            sb.AppendLine($"Forward Link: 0x{FLink:X}");
            sb.AppendLine($"Backward Link: 0x{BLink:X}");
            sb.AppendLine();

            sb.AppendLine($"Reference Count: {ReferenceCount:N0}");

            sb.AppendLine();
            sb.AppendLine($"Security descriptor length: 0x{DescriptorLength:X}");

            sb.AppendLine();
            sb.AppendLine($"Security descriptor: {SecurityDescriptor}");

            return sb.ToString();
        }
    }
}