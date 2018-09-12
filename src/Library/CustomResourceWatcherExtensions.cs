using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class CustomResourceWatcherExtensions
    {
        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static Task<IEnumerable<CustomResource<TResourceSpec>>> FindAllAsync<TResourceSpec>(this ICustomResourceWatcher<TResourceSpec> watcher)
            => Task.FromResult(watcher.RawResources);

        /// <summary>
        /// Finds a resource by its metadata.name property.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No element in the store has the given <paramref name="name"/>.</exception>
        public static Task<CustomResource<TResourceSpec>> FindByNameAsync<TResourceSpec>(this ICustomResourceWatcher<TResourceSpec> watcher, string name)
        {
            var customResource = watcher.RawResources.FirstOrDefault(res => res.Metadata.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (customResource == null)
                throw new KeyNotFoundException($"No such resource '{name}'");

            return Task.FromResult(customResource);
        }

        /// <summary>
        /// Finds all resources by its metadata.name property.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static Task<IEnumerable<CustomResource<TResourceSpec>>> FindByNamespaceAsync<TResourceSpec>(this ICustomResourceWatcher<TResourceSpec> watcher, string @namespace)
            => FindAsync(watcher, res => res.Metadata.Namespace.Equals(@namespace, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Finds all resources by the given query.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static Task<IEnumerable<CustomResource<TResourceSpec>>> FindAsync<TResourceSpec>(this ICustomResourceWatcher<TResourceSpec> watcher, Func<CustomResource<TResourceSpec>, bool> query)
            => Task.FromResult(watcher.RawResources.Where(query));

        /// <summary>
        /// Counts all stored resources.
        /// </summary>
        public static Task<long> CountAsync<TResourceSpec>(this ICustomResourceWatcher<TResourceSpec> watcher)
            => Task.FromResult(watcher.RawResources.LongCount());
    }
}
