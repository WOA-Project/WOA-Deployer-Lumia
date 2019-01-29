using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Serilog;
using Serilog.Events;

namespace Deployment.Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigureLogger();

            await Parser.Default.ParseArguments<WindowsDeploymentOptions, EnableDualBootOptions, DisableDualBootOptions>(args)
                .MapResult(
                    (WindowsDeploymentOptions opts) => new ConsoleDeployer().DeployWindows(opts),                    
                    (EnableDualBootOptions opts) => ConsoleTooling.ToogleDualBoot(true),                    
                    (DisableDualBootOptions opts) => ConsoleTooling.ToogleDualBoot(false),                    
                    HandleErrors);
        }

        private static Task HandleErrors(IEnumerable<Error> errs)
        {
            System.Console.WriteLine($"Invalid command line: {string.Join("\n", errs.Select(x => x.Tag))}");
            return Task.CompletedTask;
        }

        private static void ConfigureLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Information)
                .WriteTo.RollingFile(@"Logs\{Date}.txt")
                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }    
}