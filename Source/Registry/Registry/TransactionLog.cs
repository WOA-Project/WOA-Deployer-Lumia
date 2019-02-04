using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NLog;
using Registry.Other;

namespace Registry
{
    public class TransactionLog
    {
        private const int RegfSignature = 0x66676572;
        internal readonly Logger Logger;

        private bool _parsed;

        public TransactionLog(byte[] rawBytes, string logFile)
        {
            FileBytes = rawBytes;
            LogPath = "None";

            Logger = LogManager.GetLogger("rawBytes");

            if (!HasValidSignature())
            {
                Logger.Error("Data in byte array is not a Registry transaction log (bad signature)");

                throw new ArgumentException("Data in byte array is not a Registry transaction log (bad signature)");
            }

            LogPath = logFile;

            TransactionLogEntries = new List<TransactionLogEntry>();

            Initialize();
        }

        public TransactionLog(string logFile)
        {
            if (logFile == null)
            {
                throw new ArgumentNullException(nameof(logFile));
            }

            if (!File.Exists(logFile))
            {
                throw new FileNotFoundException();
            }

            var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var binaryReader = new BinaryReader(fileStream);

            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

            FileBytes = binaryReader.ReadBytes((int) binaryReader.BaseStream.Length);

            binaryReader.Close();
            fileStream.Close();

            if (FileBytes.Length == 0)
            {
                throw new Exception("0 byte log file. Nothing to do");
            }

            Logger = LogManager.GetLogger(logFile);

            if (!HasValidSignature())
            {
                Logger.Error($"'{logFile}' is not a Registry transaction log (bad signature)");

                throw new Exception($"'{logFile}' is not a Registry transaction log (bad signature)");
            }

            LogPath = logFile;

            TransactionLogEntries = new List<TransactionLogEntry>();

            Initialize();
        }

        public byte[] FileBytes { get; }

        public string LogPath { get; }

        public RegistryHeader Header { get; set; }
        public HiveTypeEnum HiveType { get; private set; }
        public List<TransactionLogEntry> TransactionLogEntries { get; }

        public int NewSequenceNumber { get; private set; }

        private byte[] ReadBytesFromHive(long offset, int length)
        {
            var readLength = Math.Abs(length);

            var remaining = FileBytes.Length - offset;

            if (remaining <= 0)
            {
                return new byte[0];
            }

            if (readLength > remaining)
            {
                readLength = (int) remaining;
            }

            var r = new ArraySegment<byte>(FileBytes, (int) offset, readLength);

            return r.ToArray();
        }

        private void Initialize()
        {
            var header = ReadBytesFromHive(0, 4096);

            Logger.Trace("Getting header");

            Header = new RegistryHeader(header);

            Logger.Trace("Got header. Embedded file name {0}", Header.FileName);

            var fNameBase = Path.GetFileName(Header.FileName).ToLowerInvariant();

            switch (fNameBase)
            {
                case "ntuser.dat":
                    HiveType = HiveTypeEnum.NtUser;
                    break;
                case "sam":
                    HiveType = HiveTypeEnum.Sam;
                    break;
                case "security":
                    HiveType = HiveTypeEnum.Security;
                    break;
                case "software":
                    HiveType = HiveTypeEnum.Software;
                    break;
                case "system":
                    HiveType = HiveTypeEnum.System;
                    break;
                case "drivers":
                    HiveType = HiveTypeEnum.Drivers;
                    break;
                case "usrclass.dat":
                    HiveType = HiveTypeEnum.UsrClass;
                    break;
                case "components":
                    HiveType = HiveTypeEnum.Components;
                    break;
                case "bcd":
                    HiveType = HiveTypeEnum.Bcd;
                    break;
                case "amcache.hve":
                    HiveType = HiveTypeEnum.Amcache;
                    break;
                case "syscache.hve":
                    HiveType = HiveTypeEnum.Syscache;
                    break;
                default:
                    HiveType = HiveTypeEnum.Other;
                    break;
            }

            Logger.Trace($"Hive is a {HiveType} hive");

            var version = $"{Header.MajorVersion}.{Header.MinorVersion}";

            Logger.Trace($"Hive version is {version}");
        }

        public bool ParseLog()
        {
            if (_parsed)
            {
                throw new Exception("ParseLog already called");
            }

            var index = 0x200; //data starts at offset 512 decimal

            while (index < FileBytes.Length)
            {
                var sig = Encoding.GetEncoding(1252).GetString(FileBytes, index, 4);

                if (sig != "HvLE")
                {
                    //things arent always HvLE as logs get reused, so check to see if we have another valid header at our current offset
                    break;
                }

                var size = BitConverter.ToInt32(FileBytes, index + 4);
                var buff = new byte[size];

                Buffer.BlockCopy(FileBytes, index, buff, 0, size);

                var tle = new TransactionLogEntry(buff);
                TransactionLogEntries.Add(tle);

                index += size;
            }

            _parsed = true;

            return true;
        }

        /// <summary>
        ///     For the given transaction log, update original hive bytes with the bytes contained in the dirty pages
        /// </summary>
        /// <param name="hiveBytes"></param>
        /// <remarks>This method does nothing to determine IF the data should be overwritten</remarks>
        /// <returns>Byte array containing the updated hive</returns>
        public byte[] UpdateHiveBytes(byte[] hiveBytes)
        {
            const int baseOffset = 0x1000; //hbins start at 4096 bytes

            foreach (var transactionLogEntry in TransactionLogEntries)
            {
                if (transactionLogEntry.HasValidHashes() == false)
                {
                    Logger.Debug($"Skipping transaction log entry with sequence # 0x{transactionLogEntry.SequenceNumber:X}. Hash verification failed");
                    continue;
                }
                Logger.Trace($"Processing log entry: {transactionLogEntry}");

                NewSequenceNumber = transactionLogEntry.SequenceNumber;

                foreach (var dirtyPage in transactionLogEntry.DirtyPages)
                {
                    Logger.Trace($"Processing dirty page: {dirtyPage}");

                    Buffer.BlockCopy(dirtyPage.PageBytes, 0, hiveBytes, dirtyPage.Offset + baseOffset, dirtyPage.Size);
                }
            }

            return hiveBytes;
        }

        public bool HasValidSignature()
        {
            var sig = BitConverter.ToInt32(FileBytes, 0);

            return sig.Equals(RegfSignature);
        }

        public override string ToString()
        {
            var x = 0;
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var transactionLogEntry in TransactionLogEntries)
            {
                sb.AppendLine($"LogEntry #{x} {transactionLogEntry}");
                x += 1;
            }

            return
                $"Log path: {LogPath} Valid checksum: {Header.ValidateCheckSum()} primary: 0x{Header.PrimarySequenceNumber:X} secondary: 0x{Header.SecondarySequenceNumber:X} Entries count: {TransactionLogEntries.Count:N0} Entry info: {sb}";
        }
    }
}