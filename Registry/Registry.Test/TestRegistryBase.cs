using System;
using System.IO;
using NFluent;
using NLog;
using NUnit.Framework;
using Registry.Other;

namespace Registry.Test
{
    [TestFixture]
    public class TestRegistryBase
    {
        [SetUp]
        public void PreTestSetup()
        {
            LogManager.Configuration = null;
        }

        [Test]
        public void BcdHiveShouldHaveBcdHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\BCD");
            Check.That(HiveTypeEnum.Bcd).IsEqualTo(r.HiveType);
        }

        [Test]
        public void DriversHiveShouldHaveDriversHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\Drivers");
            Check.That(HiveTypeEnum.Drivers).IsEqualTo(r.HiveType);
        }

        [Test]
        public void FileNameNotFoundShouldThrowFileNotFoundException()
        {
            Check.ThatCode(() => { new RegistryBase(@"c:\this\file\does\not\exist.reg"); })
                .Throws<FileNotFoundException>();
        }

        [Test]
        public void FileNameNotFoundShouldThrowNotSupportedException()
        {
            Check.ThatCode(() => { new RegistryBase(); }).Throws<NotSupportedException>();
        }

        [Test]
        public void HivePathShouldReflectWhatIsPassedIn()
        {
            var security = new RegistryHiveOnDemand(@"..\..\..\Hives\SECURITY");

            Check.That(security.HivePath).IsEqualTo(@"..\..\..\Hives\SECURITY");
        }

        [Test]
        public void InvalidRegistryHiveShouldThrowException()
        {
            Check.ThatCode(() => { new RegistryBase(@"..\..\..\Hives\NotAHive"); }).Throws<Exception>();
        }


        [Test]
        public void NtuserHiveShouldHaveNtuserHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\NTUSER.DAT");
            Check.That(HiveTypeEnum.NtUser).IsEqualTo(r.HiveType);
        }

        [Test]
        public void NullByteArrayShouldThrowEArgumentNullException()
        {
            byte[] nullBytes = null;
            Check.ThatCode(() => { new RegistryBase(nullBytes,null); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void NullFileNameShouldThrowEArgumentNullException()
        {
            string nullFileName = null;
            Check.ThatCode(() => { new RegistryBase(nullFileName); }).Throws<ArgumentNullException>();
        }

        [Test]
        public void OtherHiveShouldHaveOtherHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\SAN(OTHER)");
            Check.That(HiveTypeEnum.Other).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SamHiveShouldHaveSamHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\SAM");
            Check.That(HiveTypeEnum.Sam).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SecurityHiveShouldHaveSecurityHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\Security");
            Check.That(HiveTypeEnum.Security).IsEqualTo(r.HiveType);
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

            var r = new RegistryBase(fileBytes,@"..\..\..\Hives\SAM");

            Check.That(r.Header).IsNotNull();
            Check.That(r.HivePath).IsEqualTo("None");
            Check.That(r.HiveType).IsEqualTo(HiveTypeEnum.Sam);
        }

        [Test]
        public void ShouldThrowExceptionWhenNotRegistryHiveAndByteArray()
        {
            var fileStream = new FileStream(@"..\..\..\Hives\NotAHive", FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            var fileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            Check.ThatCode(() =>
                {
                    var rb = new RegistryBase(fileBytes,@"..\..\..\Hives\NotAHive");
                })
                .Throws<ArgumentException>();
        }

        [Test]
        public void SoftwareHiveShouldHaveSoftwareHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\software");
            Check.That(HiveTypeEnum.Software).IsEqualTo(r.HiveType);
        }

        [Test]
        public void SystemHiveShouldHaveSystemHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\system");
            Check.That(HiveTypeEnum.System).IsEqualTo(r.HiveType);
        }

        [Test]
        public void UsrclassHiveShouldHaveUsrclassHiveType()
        {
            var r = new RegistryBase(@"..\..\..\Hives\UsrClass 1.dat");
            Check.That(HiveTypeEnum.UsrClass).IsEqualTo(r.HiveType);
        }

        [Test]
        public void Windows10ExtraData()
        {
            var r = new RegistryBase(@"D:\SynologyDrive\Registry\SOFTWARE_win10");
            Check.That(r.Header.KtmFlags).IsEqualTo(KtmFlag.Unset);
            Check.That(r.Header.LastReorganizedTimestamp.HasValue).IsTrue();
        }
    }
}