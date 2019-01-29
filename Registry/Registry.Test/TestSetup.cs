using System.Diagnostics;
using NUnit.Framework;

namespace Registry.Test
{
    [SetUpFixture]
    public class TestSetup
    {
        //  public static RegistryHiveOnDemand SamOnDemand;
        //   public static RegistryHiveOnDemand SamHasBigEndianOnDemand;
        // public static RegistryHiveOnDemand SamDupeNameOnDemand;
        //  public static RegistryHiveOnDemand SystemOnDemand;
        //   public static RegistryHiveOnDemand NtUser1OnDemand;
        // public static RegistryHiveOnDemand UsrClassDeletedBagsOnDemand;
        //   public static RegistryHiveOnDemand SoftwareOnDemand;
        //    public static RegistryHive NtUserSlack;
        // public static RegistryHive UsrClass1;
        // public static RegistryHiveOnDemand UsrClass1OnDemand;
        //    public static RegistryHive UsrClassBeef;
        //   public static RegistryHive Bcd;
        //   public static RegistryHive UsrclassDeleted;
        //  public static RegistryHive UsrclassAcronis;
        //   public static RegistryHive Sam;
        //  public static RegistryHive SamRootValue;
        //   public static RegistryHiveOnDemand Security;
        // public static RegistryHiveOnDemand DriversOnDemand;
        //   public static RegistryHiveOnDemand UsrClassFtp;
        // public static RegistryHive System;
        //   public static RegistryHiveOnDemand SanOther;
        //  public static RegistryHive Drivers;

        //ncrunch: no coverage start

        [OneTimeSetUp]
        public void InitializeObjects()
        {
            Debug.WriteLine("Initializing hives...");
            //SamOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM");

            //var SamHasBigEndianOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM_hasBigEndianDWord");
            //   var   SamDupeNameOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SAM_DUPENAME");
            //var NtUser1OnDemand = new RegistryHiveOnDemand(@"..\..\Hives\NTUSER1.DAT");

            //var UsrClassDeletedBagsOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\UsrClassDeletedBags.dat");
            // var SoftwareOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SOFTWARE");
            //  var     SystemOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\SYSTEM");

//          var  Bcd = new RegistryHive(@"..\..\Hives\BCD");
//            Bcd.FlushRecordListsAfterParse = false;
//            Bcd.RecoverDeleted = true;
//            Bcd.ParseHive();

//          var  UsrclassDeleted = new RegistryHive(@"..\..\Hives\UsrClassDeletedBags.dat");
//            UsrclassDeleted.RecoverDeleted = true;
//            UsrclassDeleted.FlushRecordListsAfterParse = false;
//            UsrclassDeleted.ParseHive();

//            var UsrclassAcronis = new RegistryHive(@"..\..\Hives\Acronis_0x52_Usrclass.dat");
//            UsrclassAcronis.RecoverDeleted = true;
//            UsrclassAcronis.FlushRecordListsAfterParse = false;
//            UsrclassAcronis.ParseHive();

//           var UsrClass1 = new RegistryHive(@"..\..\Hives\UsrClass 1.dat");
//            UsrClass1.RecoverDeleted = true;
//            UsrClass1.FlushRecordListsAfterParse = false;
//            UsrClass1.ParseHive();

            //var UsrClass1OnDemand = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass 1.dat");

//         var   UsrClassBeef = new RegistryHive(@"..\..\Hives\UsrClass BEEF000E.dat");
//            UsrClassBeef.RecoverDeleted = true;
//            UsrClassBeef.FlushRecordListsAfterParse = false;
//            UsrClassBeef.ParseHive();

//           var NtUserSlack = new RegistryHive(@"..\..\Hives\NTUSER slack.DAT");
//            NtUserSlack.FlushRecordListsAfterParse = false;
//            NtUserSlack.ParseHive();

//            Sam = new RegistryHive(@"..\..\Hives\SAM");
//            Sam.FlushRecordListsAfterParse = false;
//            Sam.ParseHive();


//           var  Security = new RegistryHiveOnDemand(@"..\..\Hives\SECURITY");
//           var DriversOnDemand = new RegistryHiveOnDemand(@"..\..\Hives\DRIVERS");

//           var Drivers = new RegistryHive(@"..\..\Hives\DRIVERS");
//            Drivers.FlushRecordListsAfterParse = false;
//            Drivers.RecoverDeleted = true;
//            Drivers.ParseHive();

//          var  System = new RegistryHive(@"..\..\Hives\System");
//            System.FlushRecordListsAfterParse = false;
//            System.ParseHive();

//          var  SanOther = new RegistryHiveOnDemand(@"..\..\Hives\SAN(OTHER)");
//          var  UsrClassFtp = new RegistryHiveOnDemand(@"..\..\Hives\UsrClass FTP.dat");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Debug.WriteLine("Unit testing complete. Tearing down...");
        }
    }
}