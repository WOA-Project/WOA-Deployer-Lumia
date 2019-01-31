using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Deployer;
using Deployer.Lumia;
using Deployer.Lumia.NetFx;
using Deployment.Console.Options;
using Grace.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Deployment.Console
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            ConfigureLogger();

            try
            {
                var deployer = DeployerComposition.Configure(new DependencyInjectionContainer()).Locate<IAutoDeployer>();

                await Parser.Default
                    .ParseArguments<WindowsDeploymentCmdOptions, 
                        EnableDualBootCmdOptions, 
                        DisableDualBootCmdOptions,
                        InstallGpuCmdOptions,
                        NonWindowsDeploymentCmdOptions>
                        (args)
                    .MapResult(
                        (WindowsDeploymentCmdOptions opts) => deployer.Deploy(new WindowsDeploymentOptions()
                        {
                            ReservedSizeForWindowsInGb = opts.ReservedSizeForWindowsInGb,
                            WimImage = opts.WimImage,
                            Index = opts.Index,
                        }),
                        (EnableDualBootCmdOptions opts) => deployer.ToogleDualBoot(true),
                        (DisableDualBootCmdOptions opts) => deployer.ToogleDualBoot(false),
                        (InstallGpuCmdOptions opts) => deployer.InstallGpu(),
                        (NonWindowsDeploymentCmdOptions opts) => deployer.ExecuteNonWindowsScript(opts.Script),
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
                .WriteTo.RollingFile(@"Logs\Log-{Date}.txt")
                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }    
}