using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Deployer.Execution;
using Deployer.Tasks;
using Deployer.UI;
using Grace.DependencyInjection.Attributes;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    [Metadata("Name", "Scripts")]
    [Metadata("Order", 2)]
    public class ScriptsViewModel : ReactiveObject, ISection
    {
        private const string Root = "Scripts\\Extra";
        private readonly IScriptDependencyResolver scriptDependencyResolver;
        private readonly IScriptParser parser;
        private readonly IDeploymentContext deploymentContext;
        private readonly UIServices uiServices;
        private readonly IScriptRunner scriptRunner;
        private FolderNode selectedScript;
        private readonly string extraScriptsPath = Path.Combine("Scripts", "Extra");

        public ScriptsViewModel(UIServices uiServices, IScriptRunner scriptRunner,
            IOperationProgress progress, IScriptDependencyResolver scriptDependencyResolver, IScriptParser parser, IDeploymentContext deploymentContext)
        {
            this.uiServices = uiServices;
            this.scriptRunner = scriptRunner;
            this.scriptDependencyResolver = scriptDependencyResolver;
            this.parser = parser;
            this.deploymentContext = deploymentContext;

            var canRun = this.WhenAnyValue(x => x.SelectedScript).Select(s => s != null);
            var runCommand = ReactiveCommand.CreateFromTask(Run, canRun);
            RunCommand = new ProgressViewModel(runCommand, progress);

            IsBusyObservable = Observable.Merge(RunCommand.Command.IsExecuting);

            Tree = GetTree(new DirectoryInfo(extraScriptsPath)).Children.ToList();

            Scripts = Directory.EnumerateFiles(Root).Select(x =>
            {
                var name = Path.GetFileNameWithoutExtension(x);
                return new ScriptItemViewModel(name, x);
            }).ToList();

            OpenCommand = ReactiveCommand.Create(() => Process.Start(SelectedScript.Path));
            MessageBus.Current.Listen<FolderNode>().Subscribe(x => SelectedScript = x);
        }

        public List<FolderNode> Tree { get; }

        public ProgressViewModel RunCommand { get; set; }

        public IEnumerable<ScriptItemViewModel> Scripts { get; set; }

        public FolderNode SelectedScript
        {
            get => selectedScript;
            set => this.RaiseAndSetIfChanged(ref selectedScript, value);
        }

        public ReactiveCommand<Unit, Process> OpenCommand { get; }

        public IObservable<bool> IsBusyObservable { get; }

        private static FolderNode GetTree(DirectoryInfo root)
        {
            var subFolders = root.GetDirectories().Where(info => info.GetFiles().Any()).Select(GetTree);
            var subFiles = root.GetFiles().Select(x => new FolderNode
            {
                Name = Path.GetFileNameWithoutExtension(x.Name),
                Path = x.FullName
            });

            return new FolderNode
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
    }
}