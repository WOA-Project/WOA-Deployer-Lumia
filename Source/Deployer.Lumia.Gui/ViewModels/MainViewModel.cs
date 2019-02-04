using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using Deployer.Gui.Core;
using DynamicData;
using ReactiveUI;
using Serilog.Events;

namespace Deployer.Lumia.Gui.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private IDisposable logLoader;

        private ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private string DonationLink = "";

        public MainViewModel(IObservable<LogEvent> events, 
            IObservable<double> progressSubject, IEnumerable<IBusy> busies, UIServices uiServices)
        {
            progressHelper = progressSubject
                .Where(d => !double.IsNaN(d))
                .ObserveOn(SynchronizationContext.Current)
                .ToProperty(this, model => model.Progress);

            isProgressVisibleHelper = progressSubject
                .Select(d => !double.IsNaN(d))
                .ToProperty(this, x => x.IsProgressVisible);

            SetupLogging(events);

            var isBusyObs = busies.Select(x => x.IsBusyObservable).Merge();

            DonateCommand = ReactiveCommand.Create(() => { Process.Start(DonationLink); });

            isBusyHelper = isBusyObs.ToProperty(this, model => model.IsBusy);
        }

        public ReactiveCommand<Unit, Unit> DonateCommand { get; }

        public bool IsBusy => isBusyHelper.Value;

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public double Progress => progressHelper.Value;

        public RenderedLogEvent Status => statusHelper.Value;

        public void Dispose()
        {
            statusHelper?.Dispose();
            logLoader?.Dispose();
            progressHelper?.Dispose();
            isProgressVisibleHelper?.Dispose();
        }

        public string Title => $"WOA Deployer for Lumia v{Assembly.GetEntryAssembly().GetName().Version}"; 


        private void SetupLogging(IObservable<LogEvent> events)
        {
            var conn = events
                .ObserveOn(SynchronizationContext.Current)
                .Where(x => x.Level == LogEventLevel.Information)
                .Publish();

            statusHelper = conn
                .Select(RenderedLogEvent)
                .ToProperty(this, x => x.Status);

            logLoader = conn
                .ToObservableChangeSet()
                .Transform(RenderedLogEvent)
                .Bind(out logEvents)
                .DisposeMany()
                .Subscribe();

            conn.Connect();
        }

        private static RenderedLogEvent RenderedLogEvent(LogEvent x)
        {
            return new RenderedLogEvent
            {
                Message = x.RenderMessage(),
                Level = x.Level
            };
        }        
    }
}