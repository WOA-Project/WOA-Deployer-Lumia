# Registry #

NOTE: This project uses C# version 6 features! You will need VS 2015 to compile it. Alternatively you can just use the DLL from the ExampleApp project.

Full featured, offline Registry parser in C#. 

For discussion and design decisions, see http://binaryforay.blogspot.com/.

## The goals of this project are:  ##

1. full parsing of all known Registry structures
2. Make Registry value slack space accessible
3. Deleted key/value support
4. Easy access to underlying data structures and their raw data as byte arrays
5. Performance
6. Ability to compare results with other Registry parsers using a common format. So far [Willi Ballenthin](https://github.com/williballenthin "Willi Ballenthin") and [Erik Miyake](http://blog.erikmiyake.us/ "Erik Miyake") have implemented this to varying degrees

## General usage  ##
The main Registry class has a LoggingConfiguration propery, NlogConfig. This should be set to a valid NLog config after instantiating a Registry object in order for the class to actually log something. This let you use any of the targets Nlog supports vs anything I could come up with plus the code is a lot cleaner.

If you look at ExampleApp source you can see an example of how to implement both a ColoredConsole and File target.

If you use -v 1 or -v 2 with ExampleApp you will get a large volume of information as the parser does its work. Of course higher levels of verbosity slow things down, but if you run into a problem, its a nice thing to have.

Registry will honor whatever LogLevels exist in your Nlog config.

Once that is set, its as easy as creating a RegistryHive object, deciding on whether or not to recover deleted keys, and calling ParseHive:

```csharp
var hive = new RegistryHive(pathToSomeFile);
hive.RecoverDeleted = true;
hive.ParseHive();
```

There is also a RegistryOnDemand class that forgoes up front processing and only loads things as they are needed via FindKey method. This class can process hives significantly faster than Registry class as it does not handle deleted records and only gets the keys/values for the path specified.

RegistryOnDemand handled several key lookups against a 129MB SOFTWARE hive in less than 2 seconds. The same hive would take approximately 25 seconds to load in Registry.

```csharp
var hive = new RegistryHiveOnDemand(pathToSomeFile);
//returns RegistryKey object
var key = hive.GetKey(@"Local Settings\Software\Microsoft\Windows\CurrentVersion");
//returns null when path not found
var keyBad = hive.GetKey(@"Local Settings\Software\NoSuchKey");
```

Since this is on demand, only the values and subkeys are populated for the retrieved key. If you want to get details on subkeys, be sure to call GetKey on the subkey's key path.

**Example application output**

NTUser.dat hive is 9.74 MB in size. It contains 16,290 keys and 56,945 values. 3,369 deleted keys and 8,963 deleted values were recovered. Of the 8,963 deleted values, only 1,408 (approximately 15.7%) were not reassociated with a deleted key.

All of this was done in 2.24 seconds. The full output from the example app is shown below: 


    1/23/2015 8:52:56 AM -07:00: Processing 'D:\temp\re\NTUSER.DAT'
	1/23/2015 8:52:57 AM -07:00: Initial processing complete. Building tree...
	1/23/2015 8:52:57 AM -07:00: Found root node! Getting subkeys...
	1/23/2015 8:52:57 AM -07:00: Processing complete! Call BuildDeletedRegistryKeys to rebuild deleted record structures
	1/23/2015 8:52:57 AM -07:00: Associating deleted keys and values...
	1/23/2015 8:52:58 AM -07:00: Finished processing 'D:\temp\re\NTUSER.DAT'
	1/23/2015 8:52:58 AM -07:00: Results:
	
	Found 1,928 hbin records
	Found 83,313 Cell records (nk: 18,823, vk: 64,391, sk: 99, lk: 0)
	Found 3,779 List records
	Found 48,863 Data records
	
	There are 70,981 cell records marked as being referenced (85.20 %)
	There are 3,769 list records marked as being referenced (99.74 %)
	There are 41,979 data records marked as being referenced (85.91 %)
	
	Free record info
	12,332 free Cell records (nk: 3,369, vk: 8,963, sk: 0, lk: 0)
	10 free List records
	3,070 free Data records
	
	There were 0 hard parsing errors (a record marked 'in use' that didn't parse correctly.)
	There were 0 soft parsing errors (a record marked 'free' that didn't parse correctly.)
	
	Cells: Free + referenced + marked as in use but not referenced == Total? True
	Lists: Free + referenced + marked as in use but not referenced == Total? True
	Data:  Free + referenced + marked as in use but not referenced == Total? True
	
	Processing took 2.2406 seconds
	
	
	Press any key to continue to next file


**Additional testing metrics**

108 hives processed

Total cell records: 5,200,557<br />
Total free cell records: 6,448 (0.1239867191148948 % free)<br />
	
Total list records: 539,904<br />
Total free list records: 3,495 (0.6473373044096728 % free)<br />
	
Total data records: 3,708,061<br />
Total free data records: 206,423 (5.566871742401217 % free)<br />

Total records: 9,448,522<br />

Total hard parsing errors (record marked as in use): 	152 (0.0016087172152428 % errors)<br />
Total soft parsing errors (record marked as free):	522 (0.0055246735944521 % errors)<br />

***Parsing success rate: 99.99286660919031 %*** <br />



### Example data ###

Find below examples of the kinds of data that will be exposed. Of course, you don't have to deal with any of this if you just want the normal key, subkey and values. The output below is what ToString() generates for each object. All offsets are resolved and the entire hive is accessible via traditional object oriented methods using collections, linq, etc.

**Security Cell Record**

Size: 0xC8<br />
Signature: sk<br />
IsFree: False

FLink: 0x2F88C68<br />
BLink: 0x21D1078

ReferenceCount: 1

Security descriptor length: 0xB0

Security descriptor: Revision: 0x1<br />
Control: SeDaclPresent, SeSaclPresent, SeDaclAutoInherited, SeSaclAutoInherited, SeDaclProtected, SeSelfRelative

Owner offset: 0x94<br />
Owner SID: S-1-5-32-544<br />
Owner SID Type: BuiltinAdministrators

Group offset: 0xA4<br />
Group SID: S-1-5-18<br />
Group SID Type: LocalSystem

DaclrOffset: 0x1C<br />
DACL: ACL Size: 0x2<br />
ACL Type: Discretionary<br />
ACE Records Count: 5

------------ Ace record #0 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: QueryValue, EnumerateSubkeys, Notify, ReadControl<br />
SID: S-1-5-32-545<br />
SID Type: BuiltinUsers<br />
SID Type Description: S-1-5-32-545: A built-in group. After the initial installation of the operating system, the only member is the Authenticated Users group. When a computer joins a domain, the Domain Users group is added to the Users group on the computer.

------------ Ace record #1 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-5-32-544<br />
SID Type: BuiltinAdministrators<br />
SID Type Description: S-1-5-32-544: A built-in group. After the initial installation of the operating system, the only member of the group is the Administrator account. When a computer joins a domain, the Domain Administrators group is added to the Administrators group. When a server becomes a domain controller, the Enterprise Administrators group also is added to the Administrators group.

------------ Ace record #2 ------------<br />
ACE Size: 0x14<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-5-18<br />
SID Type: LocalSystem<br />
SID Type Description: S-1-5-18: An account that is used by the operating system.

------------ Ace record #3 ------------<br />
ACE Size: 0x14<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: FullControl<br />
SID: S-1-3-0<br />
SID Type: CreatorOwner<br />
SID Type Description: S-1-3-0: A placeholder in an inheritable access control entry (ACE). When the ACE is inherited, the system replaces this SID with the SID for the object's creator.

------------ Ace record #4 ------------<br />
ACE Size: 0x18<br />
ACE Type: AccessAllowedAceType<br />
ACE Flags: ContainerInheritAce<br />
Mask: QueryValue, EnumerateSubkeys, Notify, ReadControl<br />
SID: S-1-15-2-1<br />
SID Type: AllAppPackages<br />
SID Type Description: S-1-15-2-1: All applications running in an app package context.

SaclOffset: 0x14<br />
SACL: ACL Size: 0x2<br />
ACL Type: Security<br />
ACE Records Count: 0


**NK Cell Record**

Size: 0x90<br />
Signature: nk<br />
Flags: HiveEntryRootKey, NoDelete, CompressedName

Last Write Timestamp: 11/26/2014 4:42:54 PM -07:00

IsFree: False

Debug: 0x0

MaximumClassLength: 0x0<br />
ClassCellIndex: 0x0<br />
ClassLength: 0x0<br />

MaximumValueDataLength: 0x0<br />
MaximumValueDataLength: 0x0<br />
MaximumValueNameLength: 0x0

NameLength: 0x39<br />
MaximumNameLength: 0x2C<br />
Name: CsiTool-CreateHive-{00000000-0000-0000-0000-000000000000}<br />
Padding: 00-39-00-31-00-45-00

ParentCellIndex: 0x340<br />
SecurityCellIndex: 0xB0

SubkeyCountsStable: 0x1F<br />

SubkeyListsStableCellIndex: 0x2EE0750

SubkeyCountsVolatile: 0x1

UserFlags: 0x0<br />
VirtualControlFlags: 0x0<br />
WorkVar: 0x330038

ValueListCellIndex: 0x0


**Value Key Cell Record**

Size: 0x28<br />
Signature: vk<br />
Data Type: RegSz<br />

IsFree: False

DataLength: 0x4A<br />
OffsetToData: 0x69648E8

NameLength: 0xE<br />
NamePresentFlag: 0x1

ValueName: ReleaseVersion<br />
ValueData: 13.251.9001.1001-140704a-173665E-ATI<br />
ValueDataSlack: 96-06

**LH/LF List records**

Size: 0x10
Signature: lh

IsFree: False

NumberOfEntries: 1

------------ Offset/hash record #0 ------------<br />
Offset: 0x2EE6398, Hash: 4145906403<br />

------------ End of offsets ------------


**RI List record**

Size: 0x18
Signature: ri

IsFree: False

NumberOfEntries: 4

------------ Offset/hash record #0 ------------<br />
Offset: 0xC8F020<br />
------------ Offset/hash record #1 ------------<br />
Offset: 0xCA7020<br />
------------ Offset/hash record #2 ------------<br />
Offset: 0x30C3020<br />
------------ Offset/hash record #3 ------------<br />
Offset: 0x6B53020<br />

------------ End of offsets ------------



