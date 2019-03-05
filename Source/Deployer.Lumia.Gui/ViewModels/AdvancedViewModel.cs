using System;
using System.Reactive.Linq;
using ByteSizeLib;
using Deployer.Gui.Common;
using ReactiveUI;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class AdvancedViewModel : ReactiveObject, IBusy
    {
        private readonly ISettingsService settingsService;

        private readonly ObservableAsPropertyHelper<ByteSize> sizeReservedForWindows;

        public AdvancedViewModel(ISettingsService settingsService)
        {
            this.settingsService = settingsService;

            sizeReservedForWindows =
                this.WhenAnyValue(x => x.GbsReservedForWindows, ByteSize.FromGigaBytes)
                    .ToProperty(this, x => x.SizeReservedForWindows);

            IsBusyObservable = Observable.Return(false);
        }

        public ByteSize SizeReservedForWindows => sizeReservedForWindows.Value;

        public double GbsReservedForWindows
        {
            get => settingsService.SizeReservedForWindows;
            set
            {
                settingsService.SizeReservedForWindows = value;
                settingsService.Save();
                this.RaisePropertyChanged(nameof(GbsReservedForWindows));
            }
        }

        public IObservable<bool> IsBusyObservable { get; }

        public bool UseCompactDeployment
        {
            get => settingsService.UseCompactDeployment;
            set
            {
                settingsService.UseCompactDeployment = value;
                settingsService.Save();
                this.RaisePropertyChanged(nameof(UseCompactDeployment));
            }
        }
    }
}