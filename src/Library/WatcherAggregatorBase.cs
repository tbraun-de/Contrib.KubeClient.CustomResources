using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Base class for building classes that aggregate and debounce events from one more <see cref="ICustomResourceWatcher"/>s.
    /// </summary>
    [PublicAPI]
    public abstract class WatcherAggregatorBase : IHostedService, IDisposable
    {
        private IDisposable _subscription;
        private readonly ILogger<WatcherAggregatorBase> _logger;

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
            _logger = logger;
            _subscription = watchers
                           .Select(watcher => Observable.FromEventPattern(
                                addHandler => watcher.DataChanged += addHandler,
                                removeHandler => watcher.DataChanged -= removeHandler))
                           .Merge()
                           .Throttle(debounceDuration, scheduler ?? Scheduler.Default)
                           .Subscribe(OnNext);
        }

        private void OnNext(EventPattern<object> obj)
        {
            Task.Run(async () =>
            {
                try
                {
                    await OnChangedAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Execution of {nameof(OnChangedAsync)} failed");
                }
            }).Wait();
        }

        /// <summary>
        /// Called when one or more <see cref="ICustomResourceWatcher.DataChanged"/> events have occured and the debounce duration has elapsed.
        /// </summary>
        protected abstract Task OnChangedAsync();

        internal const string OnChangedAsyncName = nameof(OnChangedAsync);

        /// <summary>
        /// Does nothing. Everything is already wired up in the constructor.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Stops listening to events.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops listening to events.
        /// </summary>
        public void Dispose() => _subscription.Dispose();
    }
}
