using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using KubeClient.Models;
using Newtonsoft.Json;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResource : KubeResourceV1
    {
        [JsonIgnore]
        public string GlobalName
            => string.IsNullOrWhiteSpace(Metadata.Namespace)
                   ? $"[cluster].{Metadata.Name}"
                   : $"{Metadata.Namespace}.{Metadata.Name}";
    }

    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResource<TSpec> : CustomResource
    {
        public TSpec Spec { get; set; }
        public StatusV1 Status { get; set; }
    }

    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public class CustomResource<TSpec, TStatus> : CustomResource
    {
        public TSpec Spec { get; set; }
        public TStatus Status { get; set; }
    }
}
