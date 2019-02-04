using System;
using System.Linq;
using System.Text;

// namespaces...

namespace Registry.Other
{
    // public classes...
    public class SkSecurityDescriptor
    {
        // public enums...
        [Flags]
        public enum ControlEnum
        {
            SeDaclAutoInherited = 0x0400,
            SeDaclAutoInheritReq = 0x0100,
            SeDaclDefaulted = 0x0008,
            SeDaclPresent = 0x0004,
            SeDaclProtected = 0x1000,
            SeGroupDefaulted = 0x0002,
            SeOwnerDefaulted = 0x0001,
            SeRmControlValid = 0x4000,
            SeSaclAutoInherited = 0x0800,
            SeSaclAutoInheritReq = 0x0200,
            SeSaclDefaulted = 0x0020,
            SeSaclPresent = 0x0010,
            SeSaclProtected = 0x2000,
            SeSelfRelative = 0x8000
        }

        private readonly uint _sizeDacl;
        private readonly uint _sizeGroupSid;
        private readonly uint _sizeOwnerSid;

        private readonly uint _sizeSacl;

        // public constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="SkSecurityDescriptor" /> class.
        /// </summary>
        public SkSecurityDescriptor(byte[] rawBytes)
        {
            RawBytes = rawBytes;

            _sizeSacl = DaclOffset - SaclOffset;
            _sizeDacl = OwnerOffset - DaclOffset;
            _sizeOwnerSid = GroupOffset - OwnerOffset;
            _sizeGroupSid = (uint) (rawBytes.Length - GroupOffset);


            Padding = string.Empty; //TODO VERIFY ITS ALWAYS ZEROs
        }

        // public properties...
        public ControlEnum Control => (ControlEnum) BitConverter.ToUInt16(RawBytes, 0x02);

        public XAclRecord Dacl
        {
            get
            {
                if ((Control & ControlEnum.SeDaclPresent) == ControlEnum.SeDaclPresent)
                {
                    var rawDacl = RawBytes.Skip((int) DaclOffset).Take((int) _sizeDacl).ToArray();
                    return new XAclRecord(rawDacl, XAclRecord.AclTypeEnum.Discretionary);
                }

                return null; //ncrunch: no coverage
            }
        }

        public uint DaclOffset => BitConverter.ToUInt32(RawBytes, 0x10);

        public uint GroupOffset => BitConverter.ToUInt32(RawBytes, 0x08);

        public string GroupSid
        {
            get
            {
                var rawGroup = RawBytes.Skip((int) GroupOffset).Take((int) _sizeGroupSid).ToArray();
                return Helpers.ConvertHexStringToSidString(rawGroup);
            }
        }

        public Helpers.SidTypeEnum GroupSidType => Helpers.GetSidTypeFromSidString(GroupSid);

        public uint OwnerOffset => BitConverter.ToUInt32(RawBytes, 0x04);

        public string OwnerSid
        {
            get
            {
                var rawOwner = RawBytes.Skip((int) OwnerOffset).Take((int) _sizeOwnerSid).ToArray();
                return Helpers.ConvertHexStringToSidString(rawOwner);
            }
        }

        public Helpers.SidTypeEnum OwnerSidType => Helpers.GetSidTypeFromSidString(OwnerSid);

        public string Padding { get; }
        public byte[] RawBytes { get; }

        public byte Revision => RawBytes[0];

        public XAclRecord Sacl
        {
            get
            {
                if ((Control & ControlEnum.SeSaclPresent) == ControlEnum.SeSaclPresent)
                {
                    var rawSacl = RawBytes.Skip((int) SaclOffset).Take((int) _sizeSacl).ToArray();
                    return new XAclRecord(rawSacl, XAclRecord.AclTypeEnum.Security);
                }

                return null;
            }
        }

        public uint SaclOffset => BitConverter.ToUInt32(RawBytes, 0x0c);

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Revision: 0x{Revision:X}");
            sb.AppendLine($"Control: {Control}");

            sb.AppendLine();
            sb.AppendLine($"Owner offset: 0x{OwnerOffset:X}");
            sb.AppendLine($"Owner SID: {OwnerSid}");
            sb.AppendLine($"Owner SID Type: {OwnerSidType}");

            sb.AppendLine();
            sb.AppendLine($"Group offset: 0x{GroupOffset:X}");
            sb.AppendLine($"Group SID: {GroupSid}");
            sb.AppendLine($"Group SID Type: {GroupSidType}");

            if (Dacl != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Dacl Offset: 0x{DaclOffset:X}");
                sb.AppendLine($"DACL: {Dacl}");
            }

            if (Sacl != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Sacl Offset: 0x{SaclOffset:X}");
                sb.AppendLine($"SACL: {Sacl}");
            }

            return sb.ToString();
        }
    }
}