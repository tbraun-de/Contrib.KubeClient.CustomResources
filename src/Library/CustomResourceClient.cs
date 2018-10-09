using System;
using System.Threading;
using System.Threading.Tasks;
using HTTPlease;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.Extensions.Options;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Client for Kubernetes Custom Resources of a specific type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceClient<TResource> : KubeResourceClient, ICustomResourceClient<TResource>
        where TResource : CustomResource
    {
        private readonly TimeSpan _timeout;
        protected CustomResourceDefinition<TResource> CustomResourceDefinition { get; }

        /// <summary>
        /// Creates a Kubernetes Custom Resources client.
        /// </summary>
        /// <param name="client">The kube api client to be used.</param>
        /// <param name="crd">Information about the custom resource definition to work with.</param>
        /// <param name="options">The <see cref="KubernetesConfigurationStoreOptions"/> to be used.</param>
        public CustomResourceClient(IKubeApiClient client, CustomResourceDefinition<TResource> crd, IOptions<KubernetesConfigurationStoreOptions> options)
            : base(client)
        {
            CustomResourceDefinition = crd;
            _timeout = options.Value.WatchTimeout;
        }

        public virtual IObservable<IResourceEventV1<TResource>> Watch(string @namespace = "", string resourceVersionOffset = "0")
        {
            var httpRequest = CreateBaseRequest(@namespace)
                             .WithQueryParameter("watch", true)
                             .WithQueryParameter("resourceVersion", resourceVersionOffset)
                             .WithQueryParameter("timeoutSeconds", _timeout.TotalSeconds);

            return ObserveEvents<TResource>(httpRequest);
        }

        public virtual async Task<TResource> ReadAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(resourceName));

            var httpRequest = CreateBaseRequest(@namespace).WithRelativeUri(resourceName);
            return await GetSingleResource<TResource>(httpRequest, cancellationToken);
        }

        public virtual async Task<TResource> CreateAsync(TResource resource, CancellationToken cancellationToken = default)
        {
            var httpRequest = CreateBaseRequest(resource.Metadata.Namespace);
            var responseMessage = await Http.PostAsJsonAsync(httpRequest, resource, cancellationToken);
            return await responseMessage.ReadContentAsAsync<TResource, StatusV1>(responseMessage.StatusCode);
        }

        public virtual async Task<TResource> UpdateAsync(TResource resource, CancellationToken cancellationToken = default)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            var httpRequest = CreateBaseRequest(resource.Metadata.Namespace).WithRelativeUri(resource.Metadata.Name);

            var responseMessage = await Http.PutAsJsonAsync(httpRequest, resource, cancellationToken);
            return await responseMessage.ReadContentAsAsync<TResource>();
        }

        public virtual async Task<TResource> DeleteAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(resourceName));

            var deleteOptions = new DeleteOptionsV1 {PropagationPolicy = DeletePropagationPolicy.Foreground};
            var httpRequest = CreateBaseRequest(@namespace).WithRelativeUri(resourceName);
            var responseMessage = await Http.DeleteAsJsonAsync(httpRequest, deleteOptions, cancellationToken);

            return await responseMessage.ReadContentAsAsync<TResource, StatusV1>(responseMessage.StatusCode);
        }

        private HttpRequest CreateBaseRequest(string @namespace)
        {
            var httpRequest = KubeRequest.Create($"/apis/{CustomResourceDefinition.ApiVersion}");

            if (!string.IsNullOrWhiteSpace(@namespace))
                httpRequest = httpRequest.WithRelativeUri($"namespaces/{@namespace}/");

            return httpRequest.WithRelativeUri(CustomResourceDefinition.PluralName);
        }
    }
}
