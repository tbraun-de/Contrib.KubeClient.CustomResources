using System;
using System.Threading;
using System.Threading.Tasks;
using HTTPlease;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Client for Kubernetes Custom Resources of a specific type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceClient<TResource> : KubeResourceClient, ICustomResourceClient<TResource>
        where TResource : CustomResource
    {
        protected CustomResourceDefinition<TResource> Definition { get; }

        /// <summary>
        /// Creates a Kubernetes Custom Resources client.
        /// </summary>
        /// <param name="client">The kube api client to be used.</param>
        /// <param name="definition">Information about the custom resource definition to work with.</param>
        public CustomResourceClient(IKubeApiClient client, CustomResourceDefinition<TResource> definition)
            : base(client)
        {
            Definition = definition;
        }

        public virtual IObservable<IResourceEventV1<TResource>> Watch(string @namespace = "", string resourceVersionOffset = "0")
        {
            var httpRequest = CreateBaseRequest(@namespace)
                             .WithQueryParameter("watch", true)
                             .WithQueryParameter("resourceVersion", resourceVersionOffset)
                             .WithQueryParameter("timeoutSeconds", TimeSpan.FromMinutes(5));

            return ObserveEvents<TResource>(httpRequest, $"watching {Definition.PluralName} in {@namespace}");
        }

        public async Task<CustomResourceList<TResource>> ListAsync(string labelSelector = null, string @namespace = null, CancellationToken cancellationToken = default)
        {
            var httpRequest = CreateBaseRequest(@namespace);
            if (!string.IsNullOrWhiteSpace(labelSelector))
                httpRequest = httpRequest.WithQueryParameter("labelSelector", labelSelector);

            return await GetResourceList<CustomResourceList<TResource>>(httpRequest, cancellationToken);
        }

        public virtual async Task<TResource> ReadAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespaces.", nameof(resourceName));

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
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespaces.", nameof(resourceName));

            var deleteOptions = new DeleteOptionsV1 {PropagationPolicy = DeletePropagationPolicy.Foreground};
            var httpRequest = CreateBaseRequest(@namespace).WithRelativeUri(resourceName);
            var responseMessage = await Http.DeleteAsJsonAsync(httpRequest, deleteOptions, cancellationToken);

            return await responseMessage.ReadContentAsAsync<TResource, StatusV1>(responseMessage.StatusCode);
        }

        private HttpRequest CreateBaseRequest(string @namespace)
        {
            var httpRequest = KubeRequest.Create($"/apis/{Definition.ApiVersion}");

            if (!string.IsNullOrWhiteSpace(@namespace))
                httpRequest = httpRequest.WithRelativeUri($"namespaces/{@namespace}/");

            return httpRequest.WithRelativeUri(Definition.PluralName);
        }
    }
}
