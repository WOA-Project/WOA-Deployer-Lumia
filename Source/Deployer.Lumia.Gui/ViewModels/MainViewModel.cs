using System.Collections.Generic;
using System.Reactive;
using Deployer.Lumia.Gui.Properties;
using Deployer.UI;
using Deployer.UI.ViewModels;
using Grace.DependencyInjection;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel : MainViewModelBase
    {
        public MainViewModel(IList<Meta<ISection>> sections, IList<IBusy> busies, IDialog contextDialog) : base(sections, busies)
        {
            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => contextDialog.ShowMessage(Resources.TermsOfUseTitle, Resources.WarningNotice));
        }

        public ReactiveCommand<Unit, Unit> ShowWarningCommand { get; set; }

        protected override string DonationLink => "https://github.com/WoA-project/WOA-Deployer/blob/master/Docs/Donations.md";
        protected override string HelpLink => "https://github.com/WOA-Project/WOA-Deployer-Lumia#need-help";
        public override string Title => AppProperties.AppTitle;
    }
}