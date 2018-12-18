using System;
using System.Collections;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, TResource> _resources = new ConcurrentDictionary<string, TResource>();

        public CustomResourceWatcher(ILogger<CustomResourceWatcher<TResource>> logger, ICustomResourceClient<TResource> client, CustomResourceNamespace<TResource> @namespace = null)
        {
            _logger = logger;
            _crd = new TResource().Definition;
            _client = client;
            _namespace = @namespace?.Value ?? "";
        }

        public event EventHandler DataChanged;

        public IEnumerator<TResource> GetEnumerator() => _resources.Select(x => x.Value).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting watch for {0}.", _crd);

            await SubscribeAsync();
        }

        private readonly object _subscriptionLock = new object();
        private IDisposable _subscription;

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

        private bool _firstRun = true;

        private void OnList(IEnumerable<TResource> resources)
        {
            bool changed = false;
            var expectedUids = new HashSet<string>();

            foreach (var resource in resources)
            {
                changed |= Upsert(resource);
                expectedUids.Add(resource.Metadata.Uid);
            }

            foreach (string uid in _resources.Keys.Except(expectedUids))
                changed |= Remove(uid);

            if (changed || _firstRun)
                OnDataChanged();

            _firstRun = false;
        }

        private void OnNext(IResourceEventV1<TResource> @event)
        {
            switch (@event.EventType)
            {
                case ResourceEventType.Added:
                case ResourceEventType.Modified:
                    if (Upsert(@event.Resource))
                        OnDataChanged();
                    break;

                case ResourceEventType.Deleted:
                    if (Remove(@event.Resource.Metadata.Uid))
                        OnDataChanged();
                    break;

                default:
                    _logger.LogWarning("Unexpected event for {0}: {1}.", _crd, @event.EventType);
                    break;
            }
        }

        private bool Upsert(TResource resource)
        {
            if (_resources.TryGetValue(resource.Metadata.Uid, out var existing) && existing.Metadata.ResourceVersion == resource.Metadata.ResourceVersion)
            {
                _logger.LogTrace("Unchanged {0}: {1}", _crd, resource.Metadata.Name);
                return false;
            }
            else
            {
                _resources[resource.Metadata.Uid] = resource;
                _logger.LogDebug("Upserted {0}: {1}", _crd, resource.Metadata.Name);
                return true;
            }
        }

        private bool Remove(string uid)
        {
            if (_resources.TryRemove(uid, out var resource))
            {
                _logger.LogDebug("Removed {0}: {1}", _crd, resource.Metadata.Name);
                return true;
            }
            else
            {
                _logger.LogTrace("Already removed {0}: {1}", _crd, uid);
                return false;
            }
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
                    _logger.LogError(ex, "Resubscribing for {0} failed. Retrying in 10 seconds.", _crd);
                    await Task.Delay(TimeSpan.FromSeconds(10), _cancellationTokenSource.Token);
                }
            }
        }
    }
}
