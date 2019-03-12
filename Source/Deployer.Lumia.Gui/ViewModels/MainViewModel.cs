using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Deployer.Gui;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly IFileSystemOperations fileSystemOperations;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private const string DonationLink = "https://github.com/WoA-project/WOA-Deployer/blob/master/Docs/Donations.md";
        private const string HelpLink = "https://github.com/WOA-Project/WOA-Deployer-Lumia#need-help";

        public MainViewModel(IFileSystemOperations fileSystemOperations, IEnumerable<IBusy> busies)
        {
            this.fileSystemOperations = fileSystemOperations;
            var isBusyObs = busies.Select(x => x.IsBusyObservable).Merge();

            DonateCommand = ReactiveCommand.Create(() => { Process.Start(DonationLink); });
            HelpCommand = ReactiveCommand.Create(() => { Process.Start(HelpLink); });
            isBusyHelper = isBusyObs.ToProperty(this, model => model.IsBusy);
        }

        public bool IsBusy => isBusyHelper.Value;

        public ReactiveCommand<Unit, Unit> DonateCommand { get; }

        public string Title => AppProperties.AppTitle;

        public ReactiveCommand<Unit, Unit> HelpCommand { get; set; }
    }
}