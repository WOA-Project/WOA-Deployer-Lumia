using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using ByteSizeLib;
using CommandLine;
using Deployer;
using Deployer.DevOpsBuildClient;
using Deployer.Execution;
using Deployer.Filesystem.FullFx;
using Deployer.FileSystem;
using Deployer.Lumia;
using Deployer.Services;
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

            var taskTypes = AssemblyUtils.FindTypes(x => x.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDeploymentTask)));

            try
            {
                var deployer = new ConsoleDeployer(taskTypes);

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
                .WriteTo.RollingFile(@"Logs\{Date}.txt")
                .MinimumLevel.Verbose()
                .CreateLogger();
        }


     
    }    
}