using System;
using System.Collections.Generic;
using System.Text;
using Registry.Other;

// namespaces...

namespace Registry.Lists
{
    // internal classes...
    public class LxListRecord : IListTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="LFListRecord" /> or <see cref="LHListRecord" />  class.
        ///     <remarks>The signature determines how the hash is calculated/verified</remarks>
        /// </summary>
        /// <param name="rawBytes"></param>
        /// <param name="relativeOffset"></param>
        public LxListRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;

            RawBytes = rawBytes;
            _size = BitConverter.ToInt32(rawBytes, 0);
        }

        /// <summary>
        ///     A dictionary of relative offsets and hashes to other records
        ///     <remarks>The offset is the key and the hash value is the value</remarks>
        /// </summary>
        public Dictionary<uint, string> Offsets
        {
            get
            {
                var offsets = new Dictionary<uint, string>();

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

                    var hash = string.Empty;

                    if (Signature == "lf")
                    {
                        //first 4 chars of string
                        hash = Encoding.GetEncoding(1252).GetString(RawBytes, index, 4);
                    }
                    else
                    {
                        //numerical hash
                        hash = BitConverter.ToUInt32(RawBytes, index).ToString();
                    }

                    index += 4;

                    offsets.Add(os, hash);

                    counter += 1;
                }

                return offsets;
            }
        }

        public bool IsFree => _size > 0;

        public bool IsReferenced { get; internal set; }

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
                sb.AppendLine($"Offset: 0x{offset.Key:X}, Hash: {offset.Value}");
                i += 1;
            }

            sb.AppendLine();
            sb.AppendLine("------------ End of offsets ------------");
            sb.AppendLine();

            //ncrunch: no coverage start
            if (IsFree)
            {
                sb.AppendLine($"Raw Bytes: {BitConverter.ToString(RawBytes)}");
                sb.AppendLine();
            }
            //ncrunch: no coverage end

            return sb.ToString();
        }
    }
}