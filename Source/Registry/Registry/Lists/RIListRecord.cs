using System;
using System.Collections.Generic;
using System.Text;
using Registry.Other;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    public class RiListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="RiListRecord" />  class.
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public RiListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
        }

        /// <summary>
        ///     A list of relative offsets to other records
        /// </summary>
        public List<uint> Offsets
        {
            get
            {
                var offsets = new List<uint>();

                var index = 0x8;
                var counter = 0;

                while (counter < NumberOfEntries)
                {
                    if (index >= RawBytes.Length)
                    {
                        //ncrunch: no coverage
                        // i have seen cases where there isnt enough data, so get what we can
                        break; //ncrunch: no coverage
                    }

                    var os = BitConverter.ToUInt32(RawBytes, index);
                    index += 4;

                    offsets.Add(os);

                    counter += 1;
                }

                return offsets;
            }
        }

        // public properties...
        public bool IsFree => _size > 0;

        public bool IsReferenced { get; set; }

        public int NumberOfEntries => BitConverter.ToUInt16(RawBytes, 0x06);

        public byte[] RawBytes { get; }
        public long RelativeOffset { get; }

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

            var i = 0;

            foreach (var offset in Offsets)
            {
                sb.AppendLine($"------------ Offset/hash record #{i} ------------");
                sb.AppendLine($"Offset: 0x{offset:X}");
                i += 1;
            }

            sb.AppendLine();
            sb.AppendLine("------------ End of offsets ------------");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}