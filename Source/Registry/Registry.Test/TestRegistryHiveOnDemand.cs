using System.IO;
using NFluent;
using NUnit.Framework;

namespace Registry.Test
{
    [TestFixture]
    internal class TestRegistryHiveOnDemand
    {
        [Test]
        public void GetKeyShouldBeNullWithNonExistentPath()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key = samOnDemand.GetKey(@"SAM\Domains\Account\This\Does\Not\Exist");

            Check.That(key).IsNull();
        }

        [Test]
        public void GetKeyShouldNotBeNullWithFullPath()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key =
                samOnDemand.GetKey(
                    @"CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}\SAM\Domains\Account");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPath()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key = samOnDemand.GetKey(@"SAM\Domains\Account");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void GetKeyShouldNotBeNullWithShortPathMixedSpelling()
        {
            var samOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");
            var key = samOnDemand.GetKey(@"SAM\DomAins\AccoUnt");

            Check.That(key).IsNotNull();
        }

        [Test]
        public void ShouldProcessLiListRecord()
        {
            var usrClass1OnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClass 1.dat");

            var key =
                usrClass1OnDemand.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\\ActivatableClasses\\Package\\Microsoft.BingSports_3.0.4.244_x64__8wekyb3d8bbwe\\ActivatableClassId");

            Check.That(key).IsNotNull();
            Check.That(key.NkRecord.SubkeyCountsStable).IsEqualTo(0x224);
        }

        [Test]
        public void ShouldTakeByteArrayInConstructor()
        {
            var fileStream = new FileStream(@"..\..\..\Hives\SAM", FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            var fileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            var r = new RegistryHiveOnDemand(fileBytes,@"..\..\..\Hives\SAM");

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

        [Test]
        public void TestFileNameConstructor()
        {
            var r = new RegistryHiveOnDemand(@"..\..\..\Hives\SAM");

            Check.That(r.Header).IsNotNull();
        }

        [Test]
        public void TestsListRecords()
        {
            var driversOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\DRIVERS");

            var key =
                driversOnDemand.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}\DriverDatabase\DeviceIds");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(3878);
        }

        [Test]
        public void TestsListRecordsContinued()
        {
            var driversOnDemand = new RegistryHiveOnDemand(@"..\..\..\Hives\DRIVERS");


            var key = driversOnDemand.GetKey(@"{15a87b70-bc78-114a-95b7-b90ca5d0ec00}");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(1);
        }

        [Test]
        public void TestsListRecordsContinued3()
        {
            var usrClassFtp = new RegistryHiveOnDemand(@"..\..\..\Hives\UsrClass FTP.dat");

            var key =
                usrClassFtp.GetKey(
                    @"S-1-5-21-2417227394-2575385136-2411922467-1105_Classes\ActivatableClasses\CLSID");

            Check.That(key).IsNotNull();
            Check.That(key.SubKeys.Count).IsEqualTo(2811);
        }
    }
}