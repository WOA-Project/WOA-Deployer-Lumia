using CommandLine;

namespace Deployment.Console.Options
{
    [Verb("deploy", HelpText = "Executes a Windows deployment script")]
    public class WindowsDeploymentCmdOptions
    {
        [Option("wim", Required = true, HelpText = "Windows Image (.wim) to deploy")]
        public string WimImage { get; set; }

        [Option("index", Default = 1, HelpText = "Index of the image to deploy")]
        public int Index { get; set; }

        [Option("windows-size", Default = 18, HelpText = "Size reserved for Windows partitions in GB")]
        public double ReservedSizeForWindowsInGb { get; set; }
    }
}