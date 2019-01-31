using CommandLine;

namespace Deployment.Console.Options
{
    [Verb("deploy")]
    public class WindowsDeploymentCmdOptions
    {
        [Option("script", Required = true, HelpText = "Installation script")]
        public string Script { get; set; }

        [Option("wim", Required = true, HelpText = "Windows Image (.wim) to deploy")]
        public string WimImage { get; set; }

        [Option("index", Default = 1, HelpText = "Index of the image to deploy")]
        public int Index { get; set; }

        [Option("windows-size", Default = 18, HelpText = "Size reserved for Windows partitions in GB")]
        public double ReservedSizeForWindowsInGb { get; set; }
    }
}