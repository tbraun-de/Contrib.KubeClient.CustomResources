using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using HTTPlease;
using KubeClient.Models;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    public abstract class CustomResourceWatcher<TSpec> : ICustomResourceWatcher<TSpec>, IDisposable
    {
        private const long resourceVersionNone = 0;
        private readonly Dictionary<string, CustomResource<TSpec>> _resources = new Dictionary<string, CustomResource<TSpec>>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger _logger;
        private readonly ICustomResourceClient _client;
        private readonly string _apiGroup;
        private readonly string _crdPluralName;
        private readonly string _namespace;
        private IDisposable _subscription;
        private long _lastSeenResourceVersion = resourceVersionNone;
        private string _specName;

        protected CustomResourceWatcher(ILogger logger, ICustomResourceClient client, string apiGroup, string crdPluralName, string @namespace)
        {
            _logger = logger;
            _client = client;
            _apiGroup = apiGroup;
            _crdPluralName = crdPluralName;
            _namespace = @namespace;
            _specName = typeof(TSpec).Name;
        }

        public IEnumerable<TSpec> Resources => new ResourceMemento(_resources);
        public IEnumerable<CustomResource<TSpec>> RawResources => new RawResourceMemento(_resources);
        public event EventHandler<Exception> OnConnectionError;
        public event EventHandler OnConnected;

        public void StartWatching()
        {
            if (_subscription == null)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            DisposeSubscriptions();
            _subscription = _client.Watch<TSpec>(_apiGroup, _crdPluralName, _namespace, _lastSeenResourceVersion.ToString()).Subscribe(OnNext, OnError, OnCompleted);
            OnConnected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation($"Subscribed to {_crdPluralName}.");
        }

        private void OnNext(IResourceEventV1<CustomResource<TSpec>> @event)
        {
            if (!TryValidateResource(@event.Resource, out long resourceVersion))
            {
                _logger.LogTrace("Got outdated resource version '{0}' for '{1}' with name '{2}'", @event.Resource.Metadata.ResourceVersion, _specName, @event.Resource.GlobalName);
                return;
            }

            _lastSeenResourceVersion = resourceVersion;
            switch (@event.EventType)
            {
                case ResourceEventType.Added:
                case ResourceEventType.Modified:
                    UpsertResource(@event);
                    break;
                case ResourceEventType.Deleted:
                    DeleteResource(@event);
                    break;
                case ResourceEventType.Error:
                    _logger.LogWarning($"Got erroneous resource '{typeof(TSpec).Name}' with name {@event.Resource.GlobalName}: {@event.Resource.Status.Message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DeleteResource(IResourceEventV1<CustomResource<TSpec>> @event)
        {
            if (_resources.ContainsKey(@event.Resource.GlobalName))
            {
                _resources.Remove(@event.Resource.GlobalName);
                _logger.LogDebug("Removed resource '{0}' with name '{1}'", _specName, @event.Resource.GlobalName);
            }
        }

        private void UpsertResource(IResourceEventV1<CustomResource<TSpec>> @event)
        {
            if (_resources.ContainsKey(@event.Resource.GlobalName)
             && !_resources[@event.Resource.GlobalName].Metadata.ResourceVersion.Equals(@event.Resource.Metadata.ResourceVersion))
            {
                _resources[@event.Resource.GlobalName] = @event.Resource;
                _logger.LogDebug("Modified resource '{0}' with name '{1}'", _specName, @event.Resource.GlobalName);
            }
            else if (!_resources.ContainsKey(@event.Resource.GlobalName))
            {
                _resources.Add(@event.Resource.GlobalName, @event.Resource);
                _logger.LogDebug("Added resource '{0}' with name '{1}'", _specName, @event.Resource.GlobalName);
            }
            else
            {
                _logger.LogDebug("Got resource '{0}' with name '{1}' without changes", _specName, @event.Resource.GlobalName);
            }
        }

        private void OnError(Exception exception)
        {
            _logger.LogError(exception, $"Error occured during watch for custom resource of type {_specName}. Resubscribing...");
            if (exception is HttpRequestException<StatusV1> requestException)
            {
                HandleSubscriptionStatusException(requestException);
            }
            OnConnectionError?.Invoke(this, exception);
            Thread.Sleep(1000);
            Subscribe();
        }

        private void HandleSubscriptionStatusException(HttpRequestException<StatusV1> exception)
        {
            if (exception.StatusCode == HttpStatusCode.Gone)
            {
                _resources.Clear();
                _logger.LogDebug("Cleaned resource cache for '{0}' as the last seen resource version ({1}) is gone.", _specName, _lastSeenResourceVersion);
                _lastSeenResourceVersion = resourceVersionNone;
            }
            else
            {
                _logger.LogWarning(exception, $"Got an error from Kube API for resource '{_specName}': {exception.Response.Message}");
            }
        }

        private void OnCompleted()
        {
            _logger.LogDebug("Connection closed by Kube API during watch for custom resource of type {0}. Resubscribing...", _specName);
            OnConnectionError?.Invoke(this, new OperationCanceledException());
            Thread.Sleep(1000);
            Subscribe();
        }

        private void DisposeSubscriptions()
        {
            _subscription?.Dispose();
            _subscription = null;
            _logger.LogDebug("Unsubscribed from {0}.", _crdPluralName);
        }

        private bool TryValidateResource(CustomResource<TSpec> resource, out long parsedResourcedVersion)
        {
            long.TryParse(resource.Metadata.ResourceVersion, out parsedResourcedVersion);
            var existingResource = _resources.Values.FirstOrDefault(r => r.Metadata.Uid.Equals(resource.Metadata.Uid, StringComparison.InvariantCultureIgnoreCase));
            return existingResource == null || parsedResourcedVersion > long.Parse(existingResource.Metadata.ResourceVersion);
        }

        public virtual void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            DisposeSubscriptions();
        }

        private class ResourceMemento : IEnumerable<TSpec>
        {
            private readonly Dictionary<string, CustomResource<TSpec>> _toIterate;

            public ResourceMemento(Dictionary<string, CustomResource<TSpec>> toIterate) => _toIterate = toIterate;

            public IEnumerator<TSpec> GetEnumerator() => _toIterate.Values.Select(v => v.Spec).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class RawResourceMemento : IEnumerable<CustomResource<TSpec>>
        {
            private readonly Dictionary<string, CustomResource<TSpec>> _toIterate;

            public RawResourceMemento(Dictionary<string, CustomResource<TSpec>> toIterate) => _toIterate = toIterate;

            public IEnumerator<CustomResource<TSpec>> GetEnumerator() => _toIterate.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
