using System;
using System.Collections.Generic;
using System.Text;

using Registry.Other;

// namespaces...


//ncrunch: no coverage start

namespace Registry.Cells
{
    // public classes...
    public class LkCellRecord : ICellTemplate, IRecordBase
    {
        // public enums...
        [Flags]
        public enum FlagEnum
        {
            CompressedName = 0x0020,
            HiveEntryRootKey = 0x0004,
            HiveExit = 0x0002,
            NoDelete = 0x0008,
            PredefinedHandle = 0x0040,
            SymbolicLink = 0x0010,
            Unused0400 = 0x0400,
            Unused0800 = 0x0800,
            Unused1000 = 0x1000,
            Unused2000 = 0x2000,
            Unused4000 = 0x4000,
            Unused8000 = 0x8000,
            UnusedVolatileKey = 0x0001,
            VirtMirrored = 0x0080,
            VirtTarget = 0x0100,
            VirtualStore = 0x0200
        }

        // private fields...
        private readonly int _size;
        // protected internal constructors...

        // public fields...
        public List<ulong> ValueOffsets;

        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="NkCellRecord" /> class.
        ///     <remarks>Represents a Key Node Record</remarks>
        /// </summary>
        protected internal LkCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            ValueOffsets = new List<ulong>();

            _size = BitConverter.ToInt32(rawBytes, 0);

            //TODO FINISH THIS LIKE NK

            //RootCellIndex
            var num = BitConverter.ToUInt32(rawBytes, 0x20);

            if (num == 0xFFFFFFFF)
            {
                RootCellIndex = 0;
            }
            else
            {
                RootCellIndex = num;
            }

            //HivePointer
            num = BitConverter.ToUInt32(rawBytes, 0x24);

            if (num == 0xFFFFFFFF)
            {
                HivePointer = 0;
            }
            else
            {
                HivePointer = num;
            }

            SecurityCellIndex = BitConverter.ToUInt32(rawBytes, 0x30);

            //ClassCellIndex
            num = BitConverter.ToUInt32(rawBytes, 0x34);

            if (num == 0xFFFFFFFF)
            {
                ClassCellIndex = 0;
            }
            else
            {
                ClassCellIndex = num;
            }

            MaximumNameLength = BitConverter.ToUInt16(rawBytes, 0x38);

            var rawFlags = Convert.ToString(rawBytes[0x3a], 2).PadLeft(8, '0');

            //TODO is this a flag enum somewhere?
            var userInt = Convert.ToInt32(rawFlags.Substring(0, 4));

            var virtInt = Convert.ToInt32(rawFlags.Substring(4, 4));

            UserFlags = userInt;
            VirtualControlFlags = virtInt;

            Debug = rawBytes[0x3b];

            MaximumClassLength = BitConverter.ToUInt32(rawBytes, 0x3c);
            MaximumValueNameLength = BitConverter.ToUInt32(rawBytes, 0x40);
            MaximumValueDataLength = BitConverter.ToUInt32(rawBytes, 0x44);

            WorkVar = BitConverter.ToUInt32(rawBytes, 0x48);

            NameLength = BitConverter.ToUInt16(rawBytes, 0x4c);
            ClassLength = BitConverter.ToUInt16(rawBytes, 0x4e);

            //  if (Flags.ToString().Contains(FlagEnum.CompressedName.ToString()))
            if ((Flags & FlagEnum.CompressedName) == FlagEnum.CompressedName)
            {
                Name = Encoding.GetEncoding(1252).GetString(rawBytes, 0x50, NameLength);
            }
            else
            {
                Name = Encoding.Unicode.GetString(rawBytes, 0x50, NameLength);
            }

            var paddingOffset = 0x50 + NameLength;
            var paddingLength = Math.Abs(Size) - paddingOffset;

            if (paddingLength > 0)
            {
                Padding = new byte[paddingLength];
                Array.Copy(rawBytes, paddingOffset, Padding, 0, paddingLength);
                //Padding = BitConverter.ToString(rawBytes, paddingOffset, paddingLength);
            }

            //we have accounted for all bytes in this record. this ensures nothing is hidden in this record or there arent additional data structures we havent processed in the record.
            
        }

        // public properties...
        /// <summary>
        ///     The relative offset to a data node containing the classname
        ///     <remarks>
        ///         Use ClassLength to get the correct classname vs using the entire contents of the data node. There is often
        ///         slack slace in the data node when they hold classnames
        ///     </remarks>
        /// </summary>
        public uint ClassCellIndex { get; }

        /// <summary>
        ///     The length of the classname in the data node referenced by ClassCellIndex.
        /// </summary>
        public ushort ClassLength { get; }

        public byte Debug { get; }

        public FlagEnum Flags => (FlagEnum) BitConverter.ToUInt16(RawBytes, 6);

        /// <summary>
        ///     The last write time of this key
        /// </summary>
        public DateTimeOffset LastWriteTimestamp
        {
            get
            {
                var ts = BitConverter.ToInt64(RawBytes, 0x8);

                return DateTimeOffset.FromFileTime(ts).ToUniversalTime();
            }
        }

        public uint MaximumClassLength { get; }
        public ushort MaximumNameLength { get; }
        public uint MaximumValueDataLength { get; }
        public uint MaximumValueNameLength { get; }

        /// <summary>
        ///     The name of this key. This is what is shown on the left side of RegEdit in the key and subkey tree.
        /// </summary>
        public string Name { get; }

        public ushort NameLength { get; }
        public byte[] Padding { get; }

        /// <summary>
        ///     The relative offset to the parent key for this record
        /// </summary>
        public uint ParentCellIndex => BitConverter.ToUInt32(RawBytes, 0x14);

        /// <summary>
        ///     The relative offset to the security record for this record
        /// </summary>
        public uint SecurityCellIndex { get; }

        /// <summary>
        ///     When true, this key has been deleted
        ///     <remarks>
        ///         The parent key is determined by checking whether ParentCellIndex 1) exists and 2)
        ///         ParentCellIndex.IsReferenced == true.
        ///     </remarks>
        /// </summary>
        public bool IsDeleted { get; internal set; }

        /// <summary>
        ///     The number of subkeys this key contains
        /// </summary>
        public uint SubkeyCountsStable => BitConverter.ToUInt32(RawBytes, 0x18);

        public uint SubkeyCountsVolatile => BitConverter.ToUInt32(RawBytes, 0x1c);

        /// <summary>
        ///     The relative offset to the root cell this record is linked to.
        /// </summary>
        public uint RootCellIndex { get; }

        public uint HivePointer { get; }
        public int UserFlags { get; }
        public int VirtualControlFlags { get; }

        public uint WorkVar { get; }

        // public properties...
        public long AbsoluteOffset
        {
            get => RelativeOffset + 4096;
            set { }
        }

        public bool IsFree => _size > 0;

        public bool IsReferenced { get; internal set; }
        public byte[] RawBytes { get; }
        public long RelativeOffset { get; }

        public string Signature
        {
            get => Encoding.ASCII.GetString(RawBytes, 4, 2);
            set { }
        }

        public int Size => Math.Abs(_size);

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Size: 0x{Math.Abs(_size):X}");
            sb.AppendLine($"Relative Offset: 0x{RelativeOffset:X}");
            sb.AppendLine($"Absolute Offset: 0x{AbsoluteOffset:X}");
            sb.AppendLine($"Signature: {Signature}");
            sb.AppendLine($"Flags: {Flags}");
            sb.AppendLine();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine();
            sb.AppendLine($"Last Write Timestamp: {LastWriteTimestamp}");
            sb.AppendLine();

            sb.AppendLine($"Is Free: {IsFree}");

            sb.AppendLine();
            sb.AppendLine($"Debug: 0x{Debug:X}");

            sb.AppendLine();
            sb.AppendLine($"Maximum Class Length: 0x{MaximumClassLength:X}");
            sb.AppendLine($"Class Cell Index: 0x{ClassCellIndex:X}");
            sb.AppendLine($"Class Length: 0x{ClassLength:X}");

            sb.AppendLine();

            sb.AppendLine($"Maximum Value Data Length: 0x{MaximumValueDataLength:X}");
            sb.AppendLine($"Maximum Value Name Length: 0x{MaximumValueNameLength:X}");

            sb.AppendLine();
            sb.AppendLine($"Name Length: 0x{NameLength:X}");
            sb.AppendLine($"Maximum Name Length: 0x{MaximumNameLength:X}");

            sb.AppendLine();
            sb.AppendLine($"Parent Cell Index: 0x{ParentCellIndex:X}");
            sb.AppendLine($"Security Cell Index: 0x{SecurityCellIndex:X}");

            sb.AppendLine();
            sb.AppendLine($"Subkey Counts Stable: 0x{SubkeyCountsStable:X}");

            sb.AppendLine();
            sb.AppendLine($"Hive pointer: 0x{HivePointer:X}");
            sb.AppendLine($"Root cell index: 0x{RootCellIndex:X}");

            sb.AppendLine();
            sb.AppendLine($"Subkey Counts Volatile: 0x{SubkeyCountsVolatile:X}");

            sb.AppendLine();
            sb.AppendLine($"User Flags: 0x{UserFlags:X}");
            sb.AppendLine($"Virtual Control Flags: 0x{VirtualControlFlags:X}");
            sb.AppendLine($"Work Var: 0x{WorkVar:X}");


            sb.AppendLine();
            sb.AppendLine($"Padding: {BitConverter.ToString(Padding)}");

            return sb.ToString();
        }
    }
}