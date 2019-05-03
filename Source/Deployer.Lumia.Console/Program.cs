using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using CommandLine;
using Deployer.Console;
using Deployer.Lumia.Console.Options;
using Serilog;
using Serilog.Events;

namespace Deployer.Lumia.Console
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            ConfigureLogger();

            var progress = new OperationProgress();
            using (new ConsoleDisplayUpdater(progress))
            {
                try
                {
                    await Execute(args, progress);
                
                    Log.Information("Execution finished");

                }
                catch (Exception e)
                {
                    Log.Fatal(e, "Operation failed");
                    throw;
                }              
            }
        }
        
        private static async Task Execute(IEnumerable<string> args, IOperationProgress progress)
        {
            var optionsProvider = new WindowsDeploymentOptionsProvider();
            
            var deployer = GetDeployer(optionsProvider, progress);

            var parserResult = Parser.Default
                .ParseArguments<WindowsDeploymentCmdOptions,
                        EnableDualBootCmdOptions,
                        DisableDualBootCmdOptions,
                        NonWindowsDeploymentCmdOptions>(args);

            await parserResult
                .MapResult(
                    (WindowsDeploymentCmdOptions opts) =>
                    {
                        optionsProvider.Options = new WindowsDeploymentOptions()
                        {
                            ImageIndex = opts.Index,
                            ImagePath = opts.WimImage,
                            SizeReservedForWindows = ByteSize.FromGigaBytes(opts.ReservedSizeForWindowsInGb),
                            UseCompact = opts.UseCompact,
                        };
                        return deployer.Deploy();
                    },
                    (EnableDualBootCmdOptions opts) => deployer.ToggleDualBoot(true),
                    (DisableDualBootCmdOptions opts) => deployer.ToggleDualBoot(false),
                    (NonWindowsDeploymentCmdOptions opts) => deployer.Deploy(),
                    HandleErrors);
        }

        private static IWoaDeployer GetDeployer(WindowsDeploymentOptionsProvider op, IOperationProgress progress)
        {
            var container = CompositionRoot.CreateContainer(op, progress);

            var deployer = container.Locate<IWoaDeployer>();
            return deployer;
        }

        private static Task HandleErrors(IEnumerable<Error> errs)
        {
            var errors = string.Join("\n", errs.Select(x => x.Tag));

            System.Console.WriteLine($@"Invalid command line: {errors}");
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