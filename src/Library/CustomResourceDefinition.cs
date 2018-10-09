namespace Contrib.KubeClient.CustomResources
{
    // ReSharper disable once UnusedTypeParameter
    /// <summary>
    /// Identifies a specific Kubernetes Custom Resource type.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public class CustomResourceDefinition<TResource>
        where TResource: CustomResource
    {
        /// <summary>
        /// Creates a new Kubernetes Custom Resource Definition.
        /// </summary>
        /// <param name="apiVersion">The Kubernetes API Version of the resource (&lt;apiGroupName&gt;/&lt;version&gt;, e.g. 'your.company/v1').</param>
        /// <param name="pluralName">The plural name of the resource (see <code>spec.names.plural</code>).</param>
        public CustomResourceDefinition(string apiVersion, string pluralName)
        {
            ApiVersion = apiVersion;
            PluralName = pluralName;
        }

        /// <summary>
        /// The Kubernetes API Version of the resource (&lt;apiGroupName&gt;/&lt;version&gt;, e.g. 'your.company/v1').
        /// </summary>
        public string ApiVersion { get; }

        /// <summary>
        /// The plural name of the resource (see <code>spec.names.plural</code>).
        /// </summary>
        public string PluralName { get; }

        public override string ToString() => $"{ApiVersion}/{PluralName}";
    }
}
