using System.Collections.Generic;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Extension methods for <see cref="KubeResourceV1"/> that simplify working with Kubernetes labels.
    /// </summary>
    public static class KubernetesLabelExtensions
    {
        /// <summary>
        /// Gets a Kubernetes label from the resource or an empty string.
        /// </summary>
        public static string GetLabelOrStringEmpty(this KubeResourceV1 resource, string key)
            => resource.Metadata.Labels.TryGetValue(key, out string value) ? value : string.Empty;

        /// <summary>
        /// Gets a Kubernetes labels from the resource if present.
        /// </summary>
        public static bool TryGetLabel(this KubeResourceV1 resource, string labelName, out string value)
            => resource.Metadata.Labels.TryGetValue(labelName, out value);

        /// <summary>
        /// Determines if a Kubernetes resource has a specific label with a specific value.
        /// </summary>
        public static bool HasLabel(this KubeResourceV1 resource, string labelName, string expectedValue)
            => resource.TryGetLabel(labelName, out string value) && value == expectedValue;

        /// <summary>
        /// Determines if a Kubernetes resource has a specific label with a value matching one of a set of <paramref name="expectedValues"/>.
        /// </summary>
        public static bool HasLabel(this KubeResourceV1 resource, string labelName, ISet<string> expectedValues)
            => resource.TryGetLabel(labelName, out string value) && expectedValues.Contains(value);
    }
}
