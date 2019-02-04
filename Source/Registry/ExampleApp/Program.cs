//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using CommandLine;
//using NLog;
//using NLog.Config;
//using NLog.Targets;
//using Registry;
//using Registry.Cells;
//
//// namespaces...
//
//namespace ExampleApp
//{
//    // internal classes...
//    internal class Program
//    {
//        // private methods...
//
//        private static LoggingConfiguration GetNlogConfig(int level, string logFilePath)
//        {
//            var config = new LoggingConfiguration();
//
//            var loglevel = LogLevel.Info;
//
//            switch (level)
//            {
//                case 1:
//                    loglevel = LogLevel.Debug;
//                    break;
//
//                case 2:
//                    loglevel = LogLevel.Trace;
//                    break;
//                default:
//                    break;
//            }
//
//            var callsite = "${callsite:className=false}";
//            if (loglevel < LogLevel.Trace)
//            {
//                //if trace use expanded callstack
//                callsite = "${callsite:className=false:fileName=true:includeSourcePath=true:methodName=true}";
//            }
//
//            // Step 2. Create targets and add them to the configuration 
//            var consoleTarget = new ColoredConsoleTarget();
//
//            //var consoleWrapper = new AsyncTargetWrapper();
//            //consoleWrapper.WrappedTarget = consoleTarget;
//            //consoleWrapper.QueueLimit = 5000;
//            //consoleWrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Grow;
//
//            //     config.AddTarget("console", consoleWrapper);
//            config.AddTarget("console", consoleTarget);
//
//
//            if (logFilePath != null)
//            {
//                if (Directory.Exists(logFilePath))
//                {
//                    var fileTarget = new FileTarget();
//
//                    //var fileWrapper = new AsyncTargetWrapper();
//                    //fileWrapper.WrappedTarget = fileTarget;
//                    //fileWrapper.QueueLimit = 5000;
//                    //fileWrapper.OverflowAction = AsyncTargetWrapperOverflowAction.Grow;
//
//                    //config.AddTarget("file", fileWrapper);
//                    config.AddTarget("file", fileTarget);
//
//                    fileTarget.FileName = $"{logFilePath}/{Guid.NewGuid()}_log.txt";
//                    // "${basedir}/file.txt";
//
//                    fileTarget.Layout = @"${longdate} ${logger} " + callsite +
//                                        " ${level:uppercase=true} ${message} ${exception:format=ToString,StackTrace}";
//
//                    //var rule2 = new LoggingRule("*", loglevel, fileWrapper);
//                    var rule2 = new LoggingRule("*", loglevel, fileTarget);
//                    config.LoggingRules.Add(rule2);
//                }
//            }
//
//            consoleTarget.Layout = @"${longdate} ${logger} " + callsite +
//                                   " ${level:uppercase=true} ${message} ${exception:format=ToString,StackTrace}";
//
//            // Step 4. Define rules
//            //   var rule1 = new LoggingRule("*", loglevel, consoleWrapper);
//            var rule1 = new LoggingRule("*", loglevel, consoleTarget);
//            config.LoggingRules.Add(rule1);
//
//
//            return config;
//        }
//
//        private static void Main(string[] args)
//        {
//            var testFiles = new List<string>();
//
//
//            var result = Parser.Default.ParseArguments<Options>(args);
//              
//            //NEED TO REPLACE CommandLine Parser
//           
//            if (!result.Errors.Any())
//            {
//                if (result.Value.HiveName == null && result.Value.DirectoryName == null)
//                {
//                    Console.WriteLine(result.Value.GetUsage());
//                    Environment.Exit(1);
//                }
//
//                if (!string.IsNullOrEmpty(result.Value.HiveName))
//                {
//                    if (!string.IsNullOrEmpty(result.Value.DirectoryName))
//                    {
//                        Console.WriteLine("Must specify either -d or -f, but not both");
//                        Environment.Exit(1);
//                    }
//                }
//
//                if (!string.IsNullOrEmpty(result.Value.DirectoryName))
//                {
//                    if (!string.IsNullOrEmpty(result.Value.HiveName))
//                    {
//                        Console.WriteLine("Must specify either -d or -f, but not both");
//                        Environment.Exit(1);
//                    }
//                }
//
//                if (!string.IsNullOrEmpty(result.Value.HiveName))
//                {
//                    testFiles.Add(result.Value.HiveName);
//                }
//                else
//                {
//                    if (Directory.Exists(result.Value.DirectoryName))
//                    {
//                        foreach (var file in Directory.GetFiles(result.Value.DirectoryName))
//                        {
//                            testFiles.Add(file);
//                        }
//                    }
//                    else
//                    {
//                        Console.WriteLine("Directory '{0}' does not exist!", result.Value.DirectoryName);
//                        Environment.Exit(1);
//                    }
//                }
//            }
//            else
//            {
//                Console.WriteLine(result.Value.GetUsage());
//                Environment.Exit(1);
//            }
//
//            var verboseLevel = result.Value.VerboseLevel;
//            if (verboseLevel < 0)
//            {
//                verboseLevel = 0;
//            }
//
//            if (verboseLevel > 2)
//            {
//                verboseLevel = 2;
//            }
//
//            var config = GetNlogConfig(verboseLevel, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
//            LogManager.Configuration = config;
//
//            var logger = LogManager.GetCurrentClassLogger();
//
//            foreach (var testFile in testFiles)
//            {
//                if (File.Exists(testFile) == false)
//                {
//                    logger.Error("'{0}' does not exist!", testFile);
//                    continue;
//                }
//
//                logger.Info("Processing '{0}'", testFile);
//                Console.Title = $"Processing '{testFile}'";
//
//                var sw = new Stopwatch();
//                try
//                {
//                    var registryHive = new RegistryHive(testFile);
//                    if (registryHive.Header.ValidateCheckSum() == false)
//                    {
//                        logger.Warn("CheckSum mismatch!");
//                    }
//
//                    if (registryHive.Header.PrimarySequenceNumber != registryHive.Header.SecondarySequenceNumber)
//                    {
//                        logger.Warn("Sequence mismatch!");
//                    }
//
//                    sw.Start();
//
//                    registryHive.RecoverDeleted = result.Value.RecoverDeleted;
//
//                    registryHive.FlushRecordListsAfterParse = !result.Value.DontFlushLists;
//
//                    registryHive.ParseHive();
//
//                    logger.Info("Finished processing '{0}'", testFile);
//
//                    Console.Title = $"Finished processing '{testFile}'";
//
//                    sw.Stop();
//
//                    var freeCells = registryHive.CellRecords.Where(t => t.Value.IsFree);
//                    var referencedCells = registryHive.CellRecords.Where(t => t.Value.IsReferenced);
//
//                    var nkFree = freeCells.Count(t => t.Value is NkCellRecord);
//                    var vkFree = freeCells.Count(t => t.Value is VkCellRecord);
//                    var skFree = freeCells.Count(t => t.Value is SkCellRecord);
//                    var lkFree = freeCells.Count(t => t.Value is LkCellRecord);
//
//                    var freeLists = registryHive.ListRecords.Where(t => t.Value.IsFree);
//                    var referencedList = registryHive.ListRecords.Where(t => t.Value.IsReferenced);
//
//                    var goofyCellsShouldBeUsed =
//                        registryHive.CellRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
//
//                    var goofyListsShouldBeUsed =
//                        registryHive.ListRecords.Where(t => t.Value.IsFree == false && t.Value.IsReferenced == false);
//
//                    var sb = new StringBuilder();
//
//                    sb.AppendLine("Results:");
//                    sb.AppendLine();
//
//                    sb.AppendLine(
//                        $"Found {registryHive.HBinRecordCount:N0} hbin records. Total size of seen hbin records: 0x{registryHive.HBinRecordTotalSize:X}, Header hive size: 0x{registryHive.Header.Length:X}");
//
//                    if (registryHive.FlushRecordListsAfterParse == false)
//                    {
//                        sb.AppendLine(
//                            $"Found {registryHive.CellRecords.Count:N0} Cell records (nk: {registryHive.CellRecords.Count(w => w.Value is NkCellRecord):N0}, vk: {registryHive.CellRecords.Count(w => w.Value is VkCellRecord):N0}, sk: {registryHive.CellRecords.Count(w => w.Value is SkCellRecord):N0}, lk: {registryHive.CellRecords.Count(w => w.Value is LkCellRecord):N0})");
//                        sb.AppendLine($"Found {registryHive.ListRecords.Count:N0} List records");
//                        sb.AppendLine();
//                        sb.AppendLine(
//                            string.Format($"Header CheckSums match: {registryHive.Header.ValidateCheckSum()}"));
//                        sb.AppendLine(
//                            string.Format(
//                                $"Header sequence 1: {registryHive.Header.PrimarySequenceNumber}, Header sequence 2: {registryHive.Header.SecondarySequenceNumber}"));
//
//                        sb.AppendLine();
//
//                        sb.AppendLine(
//                            $"There are {referencedCells.Count():N0} cell records marked as being referenced ({referencedCells.Count() / (double) registryHive.CellRecords.Count:P})");
//                        sb.AppendLine(
//                            $"There are {referencedList.Count():N0} list records marked as being referenced ({referencedList.Count() / (double) registryHive.ListRecords.Count:P})");
//
//                        if (result.Value.RecoverDeleted)
//                        {
//                            sb.AppendLine();
//                            sb.AppendLine("Free record info");
//                            sb.AppendLine(
//                                $"{freeCells.Count():N0} free Cell records (nk: {nkFree:N0}, vk: {vkFree:N0}, sk: {skFree:N0}, lk: {lkFree:N0})");
//                            sb.AppendLine($"{freeLists.Count():N0} free List records");
//                        }
//
//                        sb.AppendLine();
//                        sb.AppendLine(
//                            $"Cells: Free + referenced + marked as in use but not referenced == Total? {registryHive.CellRecords.Count == freeCells.Count() + referencedCells.Count() + goofyCellsShouldBeUsed.Count()}");
//                        sb.AppendLine(
//                            $"Lists: Free + referenced + marked as in use but not referenced == Total? {registryHive.ListRecords.Count == freeLists.Count() + referencedList.Count() + goofyListsShouldBeUsed.Count()}");
//                    }
//
//                    sb.AppendLine();
//                    sb.AppendLine(
//                        $"There were {registryHive.HardParsingErrors:N0} hard parsing errors (a record marked 'in use' that didn't parse correctly.)");
//                    sb.AppendLine(
//                        $"There were {registryHive.SoftParsingErrors:N0} soft parsing errors (a record marked 'free' that didn't parse correctly.)");
//
//                    logger.Info(sb.ToString());
//
////                    foreach (var cellTemplate in fName1Test.ListRecords)
////                    {
////                        Console.WriteLine(cellTemplate.ToString());
////                    }
//
//                    if (result.Value.ExportHiveData)
//                    {
//                        Console.WriteLine();
//
//                        var baseDir = Path.Combine(Path.GetDirectoryName(testFile), "out");
//
//                        if (Directory.Exists(baseDir) == false)
//                        {
//                            Directory.CreateDirectory(baseDir);
//                        }
//
//                        var baseFname = Path.GetFileName(testFile);
//
//                        var myName = string.Empty;
//
//                        var deletedOnly = result.Value.ExportDeletedOnly;
//
//                        if (deletedOnly)
//                        {
//                            myName = "_EricZ_recovered.txt";
//                        }
//                        else
//                        {
//                            myName = "_EricZ_all.txt";
//                        }
//
//                        var outfile = Path.Combine(baseDir, $"{baseFname}{myName}");
//
//                        logger.Info("Exporting hive data to '{0}'", outfile);
//
//                        registryHive.ExportDataToCommonFormat(outfile, deletedOnly);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("There was an error: {0}", ex.Message);
//                }
//
//                logger.Info("Processing took {0:N4} seconds\r\n", sw.Elapsed.TotalSeconds);
//
//                Console.WriteLine();
//                Console.WriteLine();
//
//                if (result.Value.PauseAfterEachFile)
//                {
//                    Console.WriteLine("Press any key to continue to next file");
//                    Console.ReadKey();
//
//                    Console.WriteLine();
//                    Console.WriteLine();
//                }
//            }
//        }
//    }
//}

