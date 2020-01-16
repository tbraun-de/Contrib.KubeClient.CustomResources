using System.Collections.Generic;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Extension methods for <see cref="KubeResourceV1"/> that simplify working with Kubernetes annotations.
    /// </summary>
    public static class KubernetesAnnotationExtensions
    {
        /// <summary>
        /// Gets a Kubernetes annotation from the resource or an empty string.
        /// </summary>
        public static string GetAnnotationOrStringEmpty(this KubeResourceV1 resource, string key)
            => resource.Metadata.Annotations.TryGetValue(key, out string value) ? value : string.Empty;

        /// <summary>
        /// Gets a Kubernetes annotation from the resource if present.
        /// </summary>
        public static bool TryGetAnnotation(this KubeResourceV1 resource, string key, out string value)
            => resource.Metadata.Annotations.TryGetValue(key, out value);

        /// <summary>
        /// Determines if a Kubernetes resource has a specific annotation with a specific value.
        /// </summary>
        public static bool HasAnnotation(this KubeResourceV1 resource, string key, string expectedValue)
            => resource.TryGetAnnotation(key, out string value) && value == expectedValue;

        /// <summary>
        /// Determines if a Kubernetes resource has a specific annotation with a value matching one of a set of <paramref name="expectedValues"/>.
        /// </summary>
        public static bool HasAnnotation(this KubeResourceV1 resource, string key, ISet<string> expectedValues)
            => resource.TryGetAnnotation(key, out string value) && expectedValues.Contains(value);
    }
}
