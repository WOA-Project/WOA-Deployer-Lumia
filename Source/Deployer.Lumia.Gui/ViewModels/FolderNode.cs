using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using Deployer.UI;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class FolderNode : ReactiveObject, IDisposable
    {
        private bool isSelected;

        public FolderNode(IContextDialog dialog)
        {
            OpenCommand = ReactiveCommand.Create(() => Process.Start(Path));
            OpenCommand.ThrownExceptions.Subscribe(async ex => await dialog.ShowAlert(this, "Error", "Cannot open the script"));
        }

        public string Name { get; set; }
        public IEnumerable<FolderNode> Children { get; set; }
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

        public void Dispose()
        {
            OpenCommand?.Dispose();

            if (Children != null)
            {
                foreach (var folderNode in Children)
                {
                    folderNode.Dispose();
                }
            }
        }
    }
}