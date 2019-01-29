using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;
using static Registry.Other.Helpers;

namespace Registry
{
    // public classes...
    public class RegistryHive : RegistryBase
    {
        internal static int HardParsingErrorsInternal;
        internal static int SoftParsingErrorsInternal;
        private readonly Dictionary<string, RegistryKey> _keyPathKeyMap = new Dictionary<string, RegistryKey>();
        private readonly Dictionary<long, RegistryKey> _relativeOffsetKeyMap = new Dictionary<long, RegistryKey>();
        private bool _parsed;

        /// <summary>
        ///     If true, CellRecords and ListRecords will be purged to free memory
        /// </summary>
        public bool FlushRecordListsAfterParse = true;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="Registry" />
        ///     class.
        /// </summary>
        public RegistryHive(string hivePath) : base(hivePath)
        {
            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();

            DeletedRegistryKeys = new List<RegistryKey>();
            UnassociatedRegistryValues = new List<KeyValue>();
        }

        public RegistryHive(byte[] rawBytes, string filePath) : base(rawBytes,filePath)
        {
            CellRecords = new Dictionary<long, ICellTemplate>();
            ListRecords = new Dictionary<long, IListTemplate>();

            DeletedRegistryKeys = new List<RegistryKey>();
            UnassociatedRegistryValues = new List<KeyValue>();
        }

        public bool RecoverDeleted { get; set; }

        /// <summary>
        ///     Contains all recovered
        /// </summary>
        public List<RegistryKey> DeletedRegistryKeys { get; private set; }

        public List<KeyValue> UnassociatedRegistryValues { get; }

        /// <summary>
        ///     List of all NK, VK, and SK cell records, both in use and free, as found in the hive
        /// </summary>
        public Dictionary<long, ICellTemplate> CellRecords { get; }

        /// <summary>
        ///     The total number of record parsing errors where the records were IsFree == false
        /// </summary>
        public int HardParsingErrors => HardParsingErrorsInternal;

        public uint HBinRecordTotalSize { get; private set; } //Dictionary<long, HBinRecord>
        public int HBinRecordCount { get; private set; } //Dictionary<long, HBinRecord>

        /// <summary>
        ///     List of all DB, LI, RI, LH, and LF list records, both in use and free, as found in the hive
        /// </summary>
        public Dictionary<long, IListTemplate> ListRecords { get; }

        public RegistryKey Root { get; private set; }

        /// <summary>
        ///     The total number of record parsing errors where the records were IsFree == true
        /// </summary>
        public int SoftParsingErrors => SoftParsingErrorsInternal;

        private void DumpKeyCommonFormat(RegistryKey key, StreamWriter sw, ref int keyCount,
            ref int valueCount)
        {
            if ((key.KeyFlags & RegistryKey.KeyFlagsEnum.HasActiveParent) == RegistryKey.KeyFlagsEnum.HasActiveParent &&
                (key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) == RegistryKey.KeyFlagsEnum.Deleted)
            {
                return;
            }

            foreach (var subkey in key.SubKeys)
            {
                if ((subkey.KeyFlags & RegistryKey.KeyFlagsEnum.HasActiveParent) ==
                    RegistryKey.KeyFlagsEnum.HasActiveParent &&
                    (subkey.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) == RegistryKey.KeyFlagsEnum.Deleted)
                {
                    return;
                }

                keyCount += 1;

                sw.WriteLine("key|{0}|{1}|{2}|{3}", subkey.NkRecord.IsFree ? "U" : "A",
                    subkey.NkRecord.AbsoluteOffset, subkey.KeyPath,
                    subkey.LastWriteTime.Value.UtcDateTime.ToString("o"));

                foreach (var val in subkey.Values)
                {
                    valueCount += 1;

                    sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VkRecord.IsFree ? "U" : "A",
                        val.VkRecord.AbsoluteOffset, subkey.KeyName, val.ValueName, (int) val.VkRecord.DataType,
                        BitConverter.ToString(val.VkRecord.ValueDataRaw).Replace("-", " "));
                }

                DumpKeyCommonFormat(subkey, sw, ref keyCount, ref valueCount);
            }
        }

        private DataNode GetDataNodeFromOffset(long relativeOffset)
        {
            var dataLenBytes = ReadBytesFromHive(relativeOffset + 4096, 4);
            var dataLen = BitConverter.ToUInt32(dataLenBytes, 0);
            var size = (int) dataLen;
            size = Math.Abs(size);

            var dn = new DataNode(ReadBytesFromHive(relativeOffset + 4096, size), relativeOffset);

            return dn;
        }

        public byte[] ProcessTransactionLogs(List<TransactionLogFileInfo> logFileInfos, bool updateExistingData = false)
        {
             if (logFileInfos.Count == 0)
            {
                throw new Exception("No logs were supplied");
            }

            if (Header.PrimarySequenceNumber == Header.SecondarySequenceNumber)
            {
                throw new Exception("Sequence numbers match! Hive is not dirty");
            }

            var bytes = FileBytes;

            var logs = new List<TransactionLog>();

            foreach (var logFile in logFileInfos)
            {
                if (logFile.FileBytes.Length == 0)
                {
                    continue;
                }

                var transLog = new TransactionLog(logFile.FileBytes,logFile.FileName);

                if (HiveType != transLog.HiveType)
                {
                    throw new Exception(
                        $"Transaction log contains a type ({transLog.HiveType}) that is different from the Registry hive ({HiveType})");
                }

                if (transLog.Header.PrimarySequenceNumber < Header.SecondarySequenceNumber)
                {
                    //log predates the last confirmed update, so skip  
                    Logger.Warn(
                        $"Dropping {logFile.FileBytes} because the log's header.PrimarySequenceNumber is less than the hive's header.SecondarySequenceNumber");
                    continue;
                }

                transLog.ParseLog();

                logs.Add(transLog);
            }

            var wasUpdated = false;
            var maximumSequenceNumber = 0;

            //get first and second, do the compares

            var logOne = logs.SingleOrDefault(t => t.LogPath.EndsWith("log1", StringComparison.OrdinalIgnoreCase));
            var logTwo = logs.SingleOrDefault(t => t.LogPath.EndsWith("log2", StringComparison.OrdinalIgnoreCase));
            TransactionLog soloLog = null;

            if (logOne != null && logTwo != null)
            {
                //both sent in, compare sequence #s for higher of the two

                Logger.Info("Two transaction logs found. Determining primary log...");

                TransactionLog firstLog;
                TransactionLog secondLog;

                //Find the one with the lower sequence numbers as it contains older data than the other one
                if (logOne.Header.PrimarySequenceNumber >= logTwo.Header.PrimarySequenceNumber)
                {
                    firstLog = logTwo;
                    secondLog = logOne;
                }
                else
                {
                    firstLog = logOne;
                    secondLog = logTwo;
                }

                Logger.Info($"Primary log: {firstLog.LogPath}, secondary log: {secondLog.LogPath}");

                //start with the first log and replay it.
                //if second log's primary seq number is one more than firstLogs LAST, replay it as well
                if (Header.ValidateCheckSum() &&
                    firstLog.Header.PrimarySequenceNumber >= Header.SecondarySequenceNumber)
                {
                    Logger.Info($"Replaying log file: {firstLog.LogPath}");
                    //we can replay the log
                    bytes = firstLog.UpdateHiveBytes(bytes);
                    wasUpdated = true;
                }
                else
                {
                    bytes = secondLog.UpdateHiveBytes(bytes);
                    wasUpdated = true;
                }

                maximumSequenceNumber = firstLog.NewSequenceNumber;

                if (secondLog.Header.PrimarySequenceNumber == maximumSequenceNumber + 1 &&
                    secondLog.Header.PrimarySequenceNumber > Header.SecondarySequenceNumber)
                {
                    Logger.Info($"Replaying log file: {secondLog.LogPath}");
                    bytes = secondLog.UpdateHiveBytes(bytes);
                    maximumSequenceNumber =
                        secondLog.NewSequenceNumber; //TransactionLogEntries.Max(t => t.SequenceNumber);
                }
            }
            else if (logOne != null)
            {
                soloLog = logOne;
            }
            else if (logTwo != null)
            {
                soloLog = logTwo;
            }

            if (soloLog != null)
            {
                Logger.Info($"Single log file available: {soloLog.LogPath}");

                if (Header.ValidateCheckSum() && soloLog.Header.PrimarySequenceNumber >= Header.SecondarySequenceNumber)
                {
                    Logger.Info($"Replaying log file: {soloLog.LogPath}");
                    //we can replay the log
                    bytes = soloLog.UpdateHiveBytes(bytes);
                    maximumSequenceNumber =
                        soloLog.NewSequenceNumber; //TransactionLogEntries.Max(t => t.SequenceNumber);
                    wasUpdated = true;
                }
            }

            if (wasUpdated)
            {
                //update sequence numbers with latest available
                var seqBytes = BitConverter.GetBytes(maximumSequenceNumber);

                Buffer.BlockCopy(seqBytes, 0, bytes, 0x4, 0x4); //Primary #
                Buffer.BlockCopy(seqBytes, 0, bytes, 0x8, 0x4); //Secondary #

                Logger.Info(
                    $"At least one transaction log was applied. Sequence numbers have been updated to 0x{maximumSequenceNumber:X4}");
            }

            if (updateExistingData)
            {
                FileBytes = bytes;
                Initialize(); //reprocess header
            }

            return bytes;
        }

        /// <summary>
        ///     Given a set of Registry transaction logs, apply them in order to an existing hive's data
        /// </summary>
        /// <param name="logFiles"></param>
        /// <param name="updateExistingData"></param>
        /// <remarks>Hat tip: https://github.com/msuhanov</remarks>
        /// <returns>Byte array containing the updated bytes</returns>
        public byte[] ProcessTransactionLogs(List<string> logFiles, bool updateExistingData = false)
        {
            if (logFiles.Count == 0)
            {
                throw new Exception("No logs were supplied");
            }

            if (Header.PrimarySequenceNumber == Header.SecondarySequenceNumber)
            {
                throw new Exception("Sequence numbers match! Hive is not dirty");
            }

            var logfileInfos = new List<TransactionLogFileInfo>();

            foreach (var ofFileName in logFiles)
            {
                //get bytes for file
                var b = File.ReadAllBytes(ofFileName);
              

                if (b.Length == 0)
                {
                    continue;
                }

                var lfi = new TransactionLogFileInfo(ofFileName,b);

                logfileInfos.Add(lfi);
            }

            return ProcessTransactionLogs(logfileInfos, updateExistingData);

        }

        //TODO this needs refactored to remove duplicated code
        private List<RegistryKey> GetSubKeysAndValues(RegistryKey key)
        {
            _relativeOffsetKeyMap.Add(key.NkRecord.RelativeOffset, key);

            _keyPathKeyMap.Add(key.KeyPath.ToLowerInvariant(), key);

            Logger.Trace("Getting subkeys for {0}", key.KeyPath);

            key.KeyFlags = RegistryKey.KeyFlagsEnum.HasActiveParent;

            var keys = new List<RegistryKey>();

            if (key.NkRecord.ClassCellIndex > 0)
            {
                Logger.Trace("Getting Class cell information at relative offset 0x{0:X}", key.NkRecord.ClassCellIndex);
                var d = GetDataNodeFromOffset(key.NkRecord.ClassCellIndex);
                d.IsReferenced = true;
                var clsName = Encoding.Unicode.GetString(d.Data, 0, key.NkRecord.ClassLength);
                key.ClassName = clsName;
                Logger.Trace("Class name found {0}", clsName);
            }

            //Build ValueOffsets for this NKRecord
            if (key.NkRecord.ValueListCellIndex > 0)
            {
                //there are values for this key, so get the offsets so we can pull them next

                Logger.Trace("Getting value list offset at relative offset 0x{0:X}. Value count is {1:N0}",
                    key.NkRecord.ValueListCellIndex, key.NkRecord.ValueListCount);


                var offsetList = GetDataNodeFromOffset(key.NkRecord.ValueListCellIndex);

                offsetList.IsReferenced = true;

                var lastI = 0;
                for (var i = 0; i < key.NkRecord.ValueListCount; i++)
                {
                    //use i * 4 so we get 4, 8, 12, 16, etc
                    var os = BitConverter.ToUInt32(offsetList.Data, i * 4);
                    Logger.Trace("Got value offset 0x{0:X}", os);
                    key.NkRecord.ValueOffsets.Add(os);
                    lastI = i;
                }

                if (RecoverDeleted)
                {
                    //check to see if there are any other values hanging out in this list beyond what is expected
                    lastI += 1; //lastI initially points to where we left off, so add 1
                    var offsetIndex = lastI * 4; //our starting point
                    while (offsetIndex < offsetList.Data.Length)
                    {
                        var os = BitConverter.ToUInt32(offsetList.Data, offsetIndex);

                        if (os < 8 || os % 8 != 0)
                        {
                            break;
                        }

                        Logger.Trace("Got value offset 0x{0:X}", os);

                        if (key.NkRecord.ValueOffsets.Contains(os) == false)
                        {
                            key.NkRecord.ValueOffsets.Add(os);
                        }

                        offsetIndex += 4;
                    }
                }
            }

            var valOffsetIndex = 0;
            // look for values in this key 
            foreach (var valueOffset in key.NkRecord.ValueOffsets)
            {
                Logger.Trace("Looking for vk record at relative offset 0x{0:X}", valueOffset);

                if (CellRecords.ContainsKey((long) valueOffset))
                {
                    var vc = CellRecords[(long) valueOffset];

                    var vk = vc as VkCellRecord;

                    if (vk is null)
                    {
                        continue;
                    }

                    Logger.Trace("Found vk record at relative offset 0x{0:X}. Value name: {1}", valueOffset,
                        vk.ValueName);

                    if (valOffsetIndex >= key.NkRecord.ValueListCount)
                    {
                        if (vk.IsFree == false)
                        {
                            //not a free record, so cant add it
                            continue;
                        }
                    }

                    vk.IsReferenced = true;

                    var value = new KeyValue(vk);

                    key.Values.Add(value);
                }
                else
                {
                    if (valOffsetIndex < key.NkRecord.ValueListCount)
                    {
                        Logger.Warn($"An expected value was not found at offset 0x{valueOffset:X}. Key: {key.KeyPath}");
                    }
                }

                valOffsetIndex += 1;
            }

            if (key.Values.Count != key.NkRecord.ValueListCount)
            {
                Logger.Debug(
                    "{2}: Value count mismatch! ValueListCount is {0:N0} but NKRecord.ValueOffsets.Count is {1:N0}",
                    //ncrunch: no coverage
                    key.NkRecord.ValueListCount, key.NkRecord.ValueOffsets.Count, key.KeyPath);
            }

            Logger.Trace("Looking for sk record at relative offset 0x{0:X}", key.NkRecord.SecurityCellIndex);

//            var sk = CellRecords[key.NKRecord.SecurityCellIndex] as SKCellRecord;
//            sk.IsReferenced = true;

            if (CellRecords.ContainsKey(key.NkRecord.SecurityCellIndex))
            {
                var sk = CellRecords[key.NkRecord.SecurityCellIndex] as SkCellRecord;
                if (sk != null)
                {
                    sk.IsReferenced = true;
                }
            }


            //TODO THIS SHOULD ALSO CHECK THE # OF SUBKEYS == 0
            if (ListRecords.ContainsKey(key.NkRecord.SubkeyListsStableCellIndex) == false)
            {
                return keys;
            }

            Logger.Trace("Looking for list record at relative offset 0x{0:X}",
                key.NkRecord.SubkeyListsStableCellIndex);
            var l = ListRecords[key.NkRecord.SubkeyListsStableCellIndex];

            var sig = BitConverter.ToInt16(l.RawBytes, 4);

            switch (sig)
            {
                case LfSignature:
                case LhSignature:
                    var lxRecord = l as LxListRecord;
                    lxRecord.IsReferenced = true;
                    foreach (var offset in lxRecord.Offsets)
                    {
                        Logger.Trace("In lf or lh, looking for nk record at relative offset 0x{0:X}", offset.Key);

                        if (CellRecords.ContainsKey(offset.Key) == false)
                        {
                            Logger.Warn($"NK record at relative offset 0x{offset.Key} missing! Skipping");
                            continue;
                        }

                        var cell = CellRecords[offset.Key];

                        var nk = cell as NkCellRecord;
                        nk.IsReferenced = true;

                        Logger.Trace("In lf or lh, found nk record at relative offset 0x{0:X}. Name: {1}", offset.Key,
                            nk.Name);

                        var tempKey = new RegistryKey(nk, key);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }

                    break;

                case RiSignature:
                    var riRecord = l as RiListRecord;
                    riRecord.IsReferenced = true;
                    foreach (var offset in riRecord.Offsets)
                    {
                        Logger.Trace("In ri, looking for list record at relative offset 0x{0:X}", offset);
                        var tempList = ListRecords[offset];

                        //templist is now an li or lh list 

                        if (tempList.Signature == "li")
                        {
                            var sk3 = tempList as LiListRecord;

                            foreach (var offset1 in sk3.Offsets)
                            {
                                Logger.Trace("In ri/li, looking for nk record at relative offset 0x{0:X}", offset1);
                                var cell = CellRecords[offset1];

                                var nk = cell as NkCellRecord;
                                nk.IsReferenced = true;

                                var tempKey = new RegistryKey(nk, key);

                                var sks = GetSubKeysAndValues(tempKey);
                                tempKey.SubKeys.AddRange(sks);

                                keys.Add(tempKey);
                            }
                        }
                        else
                        {
                            var lxRecord2 = tempList as LxListRecord;
                            lxRecord2.IsReferenced = true;

                            foreach (var offset3 in lxRecord2.Offsets)
                            {
                                Logger.Trace("In ri/li, looking for nk record at relative offset 0x{0:X}",
                                    offset3.Key);
                                var cell = CellRecords[offset3.Key];

                                var nk = cell as NkCellRecord;
                                nk.IsReferenced = true;

                                var tempKey = new RegistryKey(nk, key);

                                var sks = GetSubKeysAndValues(tempKey);
                                tempKey.SubKeys.AddRange(sks);

                                keys.Add(tempKey);
                            }
                        }
                    }

                    break;

                case LiSignature:
                    var liRecord = l as LiListRecord;
                    liRecord.IsReferenced = true;
                    foreach (var offset in liRecord.Offsets)
                    {
                        Logger.Trace("In li, looking for nk record at relative offset 0x{0:X}", offset);
                        var cell = CellRecords[offset];

                        var nk = cell as NkCellRecord;
                        nk.IsReferenced = true;

                        var tempKey = new RegistryKey(nk, key);

                        var sks = GetSubKeysAndValues(tempKey);
                        tempKey.SubKeys.AddRange(sks);

                        keys.Add(tempKey);
                    }

                    break;
                default:
                    throw new Exception($"Unknown subkey list type {l.Signature}!");
            }

            return keys;
        }

        /// <summary>
        ///     Returns the length, in bytes, of the file being processed
        ///     <remarks>This is the length returned by the underlying stream used to open the file</remarks>
        /// </summary>
        /// <returns></returns>
        protected internal int HiveLength()
        {
            return FileBytes.Length;
        }

        /// <summary>
        ///     Exports contents of Registry to text format.
        /// </summary>
        /// <remarks>Be sure to set FlushRecordListsAfterParse to FALSE if you want deleted records included</remarks>
        /// <param name="outfile">The outfile.</param>
        /// <param name="deletedOnly">if set to <c>true</c> [deleted only].</param>
        public void ExportDataToCommonFormat(string outfile, bool deletedOnly)
        {
            var keyCount = 0; //root key
            var valueCount = 0;
            var keyCountDeleted = 0;
            var valueCountDeleted = 0;

            var header = new StringBuilder();
            header.AppendLine("## Registry common export format");
            header.AppendLine("## Key format");
            header.AppendLine(
                "## key|Is Free (A for in use, U for unused)|Absolute offset in decimal|KeyPath|LastWriteTime in UTC");
            header.AppendLine("## Value format");
            header.AppendLine(
                "## value|Is Free (A for in use, U for unused)|Absolute offset in decimal|KeyPath|Value name|Data type (as decimal integer)|Value data as bytes separated by a singe space");
            header.AppendLine("##");
            header.AppendLine(
                "## Comparison of deleted keys/values is done to compare recovery of vk and nk records, not the algorithm used to associate deleted keys to other keys and their values.");
            header.AppendLine(
                "## When including deleted keys, only the recovered key name should be included, not the full path to the deleted key.");
            header.AppendLine("## When including deleted values, do not include the parent key information.");
            header.AppendLine("##");
            header.AppendLine("## The following totals should also be included");
            header.AppendLine("##");
            header.AppendLine("## total_keys|total in use key count");
            header.AppendLine("## total_values|total in use value count");
            header.AppendLine("## total_deleted_keys|total recovered free key count");
            header.AppendLine("## total_deleted_values|total recovered free value count");
            header.AppendLine("##");
            header.AppendLine(
                "## Before comparison with other common export implementations, the files should be sorted");
            header.AppendLine("##");

            using (var sw = new StreamWriter(outfile, false))
            {
                sw.AutoFlush = true;

                sw.Write(header.ToString());

                if (deletedOnly == false)
                {
                    //dump active stuff
                    if (Root.LastWriteTime != null)
                    {
                        keyCount = 1;
                        sw.WriteLine("key|{0}|{1}|{2}|{3}", Root.NkRecord.IsFree ? "U" : "A",
                            Root.NkRecord.AbsoluteOffset,
                            Root.KeyPath, Root.LastWriteTime.Value.UtcDateTime.ToString("o"));
                    }

                    foreach (var val in Root.Values)
                    {
                        valueCount += 1;
                        sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.VkRecord.IsFree ? "U" : "A",
                            val.VkRecord.AbsoluteOffset, Root.KeyPath, val.ValueName, (int) val.VkRecord.DataType,
                            BitConverter.ToString(val.VkRecord.ValueDataRaw).Replace("-", " "));
                    }

                    DumpKeyCommonFormat(Root, sw, ref keyCount, ref valueCount);
                }

                var theRest = CellRecords.Where(a => a.Value.IsReferenced == false);
                //may not need to if we do not care about orphaned values

                foreach (var keyValuePair in theRest)
                {
                    try
                    {
                        if (keyValuePair.Value.Signature == "vk")
                        {
                            valueCountDeleted += 1;
                            var val = keyValuePair.Value as VkCellRecord;

                            sw.WriteLine(@"value|{0}|{1}|{2}|{3}|{4}|{5}", val.IsFree ? "U" : "A", val.AbsoluteOffset,
                                "",
                                val.ValueName, (int) val.DataType,
                                BitConverter.ToString(val.ValueDataRaw).Replace("-", " "));
                        }

                        if (keyValuePair.Value.Signature == "nk")
                        {
                            //this should never be once we re-enable deleted key rebuilding

                            keyCountDeleted += 1;
                            var nk = keyValuePair.Value as NkCellRecord;
                            var key = new RegistryKey(nk, null);

                            sw.WriteLine("key|{0}|{1}|{2}|{3}", key.NkRecord.IsFree ? "U" : "A",
                                key.NkRecord.AbsoluteOffset, key.KeyName,
                                key.LastWriteTime.Value.UtcDateTime.ToString("o"));

                            DumpKeyCommonFormat(key, sw, ref keyCountDeleted, ref valueCountDeleted);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("There was an error exporting free record at offset 0x{0:X}. Error: {1}",
                            keyValuePair.Value.AbsoluteOffset, ex.Message);
                    }
                }

                sw.WriteLine("total_keys|{0}", keyCount);
                sw.WriteLine("total_values|{0}", valueCount);
                sw.WriteLine("total_deleted_keys|{0}", keyCountDeleted);
                sw.WriteLine("total_deleted_values|{0}", valueCountDeleted);
            }
        }

        public RegistryKey GetDeletedKey(string keyPath, string lastwritetimestamp)
        {
            var segs = keyPath.Split('\\');

            //get a list that contains all matching root level unassociated keys
            var keys = DeletedRegistryKeys.Where(t => t.KeyPath == keyPath).ToList();

            if (keys.Count() == 1)
            {
                return keys.First();
            }

            if (!keys.Any())
            {
                keys = DeletedRegistryKeys.Where(t => t.KeyPath == segs[0]).ToList();
            }


            if (!keys.Any())
            {
                //handle case where someone doesn't pass in ROOT keyname
                var newPath = $"{Root.KeyName}\\{keyPath}";

                keys = DeletedRegistryKeys.Where(t => t.KeyPath == newPath).ToList();
            }


            //drill down into each until we find the right one based on last write time
            foreach (var registryKey in keys)
            {
                var foo = registryKey;

                var startKey = registryKey;

                for (var i = 1; i < segs.Length; i++)
                {
                    foo = startKey.SubKeys.SingleOrDefault(t => t.KeyName == segs[i]);
                    if (foo != null)
                    {
                        startKey = foo;
                    }
                }

                if (foo == null)
                {
                    continue;
                }

                if (foo.LastWriteTime.ToString() != lastwritetimestamp)
                {
                    continue;
                }

                return foo;

                //  break;
            }

            return null;
        }

        public RegistryKey GetKey(string keyPath)
        {
            keyPath = keyPath.ToLowerInvariant();

            //trim slashes to match the value in keyPathKeyMap
            keyPath = keyPath.Trim('\\', '/');

            if (_keyPathKeyMap.ContainsKey(keyPath))
            {
                return _keyPathKeyMap[keyPath];
            }

            //handle case where someone doesn't pass in ROOT keyname
            var newPath = $"{Root.KeyName}\\{keyPath}".ToLowerInvariant();

            if (_keyPathKeyMap.ContainsKey(newPath))
            {
                return _keyPathKeyMap[newPath];
            }

            return null;
        }

        public RegistryKey GetKey(long relativeOffset)
        {
            if (_relativeOffsetKeyMap.ContainsKey(relativeOffset))
            {
                return _relativeOffsetKeyMap[relativeOffset];
            }

            return null;
        }

        public bool ParseHive()
        {
            if (_parsed)
            {
                throw new Exception("ParseHive already called");
            }

            TotalBytesRead = 0;

            TotalBytesRead += 4096;

            SoftParsingErrorsInternal = 0;
            HardParsingErrorsInternal = 0;

            ////Look at first hbin, get its size, then read that many bytes to create hbin record
            long offsetInHive = 4096;

            var hiveLength = Header.Length + 0x1000;
            if (hiveLength < FileBytes.Length)
            {
                Logger.Debug("Header length is smaller than the size of the file.");
                hiveLength = (uint) FileBytes.Length;
            }

            if (Header.PrimarySequenceNumber != Header.SecondarySequenceNumber)
            {
                Logger.Warn(
                    "Sequence numbers do not match! Hive is dirty and the transaction logs should be reviewed for relevant data!");
            }

            //keep reading the file until we reach the end
            while (offsetInHive < hiveLength)
            {
                var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offsetInHive + 8, 4), 0);

                if (hbinSize == 0)
                {
                    Logger.Debug("Found hbin with size 0 at absolute offset 0x{0:X}", offsetInHive);
                    // Go to end if we find a 0 size block (padding?)
                    offsetInHive = HiveLength();
                    continue;
                }

                var hbinSig = BitConverter.ToInt32(ReadBytesFromHive(offsetInHive, 4), 0);

                if (hbinSig != HbinSignature)
                {
                    Logger.Warn(
                        $"hbin header incorrect at absolute offset 0x{offsetInHive:X}!!! Percent done: {(double) offsetInHive / hiveLength:P}");

//                    if (RecoverDeleted) //TODO ? always or only if recoverdeleted
//                    {
//                        //TODO need to try to recover records from the bad chunk
//                    }

                    break;
                }


                Logger.Trace(
                    $"Processing hbin at absolute offset 0x{offsetInHive:X} with size 0x{hbinSize:X} Percent done: {(double) offsetInHive / hiveLength:P}");

                var rawhbin = ReadBytesFromHive(offsetInHive, (int) hbinSize);

                try
                {
                    var h = new HBinRecord(rawhbin, offsetInHive - 0x1000, Header.MinorVersion, RecoverDeleted, this);

                    Logger.Trace("hbin info: {0}", h);

                    Logger.Trace("Getting records from hbin at absolute offset 0x{0:X}", offsetInHive);

                    var records = h.Process();

                    Logger.Trace("Found {0:N0} records from hbin at absolute offset 0x{1:X}", records.Count,
                        offsetInHive);

                    foreach (var record in records)
                        //TODO change this to compare against constants?
                    {
                        switch (record.Signature)
                        {
                            case "nk":
                            case "sk":
                            case "lk":
                            case "vk":
                                Logger.Trace("Adding cell record with signature {0} at absolute offset 0x{1:X}",
                                    record.Signature, record.AbsoluteOffset);
                                CellRecords.Add(record.AbsoluteOffset - 4096, (ICellTemplate) record);
                                break;

                            case "db":
                            case "li":
                            case "ri":
                            case "lh":
                            case "lf":
                                Logger.Trace("Adding list record with signature {0} at absolute offset 0x{1:X}",
                                    record.Signature, record.AbsoluteOffset);
                                ListRecords.Add(record.AbsoluteOffset - 4096, (IListTemplate) record);
                                break;
                        }
                    }

                    HBinRecordCount += 1;
                    HBinRecordTotalSize += hbinSize;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error processing hbin at absolute offset 0x{offsetInHive:X}.");
                }

                offsetInHive += hbinSize;
            }

            Logger.Debug("Initial processing complete. Building tree...");

            //The root node can be found by either looking at Header.RootCellOffset or looking for an nk record with HiveEntryRootKey flag set.
            //here we are looking for the flag
            var rootNode =
                CellRecords.Values.OfType<NkCellRecord>()
                    .SingleOrDefault(
                        f =>
                            (f.Flags & NkCellRecord.FlagEnum.HiveEntryRootKey) ==
                            NkCellRecord.FlagEnum.HiveEntryRootKey);

            if (rootNode == null)
            {
                Logger.Warn(
                    "Unable to find root key based on flag HiveEntryRootKey. Looking for root key via Header.RootCellOffset value...");
                rootNode =
                    CellRecords.Values.OfType<NkCellRecord>()
                        .SingleOrDefault(
                            f => f.RelativeOffset == (long) Header.RootCellOffset);

                if (rootNode == null)
                {
                    throw new KeyNotFoundException("Root nk record not found!");
                }
            }

            //validate what we found above via the flag method

            rootNode.IsReferenced = true;

            Logger.Debug("Found root node! Getting subkeys...");

            Root = new RegistryKey(rootNode, null);
            Logger.Debug("Created root node object. Getting subkeys.");


            var keys = GetSubKeysAndValues(Root);

            Root.SubKeys.AddRange(keys);

            Logger.Debug("Hive processing complete!");

            //All processing is complete, so we do some tests to see if we really saw everything
            if (RecoverDeleted && HiveLength() != TotalBytesRead)
            {
                var remainingHive = ReadBytesFromHive(TotalBytesRead, (int) (HiveLength() - TotalBytesRead));

                //Sometimes the remainder of the file is all zeros, which is useless, so check for that
                if (!Array.TrueForAll(remainingHive, a => a == 0))
                {
                    Logger.Warn(
                        "Extra, non-zero data found beyond hive length! Check for erroneous data starting at 0x{0:x}!",
                        TotalBytesRead);
                }

                //as a second check, compare Header length with what we read (taking the header into account as Header.Length is only for hbin records)

                if (Header.Length != TotalBytesRead - 0x1000)
                {
                    Logger.Warn( //ncrunch: no coverage
                        "Hive length (0x{0:x}) does not equal bytes read (0x{1:x})!! Check the end of the hive for erroneous data",
                        HiveLength(), TotalBytesRead);
                }
            }

            if (RecoverDeleted)
            {
                BuildDeletedRegistryKeys();
            }

            if (FlushRecordListsAfterParse)
            {
                Logger.Debug("Flushing record lists...");
                ListRecords.Clear();

                var toRemove = CellRecords.Where(pair => pair.Value is NkCellRecord || pair.Value is VkCellRecord)
                    .Select(pair => pair.Key)
                    .ToList();

                foreach (var key in toRemove)
                {
                    CellRecords.Remove(key);
                }
            }

            _parsed = true;
            return true;
        }

        public List<string> ExpandKeyPath(string wildCardPath)
        {
            var keyPaths = new List<string>();

            wildCardPath = wildCardPath.Trim('\\', '/');

            if (wildCardPath.ToUpperInvariant().StartsWith(Root.KeyName.ToUpperInvariant()))
            {
                wildCardPath = StripRootKeyNameFromKeyPath(wildCardPath);
            }

            if (GetKey(wildCardPath) != null)
            {
                //a key was passed in and found, so what is there to do but return?
                keyPaths.Add(wildCardPath);
                return keyPaths;
            }

            RegistryKey startKey = null;

            var pathSegments = wildCardPath.Split('\\');
            var startingSegment = pathSegments.Length - 1;

            var remainingPath = string.Empty;
            for (var i = pathSegments.Length - 1; i >= 0; i--)
            {
                var newPath = string.Join("\\", pathSegments.Take(i));

                startKey = GetKey(newPath);
                if (startKey != null)
                {
                    //we have somewhere to start from
                    remainingPath = string.Join("\\", pathSegments.Skip(startingSegment));
                    break;
                }

                startingSegment = startingSegment - 1;
            }

            if (startKey == null)
            {
                startKey = Root;
                remainingPath = string.Join("\\", pathSegments);
            }

            //Debug.WriteLine($"startkey path: {startKey.KeyPath} remainingPath: {remainingPath}");

            //expand *, for each returned, call ExpandKeyPath
            pathSegments = remainingPath.Split('\\');

            var firstSeg = pathSegments[0];
            var remainingSegs = string.Join("\\", pathSegments.Skip(1));

            var paths = ExpandStar(startKey, firstSeg);

            foreach (var path in paths)
            {
                var morePaths = ExpandKeyPath($"{path}\\{remainingSegs}");
                keyPaths.AddRange(morePaths);
            }

            return keyPaths;
        }

        private IEnumerable<string> ExpandStar(RegistryKey key, string starString)
        {
            //Debug.WriteLine($"    EXPANDING STAR: {starString}");

            var keyPaths = new List<string>();

            if (starString.Equals("*"))
            {
                //all subkeys
                foreach (var startKeySubKey in key.SubKeys)
                {
                    var cleanKey = startKeySubKey.KeyPath;
                    if (cleanKey.ToUpperInvariant().StartsWith(Root.KeyName.ToUpperInvariant()))
                    {
                        cleanKey = StripRootKeyNameFromKeyPath(cleanKey);
                    }

                    keyPaths.Add(cleanKey);
                }
            }
            else if (starString.Contains("*") == false)
            {
                //no wildcard at all
                var asdas = key.SubKeys.SingleOrDefault(t =>
                    t.KeyName.ToUpperInvariant() == starString.ToUpperInvariant());

                if (asdas != null)
                {
                    var cleanKey = asdas.KeyPath;
                    if (cleanKey.ToUpperInvariant().StartsWith(Root.KeyName.ToUpperInvariant()))
                    {
                        cleanKey = StripRootKeyNameFromKeyPath(cleanKey);
                    }

                    keyPaths.Add(cleanKey);
                }
            }
            else
            {
                //we have a wildcard, so find out some things about this segment, namely, where the * is
                var starPos1 = starString.IndexOf("*", StringComparison.InvariantCultureIgnoreCase);

                var leftOfStar = starString.Substring(0, starPos1);
                var rightOfStar = starString.Substring(starPos1 + 1);

                //now we have to look for any keys based on what we have here
                IEnumerable<RegistryKey> matches;

                if (leftOfStar.Length > 0 && rightOfStar.Length == 0)
                {
                    matches = key.SubKeys.Where(t =>
                        t.KeyName.ToUpperInvariant().StartsWith(leftOfStar.ToUpperInvariant())).ToList();
                }
                else if (leftOfStar.Length == 0 && rightOfStar.Length > 0)
                {
                    //star is at front
                    matches = key.SubKeys.Where(t =>
                        t.KeyName.ToUpperInvariant().EndsWith(rightOfStar.ToUpperInvariant())).ToList();
                }
                else
                {
                    //star is in middle somewhere (leftOfStar.Length>0 && rightOfStar.Length>0)
                    matches = key.SubKeys.Where(t =>
                        t.KeyName.ToUpperInvariant().StartsWith(leftOfStar.ToUpperInvariant()) &&
                        t.KeyName.ToUpperInvariant().EndsWith(rightOfStar.ToUpperInvariant())).ToList();
                }

                foreach (var startKeySubKey in matches)
                {
                    var cleanKey = startKeySubKey.KeyPath;
                    if (cleanKey.ToUpperInvariant().StartsWith(Root.KeyName.ToUpperInvariant()))
                    {
                        cleanKey = StripRootKeyNameFromKeyPath(cleanKey);
                    }

                    keyPaths.Add(cleanKey);
                }
            }

            return keyPaths;
        }


        /// <summary>
        ///     Associates vk records with NK records and builds a hierarchy of nk records
        ///     <remarks>Results of this method will be available in DeletedRegistryKeys</remarks>
        /// </summary>
        private void BuildDeletedRegistryKeys()
        {
            Logger.Debug("Associating deleted keys and values...");

            var unreferencedNkCells = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is NkCellRecord);

            var associatedVkRecordOffsets = new List<long>();

            var deletedRegistryKeys = new Dictionary<long, RegistryKey>();

            //Phase one is to associate any value records with key records
            foreach (var unreferencedNkCell in unreferencedNkCells)
            {
                try
                {
                    var nk = unreferencedNkCell.Value as NkCellRecord;

                    Logger.Trace("Processing deleted nk record at absolute offset 0x{0:X}", nk.AbsoluteOffset);


                    if (nk.ValueListCount > 10000)
                    {
                        Logger.Trace(
                            $"When getting values for nk record at absolute offset 0x{nk.AbsoluteOffset:X}, implausable value count ({nk.ValueListCount:N0}). Skipping");
                        continue;
                    }

                    nk.IsDeleted = true;

                    var regKey = new RegistryKey(nk, null)
                    {
                        KeyFlags = RegistryKey.KeyFlagsEnum.Deleted
                    };

                    //some sanity checking on things
                    if (regKey.NkRecord.Size < 0x50 + regKey.NkRecord.NameLength)
                    {
                        continue;
                    }

                    //Build ValueOffsets for this NKRecord
                    if (regKey.NkRecord.ValueListCellIndex > 0)
                    {
                        //there are values for this key, so get the offsets so we can pull them next

                        Logger.Trace("Processing deleted nk record values for nk at absolute offset 0x{0:X}",
                            nk.AbsoluteOffset);

                        DataNode offsetList = null;

                        var size = ReadBytesFromHive(regKey.NkRecord.ValueListCellIndex + 4096, 4);

                        var sizeNum = Math.Abs(BitConverter.ToUInt32(size, 0));

                        if (sizeNum > regKey.NkRecord.ValueListCount * 4 + 4)
                        {
                            sizeNum = regKey.NkRecord.ValueListCount * 4 + 4;
                        }

                        try
                        {
                            var rawData = ReadBytesFromHive(regKey.NkRecord.ValueListCellIndex + 4096,
                                (int) sizeNum);

                            var dr = new DataNode(rawData, regKey.NkRecord.ValueListCellIndex);

                            offsetList = dr;
                        }
                        catch (Exception) //ncrunch: no coverage
                        {
//ncrunch: no coverage
                            //sometimes the data node doesn't have enough data to even do this, or its wrong data
                            Logger.Trace( //ncrunch: no coverage
                                "When getting values for nk record at absolute offset 0x{0:X}, not enough/invalid data was found at offset 0x{1:X}to look for value offsets. Value recovery is not possible",
                                nk.AbsoluteOffset, regKey.NkRecord.ValueListCellIndex);
                        } //ncrunch: no coverage

                        var lastI = 0;
                        if (offsetList != null)
                        {
                            Logger.Trace("Found offset list for nk at absolute offset 0x{0:X}. Processing.",
                                nk.AbsoluteOffset);
                            try
                            {
                                for (var i = 0; i < regKey.NkRecord.ValueListCount; i++)
                                {
                                    //use i * 4 so we get 4, 8, 12, 16, etc
                                    var os = BitConverter.ToUInt32(offsetList.Data, i * 4);

                                    regKey.NkRecord.ValueOffsets.Add(os);
                                    lastI = i;
                                }
                            }
                            catch (Exception) //ncrunch: no coverage
                            {
//ncrunch: no coverage
                                Logger.Trace( //ncrunch: no coverage
                                    "When getting value offsets for nk record at absolute offset 0x{0:X}, not enough data was found at offset 0x{1:X} to look for all value offsets. Only partial value recovery possible",
                                    nk.AbsoluteOffset, regKey.NkRecord.ValueListCellIndex);
                            } //ncrunch: no coverage

                            //check to see if there are any other values hanging out in this list beyond what is expected
                            lastI += 1; //lastI initially points to where we left off, so add 1
                            var offsetIndex = lastI * 4; //our starting point
                            while (offsetIndex < offsetList.Data.Length)
                            {
                                var os = BitConverter.ToUInt32(offsetList.Data, offsetIndex);

                                if (os < 8 || os % 8 != 0)
                                {
                                    break;
                                }

                                Logger.Trace("Got value offset 0x{0:X}", os);

                                if (regKey.NkRecord.ValueOffsets.Contains(os) == false)
                                {
                                    regKey.NkRecord.ValueOffsets.Add(os);
                                }

                                offsetIndex += 4;
                            }
                        }
                    }

                    Logger.Trace("Looking for vk records for nk record at absolute offset 0x{0:X}", nk.AbsoluteOffset);


                    var valOffsetIndex = 0;
                    //For each value offset, get the vk record if it exists, create a KeyValue, and assign it to the current RegistryKey
                    foreach (var valueOffset in nk.ValueOffsets)
                    {
                        if (CellRecords.ContainsKey((long) valueOffset))
                        {
                            Logger.Trace(
                                "Found vk record at relative offset 0x{0:X} for nk record at absolute offset 0x{1:X}",
                                valueOffset, nk.AbsoluteOffset);

                            var val = CellRecords[(long) valueOffset] as VkCellRecord;
                            //we have a value for this key

                            if (val != null)
                            {
                                //if its an in use record AND referenced, warn
                                if (val.IsFree == false && val.IsReferenced)
                                {
                                    Logger.Trace(
                                        "When getting values for nk record at absolute offset 0x{0:X}, VK record at relative offset 0x{1:X} isn't free and is referenced by another nk record. Skipping!",
                                        nk.AbsoluteOffset, valueOffset);
                                }
                                else
                                {
                                    associatedVkRecordOffsets.Add(val.RelativeOffset);


                                    var kv = new KeyValue(val);

                                    regKey.Values.Add(kv);
                                    Logger.Trace(
                                        $"Added vk record at relative offset 0x{valueOffset:X} for nk record at absolute offset 0x{nk.AbsoluteOffset:X}");
                                }
                            }
                        }
                        else
                        {
                            Logger.Trace(
                                $"vk record at relative offset 0x{valueOffset:X} not found for nk record at absolute offset 0x{nk.AbsoluteOffset:X}");
                        }
                    }

                    Logger.Trace(
                        $"Associated {regKey.Values.Count:N0} value(s) out of {nk.ValueOffsets.Count:N0} possible values for nk record at absolute offset 0x{nk.AbsoluteOffset:X}");


                    deletedRegistryKeys.Add(nk.RelativeOffset, regKey);
                }
                catch (Exception ex) //ncrunch: no coverage
                {
//ncrunch: no coverage
                    Logger.Trace( //ncrunch: no coverage
                        ex,
                        $"Error while processing deleted nk record at absolute offset 0x{unreferencedNkCell.Value.AbsoluteOffset:X}");
                } //ncrunch: no coverage
            }

            Logger.Debug("Building tree of key/subkeys for deleted keys");

            //DeletedRegistryKeys now contains all deleted nk records and their associated values.
            //Phase 2 is to build a tree of key/subkeys
            var matchFound = true;
            while (matchFound)
            {
                var keysToRemove = new List<long>();
                matchFound = false;

                foreach (var deletedRegistryKey in deletedRegistryKeys)
                {
                    if (deletedRegistryKeys.ContainsKey(deletedRegistryKey.Value.NkRecord.ParentCellIndex))
                    {
                        //deletedRegistryKey is a child of RegistryKey with relative offset ParentCellIndex

                        //add the key as as subkey of its parent
                        var parent = deletedRegistryKeys[deletedRegistryKey.Value.NkRecord.ParentCellIndex];

                        Logger.Trace(
                            "Found subkey at absolute offset 0x{0:X} for parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NkRecord.AbsoluteOffset, parent.NkRecord.AbsoluteOffset);

                        deletedRegistryKey.Value.KeyPath = $@"{parent.KeyPath}\{deletedRegistryKey.Value.KeyName}";

                        parent.SubKeys.Add(deletedRegistryKey.Value);

                        //mark the subkey for deletion so we do not blow up the collection while iterating it
                        keysToRemove.Add(deletedRegistryKey.Value.NkRecord.RelativeOffset);

                        //reset this so the loop continutes
                        matchFound = true;
                    }
                }

                foreach (var l in keysToRemove)
                    //take out the key from main collection since we copied it above to its parent's subkey list
                {
                    deletedRegistryKeys.Remove(l);
                }
            }

            Logger.Debug("Associating top level deleted keys to active Registry keys");

            //Phase 3 is looking at top level keys from Phase 2 and seeing if any of those can be assigned to non-deleted keys in the main tree
            foreach (var deletedRegistryKey in deletedRegistryKeys)
            {
                if (CellRecords.ContainsKey(deletedRegistryKey.Value.NkRecord.ParentCellIndex))
                {
                    //an parent key has been located, so get it
                    var parentNk = CellRecords[deletedRegistryKey.Value.NkRecord.ParentCellIndex] as NkCellRecord;

                    Logger.Trace(
                        "Found possible parent key at absolute offset 0x{0:X} for deleted key at absolute offset 0x{1:X}",
                        deletedRegistryKey.Value.NkRecord.ParentCellIndex + 0x1000,
                        deletedRegistryKey.Value.NkRecord.AbsoluteOffset);

                    if (parentNk == null)
                    {
                        continue;
                    }

                    if (parentNk.IsReferenced && parentNk.IsFree == false)
                    {
                        //parent exists in our primary tree, so get that key
                        var pk = GetKey(deletedRegistryKey.Value.NkRecord.ParentCellIndex);

                        Logger.Trace(
                            "Copying subkey at absolute offset 0x{0:X} for parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NkRecord.AbsoluteOffset, pk.NkRecord.AbsoluteOffset);

                        deletedRegistryKey.Value.KeyPath = $@"{pk.KeyPath}\{deletedRegistryKey.Value.KeyName}";

                        deletedRegistryKey.Value.KeyFlags |= RegistryKey.KeyFlagsEnum.HasActiveParent;

                        UpdateChildPaths(deletedRegistryKey.Value);

                        //add a copy of deletedRegistryKey under its original parent
                        pk.SubKeys.Add(deletedRegistryKey.Value);

                        _relativeOffsetKeyMap.Add(deletedRegistryKey.Value.NkRecord.RelativeOffset,
                            deletedRegistryKey.Value);

                        if (_keyPathKeyMap.ContainsKey(deletedRegistryKey.Value.KeyPath.ToLowerInvariant()) == false)
                        {
                            _keyPathKeyMap.Add(deletedRegistryKey.Value.KeyPath.ToLowerInvariant(),
                                deletedRegistryKey.Value);
                        }

                        Logger.Trace(
                            "Associated deleted key at absolute offset 0x{0:X} to active parent key at absolute offset 0x{1:X}",
                            deletedRegistryKey.Value.NkRecord.AbsoluteOffset, pk.NkRecord.AbsoluteOffset);
                    }
                }
            }

            DeletedRegistryKeys = deletedRegistryKeys.Values.ToList();

            var unreferencedVk = CellRecords.Where(t => t.Value.IsReferenced == false && t.Value is VkCellRecord);

            foreach (var keyValuePair in unreferencedVk)
            {
                if (associatedVkRecordOffsets.Contains(keyValuePair.Key) == false)
                {
                    var vk = keyValuePair.Value as VkCellRecord;
                    var val = new KeyValue(vk);

                    UnassociatedRegistryValues.Add(val);
                }
            }
        }

        private void UpdateChildPaths(RegistryKey key)
        {
            Logger.Trace("Updating child paths or key {0}", key.KeyPath);
            foreach (var sk in key.SubKeys)
            {
                sk.KeyPath = $@"{key.KeyPath}\{sk.KeyName}";

                _relativeOffsetKeyMap.Add(sk.NkRecord.RelativeOffset, sk);

                if (_keyPathKeyMap.ContainsKey(sk.KeyPath.ToLowerInvariant()) == false)
                {
                    _keyPathKeyMap.Add(sk.KeyPath.ToLowerInvariant(), sk);
                }

                UpdateChildPaths(sk);
            }
        }

        /// <summary>
        ///     Given a file, confirm that hbin headers are found every 4096 * (size of hbin) bytes.
        /// </summary>
        /// <returns></returns>
        public HiveMetadata Verify()
        {
            var hiveMetadata = new HiveMetadata();

            hiveMetadata.HasValidHeader = true;

            long offset = 4096;

            while (offset < Header.Length)
            {
                var hbinSig = BitConverter.ToUInt32(ReadBytesFromHive(offset, 4), 0);

                if (hbinSig == HbinSignature)
                {
                    hiveMetadata.NumberofHBins += 1;
                }

                var hbinSize = BitConverter.ToUInt32(ReadBytesFromHive(offset + 8, 4), 0);

                offset += hbinSize;
            }

            return hiveMetadata;
        }

        public IEnumerable<ValueBySizeInfo> FindByValueSize(int minimumSizeInBytes)
        {
            foreach (var registryKey in _keyPathKeyMap)
            foreach (var keyValue in registryKey.Value.Values)
            {
                if (keyValue.ValueDataRaw.Length >= minimumSizeInBytes)
                {
                    yield return new ValueBySizeInfo(registryKey.Value, keyValue);
                }
            }
        }


        public IEnumerable<SearchHit> FindBase64(int minLength)
        {
            foreach (var registryKey in _keyPathKeyMap)
            foreach (var keyValue in registryKey.Value.Values)
            {
                if (keyValue.ValueData.Trim().Length < minLength)
                {
                    continue;
                }

                if (IsBase64String2(keyValue.ValueData))
                {
                    yield return new SearchHit(registryKey.Value, keyValue, keyValue.ValueData, keyValue.ValueData);
                }
            }
        }

        private static bool IsBase64String2(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length % 4 != 0
                                            || value.Contains(' ') || value.Contains('\t') || value.Contains('\r') ||
                                            value.Contains('\n'))
            {
                return false;
            }

            var index = value.Length - 1;
            if (value[index] == '=')
            {
                index--;
            }

            if (value[index] == '=')
            {
                index--;
            }

            for (var i = 0; i <= index; i++)
            {
                if (IsInvalid(value[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // Make it private as there is the name makes no sense for an outside caller
        private static bool IsInvalid(char value)
        {
            var intValue = (int) value;
            if (intValue >= 48 && intValue <= 57)
            {
                return false;
            }

            if (intValue >= 65 && intValue <= 90)
            {
                return false;
            }

            if (intValue >= 97 && intValue <= 122)
            {
                return false;
            }

            return intValue != 43 && intValue != 47;
        }

        public bool IsBase64String(string s)
        {
            s = s.Trim();

            if (s.Length == 0)
            {
                return false;
            }

            return s.Length % 4 == 0 && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.Compiled);
        }


        public IEnumerable<SearchHit> FindInKeyName(string searchTerm, bool useRegEx = false)
        {
            foreach (var registryKey in _keyPathKeyMap)
                //                if (registryKey.Value.KeyFlags.HasFlag(RegistryKey.KeyFlagsEnum.Deleted))
//                {
//                    _logger.Warn("Deleted");
//                }


            {
                if (useRegEx)
                {
                    if (Regex.IsMatch(registryKey.Value.KeyName, searchTerm, RegexOptions.IgnoreCase))
                    {
                        yield return new SearchHit(registryKey.Value, null, searchTerm, searchTerm);
                    }
                }
                else
                {
                    if (registryKey.Value.KeyName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        yield return new SearchHit(registryKey.Value, null, searchTerm, searchTerm);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindByLastWriteTime(DateTimeOffset? start, DateTimeOffset? end)
        {
            foreach (var registryKey in _keyPathKeyMap)
            {
                if (start != null && end != null)
                {
                    if (start <= registryKey.Value.LastWriteTime && registryKey.Value.LastWriteTime <= end)
                    {
                        yield return new SearchHit(registryKey.Value, null, null, null);
                    }
                }
                else if (end != null)
                {
                    if (registryKey.Value.LastWriteTime < end)
                    {
                        yield return new SearchHit(registryKey.Value, null, null, null);
                    }
                }
                else if (start != null)
                {
                    if (registryKey.Value.LastWriteTime > start)
                    {
                        yield return new SearchHit(registryKey.Value, null, null, null);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInValueName(string searchTerm, bool useRegEx = false)
        {
            foreach (var registryKey in _keyPathKeyMap)
            foreach (var keyValue in registryKey.Value.Values)
            {
                if (useRegEx)
                {
                    if (Regex.IsMatch(keyValue.ValueName, searchTerm, RegexOptions.IgnoreCase))
                    {
                        yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                    }
                }
                else
                {
                    if (keyValue.ValueName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                    }
                }
            }
        }


        public IEnumerable<SearchHit> FindInValueData(string searchTerm, bool useRegEx = false, bool literal = false)
        {
            //   var _logger = LogManager.GetLogger("FFFFFFF");

            foreach (var registryKey in _keyPathKeyMap)
                //   _logger.Debug($"Iterating key {registryKey.Value.KeyName} in path {registryKey.Value.KeyPath}. searchTerm: {searchTerm}, regex: {useRegEx}, literal: {literal}");

            foreach (var keyValue in registryKey.Value.Values)
                // _logger.Debug($"Searching value {keyValue.ValueName}");

            {
                if (useRegEx)
                {
                    if (Regex.IsMatch(keyValue.ValueData, searchTerm, RegexOptions.IgnoreCase))
                    {
                        yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                    }
                }
                else
                {
                    //plaintext matching
                    if (keyValue.ValueData.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                    }

                    if (literal)
                    {
                        continue;
                    }

                    //    _logger.Debug($"After literal match");

                    var asAscii = keyValue.ValueData;
                    var asUnicode = keyValue.ValueData;

                    if (keyValue.VkRecord.DataType == VkCellRecord.DataTypeEnum.RegBinary)
                    {
                        //this takes the raw bytes and converts it to a string, which we can then search
                        //the regex will find us the hit with exact capitalization, which we can then convert to a byte string
                        //and match against the raw data
                        asAscii = Encoding.GetEncoding(1252).GetString(keyValue.ValueDataRaw);
                        asUnicode = Encoding.Unicode.GetString(keyValue.ValueDataRaw);

                        var hitString = string.Empty;
                        try
                        {
                            hitString = Regex.Match(asAscii, searchTerm, RegexOptions.IgnoreCase).Value;
                        }
                        catch (ArgumentException)
                        {
                            // Syntax error in the regular expression
                        }

                        //        _logger.Debug($"hitstring 1: {hitString}");

                        if (hitString.Length > 0)
                        {
                            var asciihex = Encoding.GetEncoding(1252).GetBytes(hitString);

                            var asciiHit = BitConverter.ToString(asciihex);
                            yield return new SearchHit(registryKey.Value, keyValue, asciiHit, hitString);
                        }

                        hitString = string.Empty;
                        try
                        {
                            hitString = Regex.Match(asUnicode, searchTerm, RegexOptions.IgnoreCase).Value;
                        }
                        catch (ArgumentException)
                        {
                            // Syntax error in the regular expression
                        }

                        //    _logger.Debug($"hitstring 2: {hitString}");

                        if (hitString.Length <= 0)
                        {
                            continue;
                        }

                        //     _logger.Debug($"before unicodehex");

                        var unicodehex = Encoding.Unicode.GetBytes(hitString);

                        var unicodeHit = BitConverter.ToString(unicodehex);
                        yield return new SearchHit(registryKey.Value, keyValue, unicodeHit, hitString);
                    }
                }
            }
        }

        public IEnumerable<SearchHit> FindInValueDataSlack(string searchTerm, bool useRegEx = false,
            bool literal = false)
        {
            foreach (var registryKey in _keyPathKeyMap)
            foreach (var keyValue in registryKey.Value.Values)
            {
                if (useRegEx)
                {
                    if (Regex.IsMatch(keyValue.ValueSlack, searchTerm, RegexOptions.IgnoreCase))
                    {
                        yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                    }
                }
                else
                {
                    if (literal)
                    {
                        if (keyValue.ValueSlack.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            yield return new SearchHit(registryKey.Value, keyValue, searchTerm, searchTerm);
                        }
                    }
                    else
                    {
                        //this takes the raw bytes and converts it to a string, which we can then search
                        //the regex will find us the hit with exact capitalization, which we can then convert to a byte string
                        //and match against the raw data
                        var asAscii = Encoding.GetEncoding(1252).GetString(keyValue.ValueSlackRaw);
                        var asUnicode = Encoding.Unicode.GetString(keyValue.ValueSlackRaw);

                        var hitString = string.Empty;
                        try
                        {
                            hitString = Regex.Match(asAscii, searchTerm, RegexOptions.IgnoreCase).Value;
                        }
                        catch (ArgumentException)
                        {
                            // Syntax error in the regular expression
                        }

                        if (hitString.Length > 0)
                        {
                            var asciihex = Encoding.GetEncoding(1252).GetBytes(hitString);

                            var asciiHit = BitConverter.ToString(asciihex);
                            yield return new SearchHit(registryKey.Value, keyValue, asciiHit, hitString);
                        }

                        hitString = string.Empty;
                        try
                        {
                            hitString = Regex.Match(asUnicode, searchTerm, RegexOptions.IgnoreCase).Value;
                        }
                        catch (ArgumentException)
                        {
                            // Syntax error in the regular expression
                        }

                        if (hitString.Length <= 0)
                        {
                            continue;
                        }

                        var unicodehex = Encoding.Unicode.GetBytes(hitString);

                        var unicodeHit = BitConverter.ToString(unicodehex);
                        yield return new SearchHit(registryKey.Value, keyValue, unicodeHit, hitString);
                    }
                }
            }
        }
    }
}