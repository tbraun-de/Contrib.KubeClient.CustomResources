using System;
using System.Diagnostics.CodeAnalysis;
using HTTPlease;
using JetBrains.Annotations;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Options;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [UsedImplicitly]
    public class CustomResourceClient : KubeResourceClient, ICustomResourceClient
    {
        private readonly TimeSpan _timeout;

        public CustomResourceClient(IKubeApiClient client, IOptions<KubernetesConfigurationStoreOptions> options)
            : base(client)
        {
            _timeout = options.Value.WatchTimeout;
        }

        public IObservable<IResourceEventV1<CustomResource<TSpec>>> Watch<TSpec>(string apiGroup, string crdPluralName, string @namespace = "", string lastSeenResourceVersion = "0")
        {
            var httpRequest = KubeRequest.Create($"/apis/{apiGroup}/v1/");

            if (!string.IsNullOrWhiteSpace(@namespace))
                httpRequest = httpRequest.WithRelativeUri($"namespaces/{@namespace}/");

            httpRequest = httpRequest
                         .WithRelativeUri($"{crdPluralName}")
                         .WithQueryParameter("watch", true)
                         .WithQueryParameter("resourceVersion", lastSeenResourceVersion)
                         .WithQueryParameter("timeoutSeconds", _timeout.TotalSeconds);

            return ObserveEvents<CustomResource<TSpec>>(httpRequest);
        }
    }
}
