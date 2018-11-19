using System;
using System.Threading;
using System.Threading.Tasks;
using KubeClient.Models;
using KubeClient.ResourceClients;
using Microsoft.AspNetCore.JsonPatch;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Client for Kubernetes Custom Resources of a specific type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public interface ICustomResourceClient<TResource> : IKubeResourceClient
        where TResource : CustomResource
    {
        /// <summary>
        /// Watches for events related to this type of resource.
        /// </summary>
        /// <param name="namespace">The target Kubernetes namespace to watch for (leave empty for cluster-wide watch).</param>
        /// <param name="resourceVersionOffset">The resource version to start from (defaults to 0).</param>
        IObservable<IResourceEventV1<TResource>> Watch(string @namespace = "", string resourceVersionOffset = "0");

        /// <summary>
        /// Lists all instances of this type of resource.
        /// </summary>
        /// <param name="labelSelector">An optional Kubernetes label selector expression used to filter the resources.</param>
        /// <param name="namespace">The namespace to check for resources.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<CustomResourceList<TResource>> ListAsync(string labelSelector = null, string @namespace = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns an existing resource given by <paramref name="resourceName"/> within an optional <paramref name="namespace"/>.
        /// </summary>
        /// <param name="resourceName">The name of the target resource to return.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task<TResource> ReadAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new resource.
        /// </summary>
        /// <param name="resource">The resource to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the newly-created resource.</returns>
        Task<TResource> CreateAsync(TResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing resource using PATCH.
        /// </summary>
        /// <param name="name">The name of the target resource.</param>
        /// <param name="patchAction">A delegate that customizes the patch operation.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the updated resource.</returns>
        Task<TResource> UpdateAsync(string name, Action<JsonPatchDocument<TResource>> patchAction, string @namespace = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an existing resource using PUT.
        /// </summary>
        /// <param name="resource">The resource to replace. Must be an object retrieved using <see cref="ListAsync"/> or <see cref="ReadAsync"/> and then modified.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the modified resource.</returns>
        Task<TResource> ReplaceAsync(TResource resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an existing resource given by <paramref name="resourceName"/>.
        /// </summary>
        /// <param name="resourceName">The name of the target resource to delete.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The resource`s most recent state before it was deleted.</returns>
        Task<TResource> DeleteAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default);
    }
}
