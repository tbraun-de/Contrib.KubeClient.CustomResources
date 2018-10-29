using System;
using System.Threading;
using System.Threading.Tasks;
using HTTPlease;
using HTTPlease.Formatters.Json;
using KubeClient;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Client for Kubernetes Custom Resources of a specific type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceClient<TResource> : KubeResourceClient, ICustomResourceClient<TResource>
        where TResource : CustomResource, new()
    {
        private readonly CustomResourceDefinition _crd;

        /// <summary>
        /// Creates a Kubernetes Custom Resources client.
        /// </summary>
        /// <param name="client">The kube api client to be used.</param>
        public CustomResourceClient(IKubeApiClient client)
            : base(client)
        {
            _crd = new TResource().Definition;
        }

        /// <summary>
        /// The timeout for watching kubernetes event streams, after which the stream will be closed automatically.
        /// </summary>
        protected virtual TimeSpan WatchTimeout => TimeSpan.FromMinutes(5);

        public virtual IObservable<IResourceEventV1<TResource>> Watch(string @namespace = "", string resourceVersionOffset = "0")
        {
            var httpRequest = CreateBaseRequest(@namespace)
                             .WithQueryParameter("watch", true)
                             .WithQueryParameter("resourceVersion", resourceVersionOffset)
                             .WithQueryParameter("timeoutSeconds", WatchTimeout.TotalSeconds);

            return ObserveEvents<TResource>(httpRequest, $"watch {_crd.PluralName} in {@namespace}");
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

        public virtual async Task<TResource> UpdateAsync(string name, Action<JsonPatchDocument<TResource>> patchAction, string @namespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(name));
            if (patchAction == null)
                throw new ArgumentNullException(nameof(patchAction));

            var httpRequest = CreateBaseRequest(@namespace).WithRelativeUri(name);
            return await PatchResource(patchAction, httpRequest, cancellationToken);
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
            // TODO: Once KubeClient uses Camel-Case resolver by default, replace the block below with:
            // var httpRequest = KubeRequest.Create($"/apis/{_crd.ApiVersion}");
            var jsonFormatter = new JsonFormatter
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Converters = {new StringEnumConverter()}
                },
                SupportedMediaTypes = {PatchMediaType, MergePatchMediaType}
            };
            var httpRequest = HttpRequest.Create($"/apis/{_crd.ApiVersion}").ExpectJson().WithFormatter(jsonFormatter);

            if (!string.IsNullOrWhiteSpace(@namespace))
                httpRequest = httpRequest.WithRelativeUri($"namespaces/{@namespace}/");

            return httpRequest.WithRelativeUri(_crd.PluralName);
        }
    }
}
