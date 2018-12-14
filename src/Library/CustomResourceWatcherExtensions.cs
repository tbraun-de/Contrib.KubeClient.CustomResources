using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class CustomResourceWatcherExtensions
    {
        /// <summary>
        /// Finds a resource by its metadata.name property.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No element in the store has the given <paramref name="name"/>.</exception>
        public static TResource FindByName<TResource>(this ICustomResourceWatcher<TResource> watcher, string name)
            where TResource : CustomResource
        {
            var customResource = watcher.FirstOrDefault(res => res.Metadata.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (customResource == null)
                throw new KeyNotFoundException($"No such resource '{name}'");

            return customResource;
        }

        /// <summary>
        /// Finds all resources by its metadata.name property.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static IEnumerable<TResource> FindByNamespace<TResource>(this ICustomResourceWatcher<TResource> watcher, string @namespace)
            where TResource : CustomResource
            => watcher.Where(res => res.Metadata.Namespace.Equals(@namespace, StringComparison.InvariantCultureIgnoreCase));
    }
}
