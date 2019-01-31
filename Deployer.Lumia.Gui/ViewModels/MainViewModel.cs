using Deployer.Gui.Core;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel
    {
        private readonly UIServices uiService;

        public MainViewModel(UIServices uiService)
        {
            this.uiService = uiService;
        }

        public bool IsBusy { get; }
    }
}