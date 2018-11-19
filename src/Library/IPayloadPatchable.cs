using KubeClient.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// A Kubernetes resource that can represent its payload (usually spec) as a JSON Patch.
    /// </summary>
    public interface IPayloadPatchable<TResource>
        where TResource : KubeResourceV1
    {
        /// <summary>
        /// Encodes the resource's payload (usually spec) in the <paramref name="patch"/> document.
        /// </summary>
        void ToPayloadPatch(JsonPatchDocument<TResource> patch);
    }
}
