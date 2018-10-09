using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading;
using HTTPlease;
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
        where TResource : CustomResource
    {
        private const long resourceVersionNone = 0;
        private readonly Dictionary<string, TResource> _resources = new Dictionary<string, TResource>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<CustomResourceWatcher<TResource>> _logger;
        private readonly CustomResourceDefinition<TResource> _crd;
        [NotNull] private readonly string _namespace;
        private IDisposable _subscription;
        private long _lastSeenResourceVersion = resourceVersionNone;

        public CustomResourceWatcher(ILogger<CustomResourceWatcher<TResource>> logger, ICustomResourceClient<TResource> client, CustomResourceDefinition<TResource> crd, CustomResourceNamespace<TResource> @namespace = null)
        {
            _logger = logger;
            _crd = crd;
            Client = client;
            _namespace = @namespace?.Value ?? "";
        }

        public ICustomResourceClient<TResource> Client { get; }
        public IEnumerable<TResource> RawResources => new RawResourceMemento(_resources);
        public event EventHandler<Exception> ConnectionError;
        public event EventHandler Connected;
        public event EventHandler DataChanged;

        public bool IsActive => _subscription != null;

        public virtual void StartWatching()
        {
            if (_subscription == null)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            DisposeSubscriptions();
            _subscription = Client.Watch(_namespace, _lastSeenResourceVersion.ToString()).Subscribe(OnNext, OnError, OnCompleted);
            Connected?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation($"Subscribed to {_crd.PluralName}.");
        }

        private void OnNext(IResourceEventV1<TResource> @event)
        {
            if (!TryValidateResource(@event.Resource, out long resourceVersion))
            {
                _logger.LogTrace("Got outdated resource version '{0}' for '{1}' with name '{2}'", @event.Resource.Metadata.ResourceVersion, _crd, @event.Resource.GlobalName);
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
                    _logger.LogWarning("Got erroneous resource '{0}' with '{1}'.", _crd, @event.Resource.GlobalName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DeleteResource(IResourceEventV1<TResource> @event)
        {
            if (_resources.Remove(@event.Resource.Metadata.Uid))
            {
                OnDataChanged();
                _logger.LogDebug("Removed resource '{0}' with name '{1}'", _crd, @event.Resource.GlobalName);
            }
        }

        private void UpsertResource(IResourceEventV1<TResource> @event)
        {
            if (_resources.ContainsKey(@event.Resource.Metadata.Uid)
             && !_resources[@event.Resource.Metadata.Uid].Metadata.ResourceVersion.Equals(@event.Resource.Metadata.ResourceVersion))
            {
                _resources[@event.Resource.Metadata.Uid] = @event.Resource;
                OnDataChanged();
                _logger.LogDebug("Modified resource '{0}' with name '{1}'", _crd, @event.Resource.GlobalName);
            }
            else if (!_resources.ContainsKey(@event.Resource.Metadata.Uid))
            {
                _resources.Add(@event.Resource.Metadata.Uid, @event.Resource);
                OnDataChanged();
                _logger.LogDebug("Added resource '{0}' with name '{1}'", _crd, @event.Resource.GlobalName);
            }
            else
            {
                _logger.LogDebug("Got resource '{0}' with name '{1}' without changes", _crd, @event.Resource.GlobalName);
            }
        }

        private void OnError(Exception exception)
        {
            _logger.LogError(exception, $"Error occured during watch for custom resource of type {_crd}. Resubscribing...");
            if (exception is HttpRequestException<StatusV1> requestException)
            {
                HandleSubscriptionStatusException(requestException);
            }
            ConnectionError?.Invoke(this, exception);
            Thread.Sleep(1000);
            Subscribe();
        }

        private void OnCompleted()
        {
            _logger.LogDebug("Connection closed by Kube API during watch for custom resource of type {0}. Resubscribing...", _crd);
            ConnectionError?.Invoke(this, new OperationCanceledException());
            Thread.Sleep(1000);
            Subscribe();
        }

        private void OnDataChanged() => DataChanged?.Invoke(this, new EventArgs());

        private void HandleSubscriptionStatusException(HttpRequestException<StatusV1> exception)
        {
            if (exception.StatusCode == HttpStatusCode.Gone)
            {
                _resources.Clear();
                OnDataChanged();
                _logger.LogDebug("Cleaned resource cache for '{0}' as the last seen resource version ({1}) is gone.", _crd, _lastSeenResourceVersion);
                _lastSeenResourceVersion = resourceVersionNone;
            }
            else
            {
                _logger.LogWarning(exception, "Got an error from Kube API for resource '{0}': {1}", _crd, exception.Response.Message);
            }
        }

        private void DisposeSubscriptions()
        {
            _subscription?.Dispose();
            _subscription = null;
            _logger.LogDebug("Unsubscribed from {0}.", _crd.PluralName);
        }

        private bool TryValidateResource(CustomResource resource, out long parsedResourcedVersion)
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

        [ExcludeFromCodeCoverage]
        private class RawResourceMemento : IEnumerable<TResource>
        {
            private readonly Dictionary<string, TResource> _toIterate;

            public RawResourceMemento(Dictionary<string, TResource> toIterate) => _toIterate = toIterate;

            public IEnumerator<TResource> GetEnumerator() => _toIterate.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
