using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResource<TResourceSpec> : KubeResourceV1
    {
        public TResourceSpec Spec { get; set; }

        public StatusV1 Status { get; set; }

        public string GlobalName
            => string.IsNullOrWhiteSpace(Metadata.Namespace)
                   ? $"[cluster].{Metadata.Name}"
                   : $"{Metadata.Namespace}.{Metadata.Name}";
    }
}
