using System;
using System.Linq;
using NFluent;
using NUnit.Framework;
using Registry.Cells;

namespace Registry.Test
{
    [TestFixture]
    internal class TestVkCellRecord
    {
        [Test]
        public void ShouldFindKeyValueAndCheckProperties()
        {
            var sam = new RegistryHive(@"..\..\..\Hives\SAM");
            sam.FlushRecordListsAfterParse = false;
            sam.ParseHive();

            var key =
                sam.GetKey(0x418);

            Check.That(key).IsNotNull();

            Check.That(key.ToString()).IsNotEmpty();

            var val = key.Values[0];

            //TODO Need to export to reg each kind too

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsEmpty();
            Check.That(val.ValueSlack).IsEmpty();
            Check.That(val.ValueSlackRaw).IsEmpty();
            Check.That(val.ToString()).IsNotEmpty();
            Check.That(val.ValueName).IsEqualTo("(default)");
            Check.That(val.ValueType).IsEqualTo("RegNone");
            Check.That(val.ValueData).IsEqualTo("");
            Check.That(val.ValueSlack).IsEqualTo("");
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x270);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x1270);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.DataLength).IsEqualTo(0x80000000);
            Check.That(val.VkRecord.OffsetToData).IsEqualTo(0);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0);
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0);

            //This key has slack
            key =
                sam.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Users\000001F4");

            Check.That(key).IsNotNull();

            val = key.Values[0];

            Check.That(val).IsNotNull();

            Check.That(val.ValueName).IsNotEmpty();
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueSlack).IsNotEmpty();
            Check.That(val.ValueSlackRaw.Length).IsStrictlyGreaterThan(0);
            Check.That(val.ToString()).IsNotEmpty();

            Check.That(val.ValueName).IsEqualTo("F");
            Check.That(val.ValueData).IsNotEmpty();
            Check.That(val.ValueData.Length).IsEqualTo(239);
            Check.That(val.ValueSlack).IsNotEmpty();
            Check.That(val.ValueSlack.Length).IsEqualTo(11);
            Check.That(val.ValueSlackRaw.Length).IsEqualTo(4);
            Check.That(val.ToString()).IsNotEmpty();

            Check.That(val.ValueType).IsEqualTo("RegBinary");
            Check.That(val.ValueData)
                .IsEqualTo(
                    "02-00-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-FF-FF-FF-FF-FF-FF-FF-7F-00-00-00-00-00-00-00-00-F4-01-00-00-01-02-00-00-10-02-00-00-00-00-00-00-00-00-00-00-01-00-00-00-00-00-00-00-73-00-00-00");
            Check.That(val.ValueSlack).IsEqualTo("1F-00-0F-00");
            Check.That(val.VkRecord.Size).IsEqualTo(-32);
            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x39B8);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x49B8);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.DataLength).IsEqualTo(0x50);
            Check.That(val.VkRecord.OffsetToData).IsEqualTo(0x39D8);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x1);
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(1);
            Check.That(val.VkRecord.Padding.Length).IsEqualTo(7);
        }

        [Test]
        public void ShouldFindRegBigEndianDWordValues()
        {
            var samHasBigEndianOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM_hasBigEndianDWord");
            var key =
                samHasBigEndianOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Aliases");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "(default)");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDwordBigEndian);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 0);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegBinaryValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Appearance\Schemes");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "@themeui.dll,-850");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(712);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);


            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Desktop\WindowMetrics");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "IconFont");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(92);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Mouse");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "SmoothMouseXCurve");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(40);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);


            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\GlobalPowerPolicy");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Policies");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(176);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Input Method\Hot Keys\00000010");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Key Modifiers");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegDWordValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Wisp\Pen\SysEventParameters");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "DblDist");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 20);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\Windows");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "UserSelectedDefault");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 0);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows NT\CurrentVersion\MsiCorruptedFileRecovery\RepairedProducts");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "TimeWindowMinutes");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 1440);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "MaxArchiveCount");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 500);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Console");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "ColorTable11");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((uint) 16776960);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
        }

        [Test]
        public void ShouldFindRegExpandSzValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Environment");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "TEMP");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo(@"%USERPROFILE%\AppData\Local\Temp");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\Cursors");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Arrow");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo(@"%SystemRoot%\cursors\aero_arrow.cur");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\AppEvents\Schemes\Apps\.Default\WindowsUAC\.Current");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "(default)");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo(@"%SystemRoot%\media\Windows User Account Control.wav");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Themes");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastHighContrastTheme");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo(@"%SystemRoot%\resources\Ease of Access Themes\hcblack.theme");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\ThemeManager");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "DllName");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegExpandSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo(@"%SystemRoot%\resources\themes\Aero\Aero.msstyles");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(2);
        }

        [Test]
        public void ShouldFindRegMultiSzValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\International\User Profile");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "Languages");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("en-US");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);


            var usrclassAcronis = new RegistryHive(@"..\..\..\Hives\Acronis_0x52_Usrclass.dat");
            usrclassAcronis.RecoverDeleted = true;
            usrclassAcronis.FlushRecordListsAfterParse = false;
            usrclassAcronis.ParseHive();

            key =
                usrclassAcronis.GetKey(
                    @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\MuiCache\12\52C64B7E");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LanguageList");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("en-US en");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            var bcd = new RegistryHive(@"..\..\..\Hives\BCD");
            bcd.FlushRecordListsAfterParse = false;
            bcd.RecoverDeleted = true;
            bcd.ParseHive();

            key =
                bcd.GetKey(
                    @"System\Objects\{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}\Elements\14000006");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Element");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VkRecord.ValueData)
                .IsEqualTo("{4636856e-540f-4170-a130-a84776f4c654} {0ce4991b-e6b3-4b16-b23c-5e0d9250e5d9}");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                bcd.GetKey(
                    @"System\Objects\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\Elements\14000006");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Element");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("{7ea2e1ac-2e61-4728-aaa3-896d9d0a9f0e}");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(84);
        }

        [Test]
        public void ShouldFindRegQWordValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\Windows Error Reporting");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "LastWatsonCabUploaded");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 130557640214774914);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "BannedAppsLastModified");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 0);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            var usrclassAcronis = new RegistryHive(@"..\..\..\Hives\Acronis_0x52_Usrclass.dat");
            usrclassAcronis.RecoverDeleted = true;
            usrclassAcronis.FlushRecordListsAfterParse = false;
            usrclassAcronis.ParseHive();

            key =
                usrclassAcronis.GetKey(
                    @"S-1-5-21-3851833874-1800822990-1357392098-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 130294002389413697);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();
            key =
                usrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\TrayNotify");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastAdvertisement");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 130672934390152518);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);

            var ntUserSlack = new RegistryHive(@"..\..\..\Hives\NTUSER slack.DAT");
            ntUserSlack.FlushRecordListsAfterParse = false;
            ntUserSlack.ParseHive();

            key =
                ntUserSlack.GetKey(
                    @"$$$PROTO.HIV\Software\Microsoft\VisualStudio\7.0\External Tools");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "LastMerge");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 127257359392030000);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);
        }

        [Test]
        public void ShouldFindRegSzValues()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\Assemblies\0x00000409\{34745C63-B2F0-4784-8B67-5E12C8701A31}");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "Default");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("{00000000-0000-0000-0000-000000000000}");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(6);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\CTF\SortOrder\Language");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "00000000");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("00000409");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Speech\Preferences\AppCompatDisableDictation");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "dwm.exe");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\EUDC\932");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "SystemDefaultEUDCFont");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.ValueData).IsEqualTo("EUDC.TTE");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(2);

            key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Control Panel\PowerCfg\PowerPolicies\4");

            Check.That(key).IsNotNull();

            val = key.Values.Single(t => t.ValueName == "Description");

            Check.That(val).IsNotNull();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.ValueData)
                .IsEqualTo("This scheme keeps the computer on and optimizes it for high performance.");
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(2);
        }

        [Test]
        public void TestUnicodeNameWhereSupposedToBeAscii()
        {
            var ntUserSlack = new RegistryHive(@"..\..\..\Hives\NTUSER slack.DAT");
            ntUserSlack.FlushRecordListsAfterParse = false;
            ntUserSlack.ParseHive();


            var key = ntUserSlack.CellRecords[0x293490] as VkCellRecord;

            Check.That(key).IsNotNull();
            Check.That(key.ValueName).IsNotEmpty();
            Check.That(key.ValueData).IsNotNull();
        }

        [Test]
        public void TestVkRecordBigData()
        {
            var softwareOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SOFTWARE");
            var key =
                softwareOnDemand.GetKey(
                    @"CMI-CreateHive{199DAFC2-6F16-4946-BF90-5A3FC3A60902}\\Microsoft\\SystemCertificates\\AuthRoot\\AutoUpdate");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(t => t.ValueName == "EncodedCtl");

            Check.That(val).IsNotNull();

            Check.That(val.ValueDataRaw.Length).Equals(123820);
        }

        [Test]
        public void TestVkRecordFileTimeRegType()
        {
            var systemOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SYSTEM");
            var key =
                systemOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\ControlSet001\Control\DeviceContainers\{00000000-0000-0000-FFFF-FFFFFFFFFFFF}\Properties\{3464f7a4-2444-40b1-980a-e0903cb6d912}\0008");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "en-US");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x78170);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x79170);
            Check.That(val.VkRecord.Size).IsEqualTo(-32);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x1);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x5);
            Check.That(val.VkRecord.ValueName).IsEqualTo("en-US");
            Check.That(val.VkRecord.ValueData).IsInstanceOf<DateTimeOffset>();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegFileTime);
            Check.That(val.VkRecord.DataTypeRaw).IsEqualTo(0x0010);
            Check.That(val.VkRecord.DataLength).Equals((uint) 0x8);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x77d78);
            Check.That(val.VkRecord.Padding.Length).Equals(3);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(8);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordIsFreeDataBlockExceptions()
        {
            var usrClass1 = new RegistryHive(@"..\..\..\Hives\UsrClass 1.dat");
            usrClass1.RecoverDeleted = true;
            usrClass1.FlushRecordListsAfterParse = false;
            usrClass1.ParseHive();


            var key = usrClass1.CellRecords[0x406180] as VkCellRecord;

            Check.That(key).IsNotNull();
            Check.That(key.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(key.ValueData).IsNotNull();
        }

        [Test]
        public void TestVkRecordIsFreeLessDataThanDataLength2()
        {
            var usrclassAcronis = new RegistryHive(@"..\..\..\Hives\Acronis_0x52_Usrclass.dat");
            usrclassAcronis.RecoverDeleted = true;
            usrclassAcronis.FlushRecordListsAfterParse = false;
            usrclassAcronis.ParseHive();

            var val = usrclassAcronis.CellRecords[0x3f78] as VkCellRecord;

            Check.That(val).IsNotNull();

            Check.That(val.RelativeOffset).IsEqualTo(0x3f78);
            Check.That(val.AbsoluteOffset).IsEqualTo(0x4f78);
            Check.That(val.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.Signature).IsEqualTo("vk");
            Check.That(val.IsFree).IsTrue();
            Check.That(val.NamePresentFlag).IsEqualTo(0x1);
            Check.That(val.NameLength).IsEqualTo(37);
            Check.That(val.ValueName).IsEqualTo(@"@C:\Windows\System32\netcenter.dll,-2");
            Check.That(val.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.DataTypeRaw).IsEqualTo(1);
            Check.That(val.DataLength).Equals((uint) 196);
            Check.That(val.OffsetToData).Equals((uint) 61872);
            Check.That(val.Padding.Length).Equals(3);
            Check.That(val.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.ValueDataRaw.Length).IsEqualTo(76);
            Check.That(val.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordQWordWithLengthOfZero()
        {
            var samDupeNameOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM_DUPENAME");
            var key =
                samDupeNameOnDemand.GetKey(
                    @"SAM\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-4271176276-4210259494-4108073714");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0xA88);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x1A88);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x0);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.DataTypeRaw).IsEqualTo(11);
            Check.That(val.VkRecord.DataLength).Equals(0x80000000);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<ulong>();
            Check.That(val.VkRecord.ValueData).IsEqualTo((ulong) 0);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegBinary()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key =
                samOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "F");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x3078);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x4078);
            Check.That(val.VkRecord.Size).IsEqualTo(-32);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x01);
            Check.That(val.VkRecord.NameLength).IsEqualTo(1);
            Check.That(val.VkRecord.ValueName).IsEqualTo("F");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.DataLength).Equals((uint) 0xf0);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x3098);
            Check.That(val.VkRecord.Padding.Length).Equals(7);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(240);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegBinaryDeletedValue()
        {
            var usrclassDeleted = new RegistryHive(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            usrclassDeleted.RecoverDeleted = true;
            usrclassDeleted.FlushRecordListsAfterParse = false;
            usrclassDeleted.ParseHive();

            var key =
                usrclassDeleted.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\Software\Microsoft\Windows\Shell\BagMRU\1");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "0");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x5328);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x6328);
            Check.That(val.VkRecord.Size).IsEqualTo(0x100);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsTrue();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x01);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x1);
            Check.That(val.VkRecord.ValueName).IsEqualTo("0");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegBinary);
            Check.That(val.VkRecord.DataLength).Equals((uint) 0xE);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x5348);
            Check.That(val.VkRecord.Padding.Length).Equals(7);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(94);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(14);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegDWord()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key =
                samOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\LastSkuUpgrade");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x258);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x1258);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x00);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegDword);
            Check.That(val.VkRecord.DataLength).Equals(0x80000004);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x07);
            Check.That(val.ValueData).Equals("7");
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<uint>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegMultiSz()
        {
            var usrClassDeletedBagsOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClassDeletedBags.dat");
            var key =
                usrClassDeletedBagsOnDemand.GetKey(
                    @"S-1-5-21-146151751-63468248-1215037915-1000_Classes\Local Settings\MuiCache\6\52C64B7E");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "LanguageList");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x5f0);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x15f0);
            Check.That(val.VkRecord.Size).IsEqualTo(-40);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x01);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0xC);
            Check.That(val.VkRecord.ValueName).IsEqualTo("LanguageList");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegMultiSz);
            Check.That(val.VkRecord.DataLength).Equals((uint) 0x14);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0xf70);
            Check.That(val.ValueData).Equals("en-US en");
            Check.That(val.VkRecord.Padding.Length).Equals(4);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<string>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(20);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegNone()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key =
                samOnDemand.GetKey(@"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x270);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x1270);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x00);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegNone);
            Check.That(val.VkRecord.DataLength).Equals(0x80000000);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x0);
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegqWord()
        {
            var ntUser1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\NTUSER1.DAT");
            var key =
                ntUser1OnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\Software\Microsoft\Windows\CurrentVersion\Store\RefreshBannedAppList");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "BannedAppsLastModified");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x5ce0);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x6ce0);
            Check.That(val.VkRecord.Size).IsEqualTo(-48);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x01);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x16);
            Check.That(val.VkRecord.ValueName).IsEqualTo("BannedAppsLastModified");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegQword);
            Check.That(val.VkRecord.DataLength).Equals((uint) 0x8);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x3b70);
            Check.That(val.ValueData).Equals("0");
            Check.That(val.VkRecord.Padding.Length).Equals(2);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<ulong>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(8);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegSz()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key =
                samOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Builtin\Aliases\Members\S-1-5-21-727398572-3617256236-2003601904\00000201");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0xFE0);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x1FE0);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x00);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegSz);
            Check.That(val.VkRecord.DataLength).Equals(0x80000004);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0x0221);
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<string>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordRegUnknown()
        {
            var samHasBigEndianOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM_hasBigEndianDWord");
            var key =
                samHasBigEndianOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account\Groups\Names\None");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x1248);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x2248);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x0);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegUnknown);
            Check.That(val.VkRecord.DataTypeRaw).IsEqualTo(513);
            Check.That(val.VkRecord.DataLength).Equals(0x80000000);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }

        [Test]
        public void TestVkRecordUnknownRegType()
        {
            var samDupeNameOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM_DUPENAME");
            var key = samDupeNameOnDemand.GetKey(@"SAM\SAM\Domains\Account\Users");

            Check.That(key).IsNotNull();

            var val = key.Values.Single(e => e.ValueName == "(default)");

            Check.That(val).IsNotNull();

            Check.That(val.VkRecord.RelativeOffset).IsEqualTo(0x1880);
            Check.That(val.VkRecord.AbsoluteOffset).IsEqualTo(0x2880);
            Check.That(val.VkRecord.Size).IsEqualTo(-24);
            Check.That(val.VkRecord.Signature).IsEqualTo("vk");
            Check.That(val.VkRecord.IsFree).IsFalse();
            Check.That(val.VkRecord.NamePresentFlag).IsEqualTo(0x0);
            Check.That(val.VkRecord.NameLength).IsEqualTo(0x0);
            Check.That(val.VkRecord.ValueName).IsEqualTo("(default)");
            Check.That(val.VkRecord.ValueData).IsInstanceOf<byte[]>();
            Check.That(val.VkRecord.DataType).IsEqualTo(VkCellRecord.DataTypeEnum.RegUnknown);
            Check.That(val.VkRecord.DataTypeRaw).IsEqualTo(15);
            Check.That(val.VkRecord.DataLength).Equals(0x80000000);
            Check.That(val.VkRecord.OffsetToData).Equals((uint) 0);
            Check.That(val.VkRecord.Padding.Length).Equals(0);
            Check.That(val.VkRecord.ValueDataRaw.Length).IsEqualTo(4);
            Check.That(val.VkRecord.ValueDataSlack.Length).IsEqualTo(0);
            Check.That(val.VkRecord.ToString()).IsNotEmpty();
        }
    }
}