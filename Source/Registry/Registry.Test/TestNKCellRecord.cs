using NFluent;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test
{
    [TestFixture]
    internal class TestNkCellRecord
    {
        [Test]
        public void ShouldHavePaddingLengthOfZeroWhenRecordIsFree()
        {
            var bcd = new RegistryHive(@"..\..\..\Hives\BCD");
            bcd.FlushRecordListsAfterParse = false;
            bcd.RecoverDeleted = true;
            bcd.ParseHive();

            var key = bcd.GetKey(0x10e8);

            Check.That(key).IsNotNull();
            Check.That(key.NkRecord.Padding.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldHaveUnableToDetermineName()
        {
            var usrClassBeef = new RegistryHive(@"..\..\..\Hives\UsrClass BEEF000E.dat");
            usrClassBeef.RecoverDeleted = true;
            usrClassBeef.FlushRecordListsAfterParse = false;
            usrClassBeef.ParseHive();

            var key = usrClassBeef.CellRecords[0x783CD8] as NkCellRecord;

            Check.That(key).IsNotNull();

            Check.That(key.Padding.Length).IsEqualTo(0);
            Check.That(key.Name).IsEqualTo("(Unable to determine name)");
        }

        [Test]
        public void ShouldVerifyNkRecordProperties()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.NkRecord.Padding.Length).IsStrictlyGreaterThan(0);
            Check.That(key.NkRecord.ToString()).IsNotEmpty();
            Check.That(key.NkRecord.SecurityCellIndex).IsStrictlyGreaterThan(0);
            Check.That(key.NkRecord.SubkeyListsVolatileCellIndex).IsEqualTo(0);

            Check.That(key.KeyName).IsEqualTo("Domains");
            Check.That(key.KeyPath).IsEqualTo(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");
            Check.That(key.LastWriteTime.ToString()).IsEqualTo("7/3/2014 6:05:37 PM +00:00");
            Check.That(key.NkRecord.Size).IsEqualTo(0x58);
            Check.That(key.NkRecord.RelativeOffset).IsEqualTo(0x418);
            Check.That(key.NkRecord.AbsoluteOffset).IsEqualTo(0x1418);
            Check.That(key.NkRecord.Signature).IsEqualTo("nk");
            Check.That(key.NkRecord.IsFree).IsFalse();
            Check.That(key.NkRecord.Debug).IsEqualTo(0);
            Check.That(key.NkRecord.MaximumClassLength).IsEqualTo(0);
            Check.That(key.NkRecord.ClassCellIndex).IsEqualTo(0);
            Check.That(key.NkRecord.ClassLength).IsEqualTo(0);
            Check.That(key.NkRecord.MaximumValueDataLength).IsEqualTo(0);
            Check.That(key.NkRecord.MaximumValueNameLength).IsEqualTo(0);
            Check.That(key.NkRecord.NameLength).IsEqualTo(7);
            Check.That(key.NkRecord.MaximumNameLength).IsEqualTo(0xE);
            Check.That(key.NkRecord.ParentCellIndex).IsEqualTo(0xB0);
            Check.That(key.NkRecord.SecurityCellIndex).IsEqualTo(0x108);
            Check.That(key.NkRecord.SubkeyCountsStable).IsEqualTo(0x2);
            Check.That(key.NkRecord.SubkeyListsStableCellIndex).IsEqualTo(0x4580);
            Check.That(key.NkRecord.SubkeyCountsVolatile).IsEqualTo(0);
            Check.That(key.NkRecord.UserFlags).IsEqualTo(NkCellRecord.UserFlag.None);
            Check.That(key.NkRecord.VirtualControlFlags).IsEqualTo(NkCellRecord.VirtualizationControlFlag.None);
            Check.That(key.NkRecord.Access).IsEqualTo(NkCellRecord.AccessFlag.PreInitAccess);
            Check.That(key.NkRecord.WorkVar).IsEqualTo(0);
            Check.That(key.NkRecord.ValueListCount).IsEqualTo(1);
            Check.That(key.NkRecord.ValueListCellIndex).IsEqualTo(0x1f0);
            Check.That(key.NkRecord.Padding.Length).IsEqualTo(1);

            //Key flags: HasActiveParent
            //
            //Flags: CompressedName
        }
    }
}