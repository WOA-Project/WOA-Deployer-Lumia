using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Deployer.Lumia.Gui.Properties;
using Deployer.Tasks;
using Deployer.UI;
using Grace.DependencyInjection.Attributes;
using ReactiveUI;
using Serilog;

namespace Deployer.Lumia.Gui.ViewModels
{
    [Metadata("Name", "Dual Boot")]
    [Metadata("Order", 1)]
    public class DualBootViewModel : ReactiveObject, ISection
    {
        private readonly IDeploymentContext context;
        private bool isCapable;
        private bool isEnabled;
        private bool isUpdated;

        public DualBootViewModel(IDeploymentContext context, IContextDialog dialogService, IOperationContext operationContext)
        {
            this.context = context;
            var isChangingDualBoot = new Subject<bool>();

            UpdateStatusWrapper =
                new CommandWrapper<Unit, DualBootStatus>(this,
                    ReactiveCommand.CreateFromTask(GetStatus, isChangingDualBoot),
                    dialogService, operationContext);

            UpdateStatusWrapper.Command.Subscribe(x =>
            {
                IsCapable = x.CanDualBoot;
                IsEnabled = x.IsEnabled;
                IsUpdated = true;
            });

            var canChangeDualBoot = UpdateStatusWrapper.Command.IsExecuting.Select(isExecuting => !isExecuting);

            EnableDualBootWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(EnableDualBoot,
                    this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled,
                            (isCapable, isEnabled) => isCapable && !isEnabled)
                        .Merge(canChangeDualBoot)), dialogService, operationContext);
            EnableDualBootWrapper.Command.Subscribe(async _ =>
            {
                await dialogService.ShowAlert(this, Resources.Done, Resources.DualBootEnabled);
                IsEnabled = !IsEnabled;
            });

            DisableDualBootWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(DisableDualBoot,
                    this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled,
                            (isCapable, isEnabled) => isCapable && isEnabled)
                        .Merge(canChangeDualBoot)), dialogService, operationContext);

            DisableDualBootWrapper.Command.Subscribe(async _ =>
            {
                await dialogService.ShowAlert(this, Resources.Done, Resources.DualBootDisabled);
                IsEnabled = !IsEnabled;
            });


            DisableDualBootWrapper.Command.IsExecuting.Select(x => !x).Subscribe(isChangingDualBoot);
            EnableDualBootWrapper.Command.IsExecuting.Select(x => !x).Subscribe(isChangingDualBoot);

            IsBusyObservable = Observable.Merge(DisableDualBootWrapper.Command.IsExecuting,
                EnableDualBootWrapper.Command.IsExecuting, UpdateStatusWrapper.Command.IsExecuting);
        }

        public CommandWrapper<Unit, Unit> DisableDualBootWrapper { get; set; }

        public CommandWrapper<Unit, Unit> EnableDualBootWrapper { get; set; }

        public CommandWrapper<Unit, DualBootStatus> UpdateStatusWrapper { get; }

        public bool IsCapable
        {
            get => isCapable;
            set => this.RaiseAndSetIfChanged(ref isCapable, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public bool IsUpdated
        {
            get => isUpdated;
            set => this.RaiseAndSetIfChanged(ref isUpdated, value);
        }

        private IPhone Phone => (IPhone) context.Device;

        public IObservable<bool> IsBusyObservable { get; }

        private async Task EnableDualBoot()
        {
            await Phone.ToogleDualBoot(true);
            Log.Information("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            await Phone.ToogleDualBoot(false);
            Log.Information("Dual Boot disabled");
        }

        private async Task<DualBootStatus> GetStatus()
        {
            var status = await Phone.GetDualBootStatus();

            return status;
        }
    }
}