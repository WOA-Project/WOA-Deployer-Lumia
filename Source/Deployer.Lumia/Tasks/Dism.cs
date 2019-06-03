using System;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Deployer.Exceptions;
using Deployer.Services;
using Deployer.Tasks;
using Deployer.Utils;
using Serilog;

namespace Deployer.Lumia.Tasks
{
    [TaskDescription("Calling DISM")]
    public class Dism : DeploymentTask
    {
        private readonly string args;
        private readonly string workingDirectory;
        private static int batch;

        public Dism(string args, string workingDirectory, IDeploymentContext deploymentContext, IFileSystemOperations fileSystemOperations, IOperationContext operationContext) : base(deploymentContext, fileSystemOperations, operationContext)
        {
            this.args = args;
            this.workingDirectory = workingDirectory;
        }

        protected override async Task ExecuteCore()
        {
            var outputSubject = new Subject<string>();
            var subscription = outputSubject.Subscribe(Log.Verbose);
            var processResults = await ProcessMixin.RunProcess(WindowsCommandLineUtils.Dism, args,
                outputObserver: outputSubject, errorObserver: outputSubject, workingDirectory: workingDirectory);

            subscription.Dispose();

            if (processResults.ExitCode != 0)
            {
                throw new DeploymentException(
                    $"There has been a problem during deployment: DISM exited with code {processResults.ExitCode}. Output: {processResults.StandardOutput}");
            }

            var injectedDrivers = Zafiro.Core.StringExtensions.ExtractFileNames(string.Concat(processResults.StandardOutput)).ToList();

            var metadataPath = GetMetadataFilename();

            SaveMetadata(injectedDrivers, Path.Combine(AppPaths.Metadata, "Injected Drivers", metadataPath));
        }

        private string GetMetadataFilename()
        {
            string finalFilename;
            do
            {
                var prefix = $"Batch{++batch}";
                finalFilename = prefix + "_" + Path.GetRandomFileName() + "Info.json";
            } while (FileSystemOperations.FileExists(finalFilename));

            return finalFilename;
        }
    }
}