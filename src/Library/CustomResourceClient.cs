using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
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
    [PublicAPI]
    public class CustomResourceClient<TResourceSpec> : KubeResourceClient, ICustomResourceClient<TResourceSpec>
    {
        private readonly TimeSpan _timeout;
        protected CustomResourceDefinition<TResourceSpec> CustomResourceDefinition { get; private set; }

        /// <summary>
        /// Creates a <see cref="CustomResourceClient{TSpec}"/>
        /// </summary>
        /// <param name="client">The kube api client to be used.</param>
        /// <param name="crd">Information about the custom resource definition to work with.</param>
        /// <param name="options">The <see cref="KubernetesConfigurationStoreOptions"/> to be used.</param>
        public CustomResourceClient(IKubeApiClient client, CustomResourceDefinition<TResourceSpec> crd, IOptions<KubernetesConfigurationStoreOptions> options)
            : base(client)
        {
            CustomResourceDefinition = crd;
            _timeout = options.Value.WatchTimeout;
        }

        public virtual IObservable<IResourceEventV1<CustomResource<TResourceSpec>>> Watch(string @namespace = "", string resourceVersionOffset = "0")
        {
            var httpRequest = CreateBaseRequest(@namespace)
                             .WithQueryParameter("watch", true)
                             .WithQueryParameter("resourceVersion", resourceVersionOffset)
                             .WithQueryParameter("timeoutSeconds", _timeout.TotalSeconds);

            return ObserveEvents<CustomResource<TResourceSpec>>(httpRequest);
        }

        public virtual async Task<CustomResource<TResourceSpec>> CreateAsync(CustomResource<TResourceSpec> resource, CancellationToken cancellationToken = default)
        {
            var httpRequest = CreateBaseRequest(resource.Metadata.Namespace);
            var responseMessage = await Http.PostAsJsonAsync(httpRequest, resource, cancellationToken);
            return await responseMessage.ReadContentAsAsync<CustomResource<TResourceSpec>, StatusV1>(responseMessage.StatusCode);
        }

        public virtual async Task<CustomResource<TResourceSpec>> UpdateAsync(CustomResource<TResourceSpec> resource, CancellationToken cancellationToken = default)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));

            var httpRequest = CreateBaseRequest(resource.Metadata.Namespace).WithRelativeUri(resource.Metadata.Name);

            var responseMessage = await Http.PutAsJsonAsync(httpRequest, resource, cancellationToken);
            return await responseMessage.ReadContentAsAsync<CustomResource<TResourceSpec>>();
        }

        public virtual async Task<CustomResource<TResourceSpec>> DeleteAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'name'.", nameof(resourceName));

            var deleteOptions = new DeleteOptionsV1 {PropagationPolicy = DeletePropagationPolicy.Foreground};
            var httpRequest = CreateBaseRequest(@namespace).WithRelativeUri(resourceName);
            var responseMessage = await Http.DeleteAsJsonAsync(httpRequest, deleteOptions, cancellationToken);

            return await responseMessage.ReadContentAsAsync<CustomResource<TResourceSpec>, StatusV1>(responseMessage.StatusCode);
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
