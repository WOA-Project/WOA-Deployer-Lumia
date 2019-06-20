using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Tasks;
using Deployer.UI;
using Grace.DependencyInjection.Attributes;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    //[Metadata("Name", "Scripts")]
    //[Metadata("Order", 2)]
    public class ScriptsViewModel : ReactiveObject, ISection, IDisposable
    {
        private readonly IScriptDependencyResolver scriptDependencyResolver;
        private readonly IScriptParser parser;
        private readonly IDeploymentContext deploymentContext;
        private readonly UIServices uiServices;
        private readonly IScriptRunner scriptRunner;
        private FolderNode selectedScript;
        private readonly string extraScriptsPath = Path.Combine("Downloaded", "Deployment-Scripts", "Lumia", "Extra");

        public ScriptsViewModel(UIServices uiServices, IScriptRunner scriptRunner,
            IOperationProgress progress, IScriptDependencyResolver scriptDependencyResolver, IScriptParser parser, IDeploymentContext deploymentContext, IOperationContext operationContext)
        {
            this.uiServices = uiServices;
            this.scriptRunner = scriptRunner;
            this.scriptDependencyResolver = scriptDependencyResolver;
            this.parser = parser;
            this.deploymentContext = deploymentContext;

            var canRun = this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null);
            var runCommand = ReactiveCommand.CreateFromTask(Run, canRun);
            RunCommand = new ProgressViewModel(runCommand, progress, this, this.uiServices.ContextDialog, operationContext);
            
            IsBusyObservable = Observable.Merge(RunCommand.Command.IsExecuting);

            Tree = GetTree(new DirectoryInfo(extraScriptsPath)).Children.ToList();

            MessageBus.Current.Listen<FolderNode>().Subscribe(x => SelectedScript = x);
        }

        public List<FolderNode> Tree { get; }

        public ProgressViewModel RunCommand { get; set; }

        public FolderNode SelectedScript
        {
            get => selectedScript;
            set => this.RaiseAndSetIfChanged(ref selectedScript, value);
        }

        public IObservable<bool> IsBusyObservable { get; }

        private FolderNode GetTree(DirectoryInfo root)
        {
            if (!root.Exists)
            {
                return new FolderNode(uiServices.ContextDialog)
                {
                    Children = new List<FolderNode>()
                };
            }

            var subFolders = root.GetDirectories().Where(info => info.GetFiles().Any()).Select(GetTree);
            var subFiles = root.GetFiles().Select(x => new FolderNode(uiServices.ContextDialog)
            {
                Name = Path.GetFileNameWithoutExtension(x.Name),
                Path = x.FullName
            });

            return new FolderNode(uiServices.ContextDialog)
            {
                Name = root.Name,
                Path = root.FullName,
                Children = subFolders.Concat(subFiles)
            };
        }

        private async Task Run()
        {
            var script = parser.Parse(File.ReadAllText(SelectedScript.Path));
            var newContext = await scriptDependencyResolver.GetDeploymentContext(script);
            
            if (newContext != null)
            {
                var backupOptions = deploymentContext.DeploymentOptions;
                deploymentContext.DeploymentOptions = newContext.DeploymentOptions;
                await scriptRunner.Run(script);
                deploymentContext.DeploymentOptions = backupOptions;
                await uiServices.ContextDialog.ShowAlert(this, Deployer.Lumia.Gui.Properties.Resources.Done, "Script executed successfully");
            }
            else
            {
                await uiServices.ContextDialog.ShowAlert(this, "Cancelled","Script cancelled");
            }
        }

        public void Dispose()
        {
            selectedScript?.Dispose();
            RunCommand?.Dispose();
            foreach (var folderNode in Tree)
            {
                folderNode.Dispose();
            }
        }
    }
}