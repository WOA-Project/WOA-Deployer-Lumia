using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using Deployer.Execution;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class FolderNode : ReactiveObject
    {
        private bool isSelected;

        public FolderNode()
        {
            OpenCommand = ReactiveCommand.Create(() => Process.Start(Path));
        }

        public string Name { get; set; }
        public IEnumerable<FolderNode> Children { get; set; } = new List<FolderNode>();
        public string Path { get; set; }

        public ReactiveCommand<Unit, Process> OpenCommand { get; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                if (value)
                {
                    MessageBus.Current.SendMessage(this);
                }
            }
        }
    }
}