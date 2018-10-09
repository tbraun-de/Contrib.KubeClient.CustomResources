using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Base class for building classes that aggregate and debounce events from one more <see cref="ICustomResourceWatcher"/>s.
    /// </summary>
    public abstract class WatcherAggregatorBase : IDisposable
    {
        private readonly IScheduler _scheduler;
        private IDisposable _subscription;

        protected WatcherAggregatorBase(ILogger<WatcherAggregatorBase> logger,
                                        TimeSpan debounceDuration,
                                        IEnumerable<ICustomResourceWatcher> watchers,
                                        IScheduler scheduler = null)
        {
            Logger = logger;
            DebounceDuration = debounceDuration;
            Watchers = watchers;
            _scheduler = scheduler ?? Scheduler.Default;

            Subscribe();
        }

        protected ILogger Logger { get; }
        protected TimeSpan DebounceDuration { get; }
        protected IEnumerable<ICustomResourceWatcher> Watchers { get; }

        protected void Subscribe()
        {
            _subscription?.Dispose();

            var observables = Watchers.Select(watcher => Observable.FromEventPattern(addHandler => watcher.DataChanged += addHandler,
                                                                                     removeHandler => watcher.DataChanged -= removeHandler));

            _subscription = observables
                           .Merge()
                           .Throttle(DebounceDuration, _scheduler)
                           .Subscribe(OnNext);
        }

        protected virtual void OnNext(EventPattern<object> obj)
        {
            try
            {
                OnChanged();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Execution of {nameof(OnChanged)} failed");
            }
        }

        protected abstract void OnChanged();

        public void Dispose() => _subscription?.Dispose();
    }
}
