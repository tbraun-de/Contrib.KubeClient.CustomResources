using System;
using JetBrains.Annotations;
using KubeClient.Models;
using Newtonsoft.Json;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Base class for DTOs for Kubernetes Custom Resources.
    /// </summary>
    [PublicAPI]
    public class CustomResource : KubeResourceV1, IEquatable<CustomResource>
    {
        public CustomResource()
        {}

        public CustomResource(string @namespace, string name)
        {
            Metadata = new ObjectMetaV1
            {
                Namespace = @namespace,
                Name = name
            };
        }

        [JsonIgnore]
        public string GlobalName
            => string.IsNullOrWhiteSpace(Metadata.Namespace)
                   ? $"[cluster].{Metadata.Name}"
                   : $"{Metadata.Namespace}.{Metadata.Name}";

        public bool Equals(CustomResource other)
            => other != null
            && Metadata.Namespace == other.Metadata.Namespace
            && Metadata.Name == other.Metadata.Name;

        public override bool Equals(object obj) => obj is CustomResource other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Metadata?.Namespace?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Metadata?.Name?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Base class for DTOs for Kubernetes Custom Resources with a "spec" and a "status" field.
    /// </summary>
    public class CustomResource<TSpec, TStatus> : CustomResource, IEquatable<CustomResource<TSpec, TStatus>>
    {
        public TSpec Spec { get; set; }
        public TStatus Status { get; set; }

        public CustomResource()
        {}

        public CustomResource(string @namespace, string name)
            : base(@namespace, name)
        {}

        public CustomResource(string @namespace, string name, TSpec spec)
            : base(@namespace, name)
        {
            Spec = spec;
        }

        public CustomResource(TSpec spec)
        {
            Spec = spec;
        }

        public bool Equals(CustomResource<TSpec, TStatus> other)
            => other != null
            && base.Equals(other)
            && Equals(Spec, other.Spec);

        public override bool Equals(object obj) => obj is CustomResource<TSpec, TStatus> other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Spec?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Base class for DTOs for Kubernetes Custom Resources with a "spec" and a "status" field.
    /// </summary>
    public class CustomResource<TSpec> : CustomResource<TSpec, StatusV1>
    {
        public CustomResource()
        {}

        public CustomResource(string @namespace, string name) : base(@namespace, name)
        {}

        public CustomResource(string @namespace, string name, TSpec spec) : base(@namespace, name, spec)
        {}

        public CustomResource(TSpec spec) : base(spec)
        {}
    }
}
