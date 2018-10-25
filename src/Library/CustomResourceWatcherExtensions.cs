using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class CustomResourceWatcherExtensions
    {
        /// <summary>
        /// Starts watching for changes to the resource collection. Stop by calling <see cref="Stop"/>.
        /// </summary>
        public static void Start(this ICustomResourceWatcher watcher)
            => watcher.StartAsync(CancellationToken.None).Wait();

        /// <summary>
        /// Stops watching for changes to the resource collection.
        /// </summary>
        public static void Stop(this ICustomResourceWatcher watcher)
            => watcher.StopAsync(CancellationToken.None).Wait();

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static Task<IEnumerable<TResource>> FindAllAsync<TResource>(this ICustomResourceWatcher<TResource> watcher)
            where TResource : CustomResource
            => Task.FromResult(watcher.RawResources);

        /// <summary>
        /// Finds a resource by its metadata.name property.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No element in the store has the given <paramref name="name"/>.</exception>
        public static Task<TResource> FindByNameAsync<TResource>(this ICustomResourceWatcher<TResource> watcher, string name)
            where TResource : CustomResource
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
        public static Task<IEnumerable<TResource>> FindByNamespaceAsync<TResource>(this ICustomResourceWatcher<TResource> watcher, string @namespace)
            where TResource : CustomResource
            => FindAsync(watcher, res => res.Metadata.Namespace.Equals(@namespace, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        /// Finds all resources by the given query.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        public static Task<IEnumerable<TResource>> FindAsync<TResource>(this ICustomResourceWatcher<TResource> watcher, Func<TResource, bool> query) where TResource: CustomResource
            => Task.FromResult(watcher.RawResources.Where(query));

        /// <summary>
        /// Counts all stored resources.
        /// </summary>
        public static Task<long> CountAsync<TResource>(this ICustomResourceWatcher<TResource> watcher)
            where TResource : CustomResource
            => Task.FromResult(watcher.RawResources.LongCount());
    }
}
