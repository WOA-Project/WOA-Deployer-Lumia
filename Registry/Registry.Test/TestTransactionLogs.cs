using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NFluent;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Registry.Test
{
    public class TestTransactionLogs
    {
        [Test]
        public void HiveTestAmcache()
        {
            var config = new LoggingConfiguration();
            var loglevel = LogLevel.Trace;

            const string layout = @"${message}";

            var consoleTarget = new ColoredConsoleTarget();

            config.AddTarget("console", consoleTarget);

            consoleTarget.Layout = layout;

            var rule1 = new LoggingRule("Console", loglevel, consoleTarget);
            config.LoggingRules.Add(rule1);

      

            LogManager.Configuration = config;


            var hive = @"D:\SynologyDrive\Registry\amcache\aa\Amcache.hve";
            var hive1 = new RegistryHive(hive);

            var log1 = $"{hive}.LOG1";
            var log2 = $"{hive}.LOG2";

            var logs = new List<string>();
            logs.Add(log1);
            logs.Add(log2);

            var newb = hive1.ProcessTransactionLogs(logs);

            

            var newName = hive + "_NONDIRTY";

            File.WriteAllBytes(newName, newb);
        }

        [Test]
        public void HiveTests()
        {
            var dir = @"C:\Temp\hives";

            var files = Directory.GetFiles(dir);

            foreach (var file in files)
            {
                if (file.Contains("LOG") || file.EndsWith("_NONDIRTY"))
                {
                    continue;
                }

                var log1 = $"{file}.LOG1";
                var log2 = $"{file}.LOG2";

                var hive1 = new RegistryHive(file);

                var logs = new List<string>();
                logs.Add(log1);
                logs.Add(log2);

                if (hive1.Header.PrimarySequenceNumber != hive1.Header.SecondarySequenceNumber)
                {
                    Debug.WriteLine("");
                    Debug.WriteLine(
                        $"File: {file} Valid checksum: {hive1.Header.ValidateCheckSum()} Primary: 0x{hive1.Header.PrimarySequenceNumber:X} Secondary: 0x{hive1.Header.SecondarySequenceNumber:X}");
                    var newb = hive1.ProcessTransactionLogs(logs);

                    var newName = file + "_NONDIRTY";

                    File.WriteAllBytes(newName, newb);
                }
            }
        }
    }
}