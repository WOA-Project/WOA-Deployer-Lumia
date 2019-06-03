using System.Diagnostics;
using System.Reactive;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class ScriptItemViewModel : ReactiveObject
    {
        public string Name { get; }
        public string Path { get; }

        public ScriptItemViewModel(string name, string path)
        {
            Name = name;
            Path = path;
            OpenCommand = ReactiveCommand.Create(() => Process.Start(Path));
        }

        public ReactiveCommand<Unit, Process> OpenCommand { get; }
    }
}