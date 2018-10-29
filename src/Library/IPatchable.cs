using KubeClient.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// A Kubernetes resource that can represent its payload (usually spec) without its metadata as a JSON Patch.
    /// </summary>
    public interface IPatchable<TResource>
        where TResource : KubeResourceV1
    {
        /// <summary>
        /// Applies the resources payload (usually spec) to the <paramref name="patch"/> document.
        /// </summary>
        void Patch(JsonPatchDocument<TResource> patch);
    }
}
