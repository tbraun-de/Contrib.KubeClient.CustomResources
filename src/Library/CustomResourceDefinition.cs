using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResourceDefinition<TResource>
        where TResource: CustomResource
    {
        public CustomResourceDefinition(string apiVersion, string pluralName)
        {
            ApiVersion = apiVersion;
            PluralName = pluralName;
        }

        /// <summary>
        /// The crd API Version (&lt;apiGroupName&gt;/&lt;version&gt;, e.g. 'your.company/v1').
        /// </summary>
        public string ApiVersion { get; }

        /// <summary>
        /// The plural name (see <code>spec.names.plural</code>) of the CRD.
        /// </summary>
        public string PluralName { get; }
    }
}
