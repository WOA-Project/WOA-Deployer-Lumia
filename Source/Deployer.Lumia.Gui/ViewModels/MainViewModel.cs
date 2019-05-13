using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Deployer.UI;
using Grace.DependencyInjection;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private Meta<ISection> selectedItem;
        private readonly ObservableAsPropertyHelper<bool> isBigProgressVisible;
        private const string DonationLink = "https://github.com/WoA-project/WOA-Deployer/blob/master/Docs/Donations.md";
        private const string HelpLink = "https://github.com/WOA-Project/WOA-Deployer-Lumia#need-help";

        public MainViewModel(IList<Meta<ISection>> sections)
        {
            var isBusyObs = sections.Select(x => x.Value.IsBusyObservable).Merge();

            DonateCommand = ReactiveCommand.Create(() => { Process.Start(DonationLink); });
            HelpCommand = ReactiveCommand.Create(() => { Process.Start(HelpLink); });
            isBusyHelper = isBusyObs.ToProperty(this, model => model.IsBusy);
            Sections = sections.OrderBy(meta => (int)meta.Metadata["Order"]).ToList();
            isBigProgressVisible = this.WhenAnyValue(x => x.SelectedItem)
                .CombineLatest(isBusyObs, (section, busy) => section != null && (int)section.Metadata["Order"] == 0 && busy)
                .ToProperty(this, x => x.IsBigProgressVisible);
        }

        public IList<Meta<ISection>> Sections { get; set; }

        public bool IsBusy => isBusyHelper.Value;

        public ReactiveCommand<Unit, Unit> DonateCommand { get; }

        public string Title => AppProperties.AppTitle;

        public ReactiveCommand<Unit, Unit> HelpCommand { get; set; }

        public bool IsBigProgressVisible => isBigProgressVisible.Value;

        public Meta<ISection> SelectedItem
        {
            get => selectedItem;
            set => this.RaiseAndSetIfChanged(ref selectedItem, value);
        }
    }
}