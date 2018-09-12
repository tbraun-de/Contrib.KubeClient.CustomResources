﻿using System;
using System.Threading;
using System.Threading.Tasks;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    public interface ICustomResourceClient<TResourceSpec>
    {
        /// <summary>
        /// Watches for events related to <see cref="CustomResource{TResourceSpec}"/>.
        /// </summary>
        /// <param name="namespace">The target Kubernetes namespace to watch for (leave empty for cluster-wide watch).</param>
        /// <param name="resourceVersionOffset">The resource version to start from (defaults to 0).</param>
        IObservable<IResourceEventV1<CustomResource<TResourceSpec>>> Watch(string @namespace = "", string resourceVersionOffset = "0");

        /// <summary>
        /// Creates a new resource.
        /// </summary>
        /// <param name="resource">The resource to create.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="CustomResource{TResourceSpec}"/> representing the current state for the newly-created resource.</returns>
        Task<CustomResource<TResourceSpec>> CreateAsync(CustomResource<TResourceSpec> resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing resource given by <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The resource to overwrite.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="CustomResource{TResourceSpec}"/> representing the current state for the updated resource.</returns>
        Task<CustomResource<TResourceSpec>> UpdateAsync(CustomResource<TResourceSpec> resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an existing resource given by <paramref name="resourceName"/>.
        /// </summary>
        /// <param name="resourceName">The name of the target resource to delete.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="CustomResource{TResourceSpec}"/> representing the resource`s most recent state before it was deleted.</returns>
        Task<CustomResource<TResourceSpec>> DeleteAsync(string resourceName, string @namespace = null, CancellationToken cancellationToken = default);
    }
}
