using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployment.Console.Options;
using Serilog;
using Serilog.Events;

namespace Deployment.Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigureLogger();

            var taskTypes = AssemblyUtils.FindTypes(x => x.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask)));

            try
            {
                var consoleDeployer = new ConsoleDeployer(taskTypes);

                await Parser.Default
                    .ParseArguments<WindowsDeploymentCmdOptions, 
                        EnableDualBootCmdOptions, 
                        DisableDualBootCmdOptions,
                        InstallGpuCmdOptions,
                        NonWindowsDeploymentCmdOptions>
                        (args)
                    .MapResult(
                        (WindowsDeploymentCmdOptions opts) => consoleDeployer.ExecuteWindowsScript(opts),
                        (EnableDualBootCmdOptions opts) => consoleDeployer.ToogleDualBoot(true),
                        (DisableDualBootCmdOptions opts) => consoleDeployer.ToogleDualBoot(false),
                        (InstallGpuCmdOptions opts) => consoleDeployer.InstallGpu(),
                        (NonWindowsDeploymentCmdOptions opts) => consoleDeployer.ExecuteNonWindowsScript(opts.Script),
                        HandleErrors);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Operation failed");
                throw;
            }
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