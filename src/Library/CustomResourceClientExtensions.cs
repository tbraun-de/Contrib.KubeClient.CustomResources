using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HTTPlease;
using KubeClient.Models;
using Microsoft.Extensions.Logging;

namespace Contrib.KubeClient.CustomResources
{
    public static class CustomResourceClientExtensions
    {
        /// <summary>
        /// Updates an existing resource using GET, PUT and MVCC.
        /// </summary>
        /// <param name="client">The client used to perform operations.</param>
        /// <param name="resourceName">The name of the target resource to update.</param>
        /// <param name="modifyResource">A callback that applies the desired modifications to the resource.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the modified resource.</returns>
        public static async Task<TResource> UpdateAsync<TResource>(this ICustomResourceClient<TResource> client,
                                                                   string resourceName,
                                                                   Func<TResource, Task> modifyResource,
                                                                   string @namespace = null,
                                                                   CancellationToken cancellationToken = default)
            where TResource : CustomResource, IPayloadPatchable<TResource>
        {
            while (true)
            {
                var resource = await client.ReadAsync(resourceName, @namespace, cancellationToken);
                await modifyResource(resource);
                try
                {
                    return await client.ReplaceAsync(resource, cancellationToken);
                }
                catch (HttpRequestException<StatusV1> ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    client.KubeClient.LoggerFactory
                          .CreateLogger(typeof(CustomResourceClientExtensions).FullName)
                          .LogWarning(ex, "Conflict detected while updating {0}. Retrying in a moment.", resourceName);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Updates an existing resource using GET, PUT and MVCC.
        /// </summary>
        /// <param name="client">The client used to perform operations.</param>
        /// <param name="resourceName">The name of the target resource to update.</param>
        /// <param name="modifyResource">A callback that applies the desired modifications to the resource.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the modified resource.</returns>
        public static Task<TResource> UpdateAsync<TResource>(this ICustomResourceClient<TResource> client,
                                                             string resourceName,
                                                             Action<TResource> modifyResource,
                                                             string @namespace = null,
                                                             CancellationToken cancellationToken = default)
            where TResource : CustomResource, IPayloadPatchable<TResource>
            => client.UpdateAsync(resourceName, resource =>
            {
                modifyResource(resource);
                return Task.CompletedTask;
            }, @namespace, cancellationToken);

        /// <summary>
        /// Updates an existing resource using PATCH.
        /// </summary>
        /// <param name="client">The client used to perform operations.</param>
        /// <param name="resource">The new desired state of the resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the updated resource.</returns>
        public static Task<TResource> UpdateAsync<TResource>(this ICustomResourceClient<TResource> client,
                                                             TResource resource,
                                                             CancellationToken cancellationToken = default)
            where TResource : CustomResource, IPayloadPatchable<TResource>
            => client.UpdateAsync(
                resource.Metadata.Name,
                patch =>
                {
                    patch.Replace(x => x.Metadata.Labels, resource.Metadata.Labels);
                    resource.ToPayloadPatch(patch);
                },
                resource.Metadata.Namespace,
                cancellationToken);

        /// <summary>
        /// Realizes a desired state by creating, updating and deleting resources as required.
        /// </summary>
        /// <param name="client">The client used to perform operations.</param>
        /// <param name="desired">The desired state to achieve.</param>
        /// <param name="labelSelector">A label selector to restrict the modifications to.</param>
        /// <param name="namespace">A namespace to restrict the modifications to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task RealizeStateAsync<TResource>(this ICustomResourceClient<TResource> client,
                                                              IReadOnlyCollection<TResource> desired,
                                                              string labelSelector = null,
                                                              string @namespace = null,
                                                              CancellationToken cancellationToken = default)
            where TResource : CustomResource, IPayloadPatchable<TResource>
        {
            var existing = await client.ListAsync(labelSelector, @namespace, cancellationToken);

            foreach (var resource in desired)
            {
                var existingResource = existing.FirstOrDefault(resource.NameEquals);

                if (existingResource == null)
                    await client.CreateAsync(resource, cancellationToken);
                else if (!existingResource.Equals(resource))
                    await client.UpdateAsync(resource, cancellationToken);
            }

            foreach (var resource in existing.Where(desired.DoesNotContain))
                await client.DeleteAsync(resource.Metadata.Name, resource.Metadata.Namespace, cancellationToken);
        }

        private static bool DoesNotContain(this IEnumerable<CustomResource> list, CustomResource element)
            => !list.Any(element.NameEquals);
    }
}
