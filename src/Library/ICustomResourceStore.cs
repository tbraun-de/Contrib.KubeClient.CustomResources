using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// A storage for custom resources.
    /// </summary>
    public interface ICustomResourceStore<TResource>
    {
        /// <summary>
        /// Finds a resource by its metadata.name property.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">No element in the store has the given <paramref name="name"/></exception>
        Task<CustomResource<TResource>> FindByNameAsync([NotNull] string name);

        /// <summary>
        /// Finds all resources by its metadata.name property.
        /// </summary>
        /// <returns>empty enumerable if nothing found</returns>
        Task<IEnumerable<CustomResource<TResource>>> FindByNamespaceAsync([NotNull] string @namespace);

        /// <summary>
        /// Finds all resources by the given query.
        /// </summary>
        /// <returns>empty enumerable if nothing found</returns>
        Task<IEnumerable<CustomResource<TResource>>> FindAsync([NotNull] Func<CustomResource<TResource>, bool> query);

        /// <summary>
        /// Counts all stored resources
        /// </summary>
        Task<long> Count();
    }
}
