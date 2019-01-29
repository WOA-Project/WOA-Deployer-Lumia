using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Lists;
using Registry.Other;

namespace Registry
{
    public class RegistrySkeleton
    {
        private const int SecurityOffset = 0x30;
        private const int ClassOffset = 0x34;
        private const int SubkeyCountStableOffset = 0x18;
        private const int SubkeyListsStableCellIndex = 0x20;
        private const int ValueListCellIndex = 0x2C;
        private const int ValueCountIndex = 0x28;
        private const int ParentCellIndex = 0x14;
        private const int RootCellIndex = 0x24;
        private const int ValueDataOffset = 0x0C;
        private const int HeaderMinorVersion = 0x18;
        private const int CheckSumOffset = 0x1fc;

        private readonly RegistryHive _hive;

        private readonly List<SkeletonKeyRoot> _keys;

        private readonly Dictionary<long, int> _skMap = new Dictionary<long, int>();

        private int _currentOffsetInHbin = 0x20;

        private byte[] _hbin = new byte[0];

        private int _relativeOffset;

        public RegistrySkeleton(RegistryHive hive)
        {
            if (hive == null)
            {
                throw new NullReferenceException();
            }

            _hive = hive;
            _keys = new List<SkeletonKeyRoot>();
        }

        public ReadOnlyCollection<SkeletonKeyRoot> Keys => _keys.AsReadOnly();

        /// <summary>
        ///     Adds a SkeletonKey to the SkeletonHive
        /// </summary>
        /// <remarks>Returns true if key is already in list or it is added</remarks>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool AddEntry(SkeletonKeyRoot key)
        {
            var hiveKey = _hive.GetKey(key.KeyPath);

            if (hiveKey == null)
            {
                return false;
            }

            if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
            {
                var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                var tempKey = new SkeletonKeyRoot(newKeyPath, key.AddValues, key.Recursive);
                key = tempKey;
            }

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                _keys.Add(key);

                if (key.Recursive)
                {
                    // for each subkey in hivekey, create another skr and add it
                    var subs = GetSubkeyNames(hiveKey);

                    foreach (var sub in subs)
                    {
                        var subsk = new SkeletonKeyRoot(sub, true, false);
                        _keys.Add(subsk);
                    }
                }
            }

            return true;
        }

        private List<string> GetSubkeyNames(RegistryKey key)
        {
            var l = new List<string>();

            foreach (var registryKey in key.SubKeys)
            {
                l.AddRange(GetSubkeyNames(registryKey));

                l.Add(registryKey.KeyPath);
            }

            return l;
        }

        public bool RemoveEntry(SkeletonKeyRoot key)
        {
            if (key.KeyPath.StartsWith(_hive.Root.KeyName) == false)
            {
                var newKeyPath = $"{_hive.Root.KeyName}\\{key.KeyPath}";
                var tempKey = new SkeletonKeyRoot(newKeyPath, key.AddValues, key.Recursive);
                key = tempKey;
            }

            var intKey = _keys.SingleOrDefault(t => t.KeyPath == key.KeyPath);

            if (intKey == null)
            {
                return false;
            }

            _keys.Remove(intKey);

            return true;
        }

        private byte[] GetEmptyHbin(int size)
        {
            var newHbin = new byte[size];

            //signature 'hbin'
            newHbin[0] = 0x68;
            newHbin[1] = 0x62;
            newHbin[2] = 0x69;
            newHbin[3] = 0x6E;

            BitConverter.GetBytes(_relativeOffset).CopyTo(newHbin, 0x4);
            _relativeOffset += size;

            BitConverter.GetBytes(size).CopyTo(newHbin, 0x8); //size

            BitConverter.GetBytes(DateTimeOffset.UtcNow.ToFileTime()).CopyTo(newHbin, 0x14); //last write

            return newHbin;
        }

        public bool Write(string outHive)
        {
            if (_keys.Count == 0)
            {
                throw new InvalidOperationException("At least one SkeletonKey must be added before calling Write");
            }

            if (File.Exists(outHive))
            {
                File.Delete(outHive);
            }

            _hbin = _hbin.Concat(GetEmptyHbin(0x1000)).ToArray();


            var treeKey = BuildKeyTree();

            var parentOffset = ProcessSkeletonTree(treeKey); //always include keys/values for now

            //mark any remaining hbin as free
            var freeSize = _hbin.Length - _currentOffsetInHbin;
            if (freeSize > 0)
            {
                BitConverter.GetBytes(freeSize).CopyTo(_hbin, _currentOffsetInHbin);
            }

            //work is done, get header, update rootcelloffset, adjust its length to match new hbin length, and write it out

            var headerBytes = _hive.ReadBytesFromHive(0, 0x1000);

            BitConverter.GetBytes(_hbin.Length).CopyTo(headerBytes, 0x28);
            BitConverter.GetBytes(5).CopyTo(headerBytes, HeaderMinorVersion);
            BitConverter.GetBytes(parentOffset).CopyTo(headerBytes, RootCellIndex);

            //update checksum
            var index = 0;
            var xsum = 0;
            while (index <= 0x1fb)
            {
                xsum ^= BitConverter.ToInt32(headerBytes, index);
                index += 0x04;
            }

            var newcs = xsum;

            BitConverter.GetBytes(newcs).CopyTo(headerBytes, CheckSumOffset);

            var outBytes = headerBytes.Concat(_hbin).ToArray();

            File.WriteAllBytes(outHive, outBytes);

            return true;
        }

        private void CheckhbinSize(int recordSize)
        {
            if (_currentOffsetInHbin + recordSize > _hbin.Length)
            {
                //we need to add another hbin

                //set remaining space to free record
                var freeSize = _hbin.Length - _currentOffsetInHbin;
                if (freeSize > 0)
                {
                    BitConverter.GetBytes(freeSize).CopyTo(_hbin, _currentOffsetInHbin);
                }

                //go to end of current _hbin
                _currentOffsetInHbin = _hbin.Length;

                //we have to make our hbin at least as big as the data that needs to go in it, so figure that out
                var hbinBaseSize = (int) Math.Ceiling(recordSize / (double) 4096);
                var hbinSize = hbinBaseSize * 0x1000;

                //add more space
                _hbin = _hbin.Concat(GetEmptyHbin(hbinSize)).ToArray();

                //move pointer to next usable space
                _currentOffsetInHbin += 0x20;
            }
        }

        private int ProcessSkRecord(uint skIndex)
        {
            if (!_hive.CellRecords.ContainsKey(skIndex))
            {
                return 0;
            }

            var sk = _hive.CellRecords[skIndex] as SkCellRecord;

            if (_skMap.ContainsKey(sk.RelativeOffset))
            {
                //sk is already in _hbin
                return _skMap[sk.RelativeOffset];
            }

            CheckhbinSize(sk.RawBytes.Length);
            sk.RawBytes.CopyTo(_hbin, _currentOffsetInHbin);

            var skOffset = _currentOffsetInHbin;
            _skMap.Add(sk.RelativeOffset, skOffset);
            _currentOffsetInHbin += sk.RawBytes.Length;
            return skOffset;
        }

        private int ProcessClassCell(uint classcellId)
        {
            //todo make this work
            return 0;

            if (classcellId == 0)
            {
                return 0;
            }

            var dataLenBytes = _hive.ReadBytesFromHive(classcellId + 4096, 4);
            var dataLen = BitConverter.ToUInt32(dataLenBytes, 0);
            var size = (int) dataLen;
            size = Math.Abs(size);

            var dn = new DataNode(_hive.ReadBytesFromHive(classcellId + 4096, size), classcellId);

            //write it out, return offset
            CheckhbinSize(dn.RawBytes.Length);
            dn.RawBytes.CopyTo(_hbin, _currentOffsetInHbin);

            _currentOffsetInHbin += dn.RawBytes.Length;

            return _currentOffsetInHbin;
        }

        private int ProcessValue(KeyValue value)
        {
            const uint dwordSignMask = 0x80000000;

            var vkBytes = value.VkRecord.RawBytes;

            if ((value.VkRecord.DataLength & dwordSignMask) != dwordSignMask)
            {
                //non-resident data, so write out the data and update the vkrecords pointer to said data

                if (value.VkRecord.DataLength > 16344)
                {
                    //big data baby!

                    //big data case
                    //get data and slack
                    //split into 16344 chunks
                    //add 4 bytes of padding at the end of each chunk
                    //this makes each chunk 16348 of data plus 4 bytes at front for size (16352 total)
                    //write out data chunks, keeping a record of where they went
                    //build db list
                    //point vk record ValueDataOffset to this location

                    var dataraw = value.ValueDataRaw.Concat(value.ValueSlackRaw).ToArray();

                    var pos = 0;

                    var chunks = new List<byte[]>();

                    while (pos < dataraw.Length)
                    {
                        if (dataraw.Length - pos < 16344)
                        {
                            //we are out of data
                            chunks.Add(dataraw.Skip(pos).Take(dataraw.Length - pos).ToArray());
                            pos = dataraw.Length;
                        }

                        chunks.Add(dataraw.Skip(pos).Take(16344).ToArray());
                        pos += 16344;
                    }

                    var dbOffsets = new List<int>();

                    foreach (var chunk in chunks)
                    {
                        var rawChunk = chunk.Concat(new byte[4]).ToArray(); //add our extra 4 bytes at the end
                        var toWrite = BitConverter.GetBytes(-1 * (rawChunk.Length + 4)).Concat(rawChunk).ToArray();
                        //add the size

                        CheckhbinSize(toWrite.Length);
                        toWrite.CopyTo(_hbin, _currentOffsetInHbin);

                        dbOffsets.Add(_currentOffsetInHbin);

                        _currentOffsetInHbin += toWrite.Length;
                    }


                    //next is the list itself of offsets to the data chunks

                    var offsetSize = 4 + dbOffsets.Count * 4; //size itself plus a slot for each offset

                    if ((4 + offsetSize) % 8 != 0)
                    {
                        offsetSize += 4;
                    }

                    var offsetList =
                        BitConverter.GetBytes(-1 * offsetSize).Concat(new byte[dbOffsets.Count * 4]).ToArray();

                    var i = 1;
                    foreach (var dbo in dbOffsets)
                    {
                        BitConverter.GetBytes(dbo).CopyTo(offsetList, i * 4);
                        i += 1;
                    }

                    //write offsetList to hbin
                    CheckhbinSize(offsetList.Length);

                    var offsetOffset = _currentOffsetInHbin;

                    offsetList.CopyTo(_hbin, offsetOffset);
                    _currentOffsetInHbin += offsetList.Length;


                    //all the data is written, build a dblist to reference it
                    //db list is just an offset to offsets
                    //size db #entries offset

                    var dbRaw =
                        BitConverter.GetBytes(-16)
                            .Concat(Encoding.ASCII.GetBytes("db"))
                            .Concat(
                                BitConverter.GetBytes((short) dbOffsets.Count)
                                    .Concat(BitConverter.GetBytes(offsetOffset)))
                            .Concat(new byte[4])
                            .ToArray();

                    var dbOffset = _currentOffsetInHbin;
                    CheckhbinSize(dbRaw.Length);
                    dbRaw.CopyTo(_hbin, dbOffset);

                    _currentOffsetInHbin += dbRaw.Length;

                    BitConverter.GetBytes(dbOffset).CopyTo(vkBytes, ValueDataOffset);
                }
                else
                {
                    //TODO pull function out of here to write data and return offset
                    var dataraw = value.ValueDataRaw.Concat(value.ValueSlackRaw).ToArray();

                    var datarawBytes = new byte[4 + dataraw.Length];

                    BitConverter.GetBytes(-1 * datarawBytes.Length).CopyTo(datarawBytes, 0);
                    dataraw.CopyTo(datarawBytes, 4);

                    CheckhbinSize(datarawBytes.Length);
                    datarawBytes.CopyTo(_hbin, _currentOffsetInHbin);

                    BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(vkBytes, ValueDataOffset);

                    _currentOffsetInHbin += datarawBytes.Length;
                }
            }

            CheckhbinSize(vkBytes.Length);

            var vkOffset = _currentOffsetInHbin;

            //update size of value so its found by other tools
            //TODO does this need to be optional?
            BitConverter.GetBytes(-1 * vkBytes.Length).CopyTo(vkBytes, 0);

            vkBytes.CopyTo(_hbin, vkOffset);

            _currentOffsetInHbin += vkBytes.Length;

            return vkOffset
                ;
        }

        private int ProcessKey(RegistryKey key, int parentCellIndex, bool addValues, bool addSubkeys)
        {
            var skOffset = ProcessSkRecord(key.NkRecord.SecurityCellIndex);

            var classOffset = ProcessClassCell(key.NkRecord.ClassCellIndex);

            //do we have enough room left to place our NK?
            CheckhbinSize(key.NkRecord.RawBytes.Length);

            //this is where we will be placing our record
            var nkOffset = _currentOffsetInHbin;

            //move our pointer to the beginning of free space for any subsequent records
            CheckhbinSize(key.NkRecord.RawBytes.Length);
            _currentOffsetInHbin += key.NkRecord.RawBytes.Length;

            var nkBytes = key.NkRecord.RawBytes;

            //processValues

            BitConverter.GetBytes(0)
                .CopyTo(nkBytes, ValueCountIndex); // zero out value count unless its required for this key
            BitConverter.GetBytes(0)
                .CopyTo(nkBytes, ValueListCellIndex); // zero out value list unless its required for this key
            if (addValues)
            {
                var valueOffsets = new List<int>();

                foreach (var keyValue in key.Values)
                {
                    var valOffset = ProcessValue(keyValue);

                    valueOffsets.Add(valOffset);
                }

                var valueListBytes = BuildValueList(valueOffsets);
                CheckhbinSize(valueListBytes.Length);
                valueListBytes.CopyTo(_hbin, _currentOffsetInHbin);

                //update NK record to point to our new list of values and update the value count
                BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(nkBytes, ValueListCellIndex);
                BitConverter.GetBytes(valueOffsets.Count).CopyTo(nkBytes, ValueCountIndex);

                _currentOffsetInHbin += valueListBytes.Length;
            }

            //processSubkeys

            BitConverter.GetBytes(0)
                .CopyTo(nkBytes, SubkeyCountStableOffset); // zero out subkey count unless its required for this key
            BitConverter.GetBytes(0)
                .CopyTo(nkBytes, SubkeyListsStableCellIndex); // zero out subkey list unless its required for this key
            if (addSubkeys)
            {
                var subkeyOffsets = new Dictionary<int, string>();

                foreach (var registryKey in key.SubKeys)
                {
                    var subkeyOffset = ProcessKey(registryKey, nkOffset, addValues, true);

                    var hash = registryKey.KeyName;
                    if (registryKey.KeyName.Length >= 4)
                    {
                        hash = registryKey.KeyName.Substring(0, 4);
                    }

                    //generate list for key offsets
                    subkeyOffsets.Add(subkeyOffset, hash);
                }

                //TODO this should generate an ri list pointing to lh lists when the number of subkeys > 500. each lh should be 500 in size
                //TODO test this with some hive that has a ton of keys

                //write list and save address
                var subkeyListBytes = BuildlfList(subkeyOffsets);

                CheckhbinSize(subkeyListBytes.RawBytes.Length);
                subkeyListBytes.RawBytes.CopyTo(_hbin, _currentOffsetInHbin);

                //update nk record pointers to subkeylist and subkey count
                BitConverter.GetBytes(_currentOffsetInHbin).CopyTo(nkBytes, SubkeyListsStableCellIndex);
                BitConverter.GetBytes(subkeyOffsets.Count).CopyTo(nkBytes, SubkeyCountStableOffset);

                //update size to always be negative so it shows up in other tools
                //TODO maybe this needs to be optional
                BitConverter.GetBytes(-1 * nkBytes.Length).CopyTo(nkBytes, 0);

                _currentOffsetInHbin += subkeyListBytes.RawBytes.Length;
            }

            //update nkBytes

            if ((key.NkRecord.Flags & NkCellRecord.FlagEnum.HiveEntryRootKey) != NkCellRecord.FlagEnum.HiveEntryRootKey)
            {
                //update parent offset since this isnt the root cell
                BitConverter.GetBytes(parentCellIndex).CopyTo(nkBytes, ParentCellIndex);
            }

            BitConverter.GetBytes(skOffset).CopyTo(nkBytes, SecurityOffset);
            BitConverter.GetBytes(classOffset).CopyTo(nkBytes, ClassOffset);


            //commit our nk record to hbin
            CheckhbinSize(nkBytes.Length);
            nkBytes.CopyTo(_hbin, nkOffset);

            return nkOffset;
        }

        private byte[] BuildValueList(IReadOnlyCollection<int> offsets)
        {
            var valListSize = 4 + offsets.Count * 4;
            if (valListSize % 8 != 0)
            {
                valListSize += 4;
            }

            var offsetListBytes = new byte[valListSize];

            BitConverter.GetBytes(-1 * valListSize).CopyTo(offsetListBytes, 0);

            var index = 4;
            foreach (var valueOffset in offsets)
            {
                BitConverter.GetBytes(valueOffset).CopyTo(offsetListBytes, index);
                index += 4;
            }

            return offsetListBytes;
        }

        private int ProcessSkeletonTree(SkeletonKey treeKey)
        {
            //call processSkel for treekey.treepath
            //call for each subkey in subkeys
            //drop addsubkeys param as you will only ever be adding a key as found in tk.subkeys or keypath
            //this is where we need to build a list of subkey offsets so we can adjust SubkeyCountStableOffset and SubkeyListsStableCellIndex
            //once we know these and write it, jump to location of key and update accordingly

            Debug.WriteLine($"Processing {treeKey.KeyPath}. AddValues: {treeKey.AddValues}");

            foreach (var skeletonKey in treeKey.Subkeys)
            {
                ProcessSkeletonTree(skeletonKey);
            }

            var key = _hive.GetKey(treeKey.KeyPath);

            var parentOffset = ProcessKey(key, -1, treeKey.AddValues, true);

            return parentOffset;
        }


        private LxListRecord BuildlfList(Dictionary<int, string> subkeyInfo)
        {
            var totalSize = 4 + 2 + 2 + subkeyInfo.Count * 8; //size + sig + num entries + bytes for list itself

            var listBytes = new byte[totalSize];

            BitConverter.GetBytes(-1 * totalSize).CopyTo(listBytes, 0);
            Encoding.ASCII.GetBytes("lf").CopyTo(listBytes, 4);
            BitConverter.GetBytes((short) subkeyInfo.Count).CopyTo(listBytes, 6);

            var index = 0x8;

            foreach (var entry in subkeyInfo)
            {
                BitConverter.GetBytes(entry.Key).CopyTo(listBytes, index);
                index += 4;
                Encoding.ASCII.GetBytes(entry.Value).CopyTo(listBytes, index);
                index += 4;
            }

            //we can set relative offset to 0 since we are only interested in the bytes
            return new LxListRecord(listBytes, 0);
        }

        private SkeletonKey BuildKeyTree()
        {
            SkeletonKey root = null;

            foreach (var keyRoot in _keys)
            {
                var current = root;

                //need to make sure root key name is at beginning of each

                var segs = keyRoot.KeyPath.Split('\\');

                var withVals = keyRoot.AddValues;
                foreach (var seg in segs)
                {
                    if (seg == segs.Last())
                    {
                        withVals = keyRoot.AddValues;
                    }

                    if (root == null)
                    {
                        root = new SkeletonKey(seg, seg, withVals);
                        current = root;
                        continue;
                    }

                    if (current.KeyName == segs.First() && seg == segs.First())
                    {
                        continue;
                    }

                    if (current.Subkeys.Any(t => t.KeyName == seg))
                    {
                        current = current.Subkeys.Single(t => t.KeyName == seg);
                        continue;
                    }

                    if (seg == segs.Last())
                    {
                        withVals = keyRoot.AddValues;
                    }

                    var sk = new SkeletonKey($"{current.KeyPath}\\{seg}", seg, withVals);
                    current.Subkeys.Add(sk);
                    current = sk;
                }
            }

            return root;
        }
    }

    public class SkeletonKeyRoot
    {
        public SkeletonKeyRoot(string keyPath, bool addValues, bool recursive)
        {
            KeyPath = keyPath;
            AddValues = addValues;
            Recursive = recursive;
        }

        public string KeyPath { get; }
        public bool AddValues { get; }
        public bool Recursive { get; }
    }

    public class SkeletonKey
    {
        public SkeletonKey(string keyPath, string keyName, bool addValues)
        {
            KeyPath = keyPath;
            KeyName = keyName;
            AddValues = addValues;
            Subkeys = new List<SkeletonKey>();
        }

        public string KeyName { get; }
        public string KeyPath { get; }
        public bool AddValues { get; }
        public List<SkeletonKey> Subkeys { get; }
    }
}