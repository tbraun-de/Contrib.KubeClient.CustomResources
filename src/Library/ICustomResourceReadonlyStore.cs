using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// A storage for custom resources.
    /// </summary>
    [PublicAPI]
    public interface ICustomResourceReadonlyStore<TResource>
    {
        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<CustomResource<TResource>>> FindAllAsync();

        /// <summary>
        /// Finds a resource by its metadata.name property.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No element in the store has the given <paramref name="name"/>.</exception>
        Task<CustomResource<TResource>> FindByNameAsync([NotNull] string name);

        /// <summary>
        /// Finds all resources by its metadata.name property.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        Task<IEnumerable<CustomResource<TResource>>> FindByNamespaceAsync([NotNull] string @namespace);

        /// <summary>
        /// Finds all resources by the given query.
        /// </summary>
        /// <returns>Empty enumerable if nothing found.</returns>
        Task<IEnumerable<CustomResource<TResource>>> FindAsync([NotNull] Func<CustomResource<TResource>, bool> query);

        /// <summary>
        /// Counts all stored resources.
        /// </summary>
        Task<long> CountAsync();
    }
}
