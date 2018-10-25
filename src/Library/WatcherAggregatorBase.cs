using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Base class for building classes that aggregate and debounce events from one more <see cref="ICustomResourceWatcher"/>s.
    /// </summary>
    [PublicAPI]
    public abstract class WatcherAggregatorBase : IDisposable
    {
        private IDisposable _subscription;
        protected ILogger Logger { get; }

        /// <summary>
        /// Starts listening to <see cref="ICustomResourceWatcher.DataChanged"/> events.
        /// </summary>
        /// <param name="watchers">The watchers to get events from.</param>
        /// <param name="debounceDuration">The minimum amount of time to wait before reacting to events.</param>
        /// <param name="logger">Used to log problems handling events.</param>
        /// <param name="scheduler">RX scheduler; leave <c>null</c> for default.</param>
        protected WatcherAggregatorBase(IEnumerable<ICustomResourceWatcher> watchers,
                                        TimeSpan debounceDuration,
                                        ILogger<WatcherAggregatorBase> logger,
                                        IScheduler scheduler = null)
        {
            Logger = logger;
            _subscription = watchers
                           .Select(watcher => Observable.FromEventPattern(
                                addHandler => watcher.DataChanged += addHandler,
                                removeHandler => watcher.DataChanged -= removeHandler))
                           .Merge()
                           .Throttle(debounceDuration, scheduler ?? Scheduler.Default)
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

        /// <summary>
        /// Called when one or more <see cref="ICustomResourceWatcher.DataChanged"/> events have occured and the debounce duration has elapsed.
        /// </summary>
        protected abstract void OnChanged();

        /// <summary>
        /// Stops listening to events.
        /// </summary>
        public void Dispose() => _subscription.Dispose();
    }
}
