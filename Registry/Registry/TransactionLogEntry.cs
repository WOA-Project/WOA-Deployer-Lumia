using System;
using System.Collections.Generic;
using System.Text;
using Registry.Other;

namespace Registry
{
    public class TransactionLogEntry
    {
        private byte[] _rawBytes;

        public TransactionLogEntry(byte[] rawBytes)
        {
            var sig = Encoding.GetEncoding(1252).GetString(rawBytes, 0, 4);

            if (sig != "HvLE")
            {
                throw new Exception("Data is not a transaction log entry (bad signature)");
            }

            _rawBytes = rawBytes;

            var index = 4;

            Size = BitConverter.ToUInt32(rawBytes, index);
            index += 4;

            var flags = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            SequenceNumber = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            var hiveBinDataSize = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            DirtyPageCount = BitConverter.ToInt32(rawBytes, index);
            index += 4;

            Hash1 = BitConverter.ToUInt64(rawBytes, index);
            index += 8;

            Hash2 = BitConverter.ToUInt64(rawBytes, index);
            index += 8;

            var dpCount = 0;

            var dpBuff = new byte[8];

            DirtyPages = new List<DirtyPageInfo>();

            while (dpCount < DirtyPageCount)
            {
                Buffer.BlockCopy(rawBytes, index, dpBuff, 0, 8);
                index += 8;

                var off = BitConverter.ToInt32(dpBuff, 0);
                var pageSize = BitConverter.ToInt32(dpBuff, 4);

                var dp = new DirtyPageInfo(off, pageSize);

                DirtyPages.Add(dp);

                dpCount += 1;
            }

            //should be sitting at hbin

            var hbinsig = Encoding.GetEncoding(1252).GetString(rawBytes, index, 4);

            if (hbinsig != "hbin")
            {
                throw new Exception($"hbin header not found at offset 0x{index}");
            }

            //from here are hbins in order

            foreach (var dirtyPageInfo in DirtyPages)
            {
                //dirtyPageInfo.Size contains how many bytes we need to overwrite in the main hive's bytes
                //from index, read size bytes, update dirtyPage

                var pageBuff = new byte[dirtyPageInfo.Size];

                Buffer.BlockCopy(rawBytes, index, pageBuff, 0, dirtyPageInfo.Size);

                dirtyPageInfo.UpdatePageBytes(pageBuff);

                index += dirtyPageInfo.Size;
            }
        }

        public List<DirtyPageInfo> DirtyPages { get; }

        public int DirtyPageCount { get; }
        public ulong Hash1 { get; }
        public ulong Hash2 { get; }
        public int SequenceNumber { get; }
        public uint Size { get; }

        public bool HasValidHashes()
        {
            return Hash1 == CalculateHash1() && Hash2 == CalculateHash2();
        }

        private ulong CalculateHash1()
        {
            var b = new byte[_rawBytes.Length - 40];
            Buffer.BlockCopy(_rawBytes,40,b,0,b.Length);

           var aaa =  Marvin.ComputeHash(ref b[0], b.Length, 0x82EF4D887A4E55C5);

            return (ulong) aaa;
        }
        private ulong CalculateHash2()
        {
            var b = new byte[32];
            Buffer.BlockCopy(_rawBytes,0,b,0,32);

            var aaa =  Marvin.ComputeHash(ref b[0], b.Length, 0x82EF4D887A4E55C5);

            return (ulong) aaa;
        }

        public override string ToString()
        {
            var x = 0;
            var sb = new StringBuilder();

            foreach (var dp in DirtyPages)
            {
                sb.AppendLine($"Index: {x} {dp}");
                x += 1;
            }

            return
                $"Size: 0x{Size:X4}, Sequence Number: 0x{SequenceNumber:X4}, Dirty Page Count: {DirtyPageCount:N0}, Hash1: 0x{Hash1:X}, Hash2: 0x{Hash2:X} "; //Page info: {sb}
        }
    }

    public class DirtyPageInfo
    {
        public DirtyPageInfo(int offset, int size)
        {
            Offset = offset;
            Size = size;
        }

        public int Offset { get; }
        public int Size { get; }

        /// <summary>
        ///     Contains the bytes to be overwritten at Offset in the original hive (from the start of hbins)
        /// </summary>
        public byte[] PageBytes { get; private set; }

        /// <summary>
        ///     Updates PageBytes to contain the data to be used when overwriting part of an original hive
        /// </summary>
        /// <param name="bytes"></param>
        public void UpdatePageBytes(byte[] bytes)
        {
            PageBytes = bytes;
        }

        public override string ToString()
        {
            return $"Offset: 0x{Offset:X4}, Size: 0x{Size:X4}";
        }
    }
}