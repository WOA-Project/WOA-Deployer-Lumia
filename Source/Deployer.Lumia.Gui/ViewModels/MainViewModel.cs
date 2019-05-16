using System.Collections.Generic;
using Deployer.UI;
using Deployer.UI.ViewModels;
using Grace.DependencyInjection;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel : MainViewModelBase
    {
        public MainViewModel(IList<Meta<ISection>> sections, IList<IBusy> busies) : base(sections, busies)
        {
        }

        protected override string DonationLink => "https://github.com/WoA-project/WOA-Deployer/blob/master/Docs/Donations.md";
        protected override string HelpLink => "https://github.com/WOA-Project/WOA-Deployer-Lumia#need-help";
        public override string Title => AppProperties.AppTitle;
    }
}