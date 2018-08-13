using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResource<TSpec> : KubeResourceV1
    {
        public TSpec Spec { get; set; }

        public StatusV1 Status { get; set; }

        public string GlobalName
        {
            get
            {
                return string.IsNullOrWhiteSpace(Metadata.Namespace)
                           ? $"[cluster].{Metadata.Name}"
                           : $"{Metadata.Namespace}.{Metadata.Name}";
            }
        }
    }
}
