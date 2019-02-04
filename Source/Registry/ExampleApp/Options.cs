using System.Text;
using CommandLine;

internal class Options
{
    [Option('f', "file", Required = false,
        HelpText = "Name of registry hive to process")]
    public string HiveName { get; set; }

    [Option('d', "directory", Required = false,
        HelpText = "Name of directory to lok for registry hives to process")]
    public string DirectoryName { get; set; }

    [Option('e', Default = false, Required = false,
        HelpText = "If true, export a file that can be compared to other Registry parsers")]
    public bool ExportHiveData { get; set; }

    [Option('p', Default = false, Required = false,
        HelpText = "If true, pause after processing a hive and wait for keypress to continue")]
    public bool PauseAfterEachFile { get; set; }

    [Option('a', Default = false, Required = false,
        HelpText = "If true, only recovered deleted keys/values will be exported")]
    public bool ExportDeletedOnly { get; set; }

    [Option('r', Default = false, Required = false,
        HelpText = "If true, recover and process deleted Registry keys/values")]
    public bool RecoverDeleted { get; set; }

    [Option('v', Default = 0, Required = false,
        HelpText = "Verbosity level. 0 = Info, 1 = Debug, 2 = Trace")]
    public int VerboseLevel { get; set; }

    [Option('y', Default = false, Required = false,
        HelpText = "If false, lists containing cell record and lists will be flushed at the end of parsing")]
    public bool DontFlushLists { get; set; }

    public string GetUsage()
    {
        var usage = new StringBuilder();
        usage.AppendLine("Registry example app help");
        usage.AppendLine("-d <directory>: Process files found in <directory>");
        usage.AppendLine("-f <file>: Process <file>");
        usage.AppendLine("-p: Pause after processing each file");
        usage.AppendLine("-r: Recover and process deleted Registry keys/values");
        usage.AppendLine("-v: Verbosity level. 0 = Info, 1 = Debug, 2 = Trace");
        usage.AppendLine("-y: Flush lists containing cell and list records");
        usage.AppendLine(
            "-e: If present, export a file that can be compared to other Registry parsers to same directory as hive is found in");
        usage.AppendLine("-a: Only export deleted key/values");

        usage.AppendLine("");
        usage.AppendLine("-d or -f must be specified, but not both");
        return usage.ToString();
    }
}