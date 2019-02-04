using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class XAclRecord
    {
        // public enums...
        public enum AclTypeEnum
        {
            Security,
            Discretionary
        }

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="XAclRecord" /> class.
        /// </summary>
        public XAclRecord(byte[] rawBytes, AclTypeEnum aclTypetype)
        {
            RawBytes = rawBytes;

            AclType = aclTypetype;
        }

        // public properties...
        public ushort AceCount => BitConverter.ToUInt16(RawBytes, 0x4);

        public List<AceRecord> AceRecords
        {
            get
            {
                var index = 0x8; // the start of ACE structures

                var chunks = new List<byte[]>();

                for (var i = 0; i < AceCount; i++)
                {
                    if (index > RawBytes.Length)
                    {
                        //ncrunch: no coverage
                        break; //ncrunch: no coverage
                    }

                    var aceSize = RawBytes[index + 2];
                    var rawAce = RawBytes.Skip(index).Take(aceSize).ToArray();

                    chunks.Add(rawAce);

                    index += aceSize;
                }

                var records = new List<AceRecord>();

                foreach (var chunk in chunks)
                {
                    if (chunk.Length <= 0)
                    {
                        continue;
                    }

                    var ace = new AceRecord(chunk);

                    records.Add(ace);
                }

                return records;
            }
        }

        public byte AclRevision => RawBytes[0];

        public ushort AclSize => BitConverter.ToUInt16(RawBytes, 0x2);

        public AclTypeEnum AclType { get; }
        public byte[] RawBytes { get; }

        public byte Sbz1 => RawBytes[1];

        public ushort Sbz2 => BitConverter.ToUInt16(RawBytes, 0x6);

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"ACL Revision: 0x{AclRevision:X}");
            sb.AppendLine($"ACL Size: 0x{AclSize:X}");
            sb.AppendLine($"ACL Type: {AclType}");
            sb.AppendLine($"Sbz1: 0x{Sbz1:X}");
            sb.AppendLine($"Sbz2: 0x{Sbz2:X}");

            sb.AppendLine($"ACE Records Count: {AceCount}");

            sb.AppendLine();

            var i = 0;
            foreach (var aceRecord in AceRecords)
            {
                sb.AppendLine($"------------ Ace record #{i} ------------");
                sb.AppendLine(aceRecord.ToString());
                i += 1;
            }

            return sb.ToString();
        }
    }
}