using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Contrib.KubeClient.CustomResources
{
    public static class CustomResourceClientExtensions
    {
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
            where TResource : CustomResource, IPatchable<TResource>
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

        /// <summary>
        /// Updates an existing resource.
        /// </summary>
        /// <param name="client">The client used to perform operations.</param>
        /// <param name="resource">The new desired state of the resource.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current state for the updated resource.</returns>
        public static Task UpdateAsync<TResource>(this ICustomResourceClient<TResource> client,
                                                  TResource resource,
                                                  CancellationToken cancellationToken = default)
            where TResource : CustomResource, IPatchable<TResource>
            => client.UpdateAsync(resource.Metadata.Name, resource.Patch, resource.Metadata.Namespace, cancellationToken);

        private static bool DoesNotContain(this IEnumerable<CustomResource> list, CustomResource element)
            => !list.Any(element.NameEquals);
    }
}
