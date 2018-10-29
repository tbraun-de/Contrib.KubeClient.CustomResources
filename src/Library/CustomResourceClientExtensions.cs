using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KubeClient.Models;

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
                var existingResource = existing.FirstOrDefault(resource.NameMatch);

                if (existingResource == null)
                    await client.CreateAsync(resource, cancellationToken);
                else if (!existingResource.Equals(resource))
                    await client.UpdateAsync(resource.Metadata.Name, resource.Patch, resource.Metadata.Namespace, cancellationToken);
            }

            foreach (var resource in existing.Where(desired.DoesNotContain))
                await client.DeleteAsync(resource.Metadata.Name, resource.Metadata.Namespace, cancellationToken);
        }

        public static bool NameMatch(this KubeResourceV1 a, KubeResourceV1 b)
            => a.Metadata.Name == b.Metadata.Name
            && (a.Metadata.Namespace ?? "") == (b.Metadata.Namespace ?? "");

        private static bool DoesNotContain(this IEnumerable<KubeResourceV1> list, KubeResourceV1 element)
            => !list.Any(element.NameMatch);
    }
}
