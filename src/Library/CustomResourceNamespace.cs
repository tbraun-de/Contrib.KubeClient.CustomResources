using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Specifies the Kubernetes namespace to use for a specific Resources type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceNamespace<TResource>
    {
        /// <summary>
        /// Creates a new namespace selector.
        /// </summary>
        /// <param name="value">The name of the Kubernetes namespace.</param>
        public CustomResourceNamespace([CanBeNull] string value)
        {
            Value = value;
        }

        /// <summary>
        /// The name of the Kubernetes namespace.
        /// </summary>
        [CanBeNull]
        public string Value { get; }
    }
}
