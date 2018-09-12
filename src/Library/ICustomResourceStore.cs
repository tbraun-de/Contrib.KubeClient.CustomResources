using System.Threading.Tasks;

namespace Contrib.KubeClient.CustomResources
{
    public interface ICustomResourceStore<TResourceSpec> : ICustomResourceReadonlyStore<TResourceSpec>
    {
        /// <summary>
        /// Adds the given <paramref name="resource"/> in the kube api.
        /// </summary>
        Task<CustomResource<TResourceSpec>> AddAsync(CustomResource<TResourceSpec> resource);

        /// <summary>
        /// Updates an existing resource given by <paramref name="resource"/>.
        /// </summary>
        /// <param name="resource">The resource to overwrite.</param>
        /// <returns>A <see cref="CustomResource{TResourceSpec}"/> representing the current state for the updated resource.</returns>
        Task<CustomResource<TResourceSpec>> UpdateAsync(CustomResource<TResourceSpec> resource);

        /// <summary>
        /// Deletes an existing resource given by <paramref name="resourceName"/>.
        /// </summary>
        /// <param name="resourceName">The name of the target resource to delete.</param>
        /// <param name="namespace">The namespace the resource is located in.</param>
        /// <returns>A <see cref="CustomResource{TResourceSpec}"/> representing the resource`s most recent state before it was deleted.</returns>
        Task<CustomResource<TResourceSpec>> DeleteAsync(string resourceName, string @namespace = null);
    }
}
