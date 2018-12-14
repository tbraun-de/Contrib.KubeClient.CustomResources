using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using KubeClient.Models;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Watches Kubernetes Custom Resources of a specific type for changes and keeps an in-memory representation.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceWatcher<TResource> : ICustomResourceWatcher<TResource>
        where TResource : CustomResource, new()
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<CustomResourceWatcher<TResource>> _logger;
        private readonly CustomResourceDefinition _crd;
        private readonly ICustomResourceClient<TResource> _client;
        [NotNull] private readonly string _namespace;

        private readonly object _resourcesLock = new object();
        private IDictionary<string, TResource> _resources;

        private readonly object _subscriptionLock = new object();
        private IDisposable _subscription;

        public event EventHandler DataChanged;

        public CustomResourceWatcher(ILogger<CustomResourceWatcher<TResource>> logger, ICustomResourceClient<TResource> client, CustomResourceNamespace<TResource> @namespace = null)
        {
            _logger = logger;
            _crd = new TResource().Definition;
            _client = client;
            _namespace = @namespace?.Value ?? "";
        }

        public IEnumerator<TResource> GetEnumerator()
        {
            lock (_resourcesLock)
                return (_resources?.Values.ToList() ?? Enumerable.Empty<TResource>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting watch for {0}.", _crd);

            await SubscribeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Stopping watch for {0}.", _crd);

            _cancellationTokenSource.Cancel();
            _subscription?.Dispose();
        }

        private async Task SubscribeAsync()
        {
            var list = await _client.ListAsync(@namespace: _namespace);
            OnList(list);

            lock (_subscriptionLock)
            {
                _subscription?.Dispose();
                _subscription = _client.Watch(_namespace, list.Metadata.ResourceVersion)
                                       .Subscribe(OnNext, OnError, OnCompleted);
            }

            _logger.LogDebug("Subscribed to {0}.", _crd);
        }

        private void OnList(IEnumerable<TResource> resources)
        {
            lock (_resourcesLock)
            {
                _resources = resources.ToDictionary(x => x.Metadata.Uid, x => x);
                _logger.LogDebug("Got full list of {0}: {1} elements", _crd, _resources.Count);
            }
            OnDataChanged();
        }

        private void OnNext(IResourceEventV1<TResource> @event)
        {
            switch (@event.EventType)
            {
                case ResourceEventType.Added:
                case ResourceEventType.Modified:
                    lock (_resourcesLock)
                        _resources[@event.Resource.Metadata.Uid] = @event.Resource;
                    _logger.LogDebug("Upserted {0}: {1}", _crd, @event.Resource.GlobalName);
                    break;

                case ResourceEventType.Deleted:
                    lock (_resourcesLock)
                        _resources.Remove(@event.Resource.Metadata.Uid);
                    _logger.LogDebug("Removed {0}: {1}", _crd, @event.Resource.GlobalName);
                    break;

                default:
                    _logger.LogWarning("Unexpected event for {0}: {1}.", _crd, @event.EventType);
                    return;
            }

            OnDataChanged();
        }

        private void OnDataChanged() => DataChanged?.Invoke(this, new EventArgs());

        private void OnError(Exception exception)
        {
            _logger.LogWarning(exception, "Subscription for {0} closed with error.", _crd);
            Resubscribe();
        }

        private void OnCompleted()
        {
            _logger.LogDebug("Subscription for {0} closed normally.", _crd);
            Resubscribe();
        }

        private async void Resubscribe()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogDebug("Resubscribing for {0}.", _crd);
                try
                {
                    await SubscribeAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Resubscribing for {0} failed. Retrying in 10 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token);
                }
            }
        }
    }
}
