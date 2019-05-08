using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using KubeClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Base class for DTOs for Kubernetes Custom Resources.
    /// </summary>
    [PublicAPI]
    public abstract class CustomResource : KubeResourceV1
    {
        [JsonIgnore, YamlIgnore]
        public CustomResourceDefinition Definition { get; }

        protected CustomResource(CustomResourceDefinition definition)
        {
            Definition = definition;
            ApiVersion = Definition.ApiVersion;
            Kind = Definition.Kind;
        }

        protected CustomResource(CustomResourceDefinition definition, string @namespace, string name)
            : this(definition)
        {
            Metadata = new ObjectMetaV1
            {
                Namespace = @namespace,
                Name = name
            };
        }

        public override bool Equals(object obj)
            => obj is CustomResource other
            && NameEquals(other);

        /// <summary>
        /// Returns <c>true</c> if the name and namespace of the other resource equal those of this resource.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool NameEquals(KubeResourceV1 other)
            => Metadata.Name == other.Metadata.Name
            && (Metadata.Namespace ?? "") == (other.Metadata.Namespace ?? "");

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Metadata?.Namespace?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (Metadata?.Name?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        private readonly List<ErrorContext> _serializationErrors = new List<ErrorContext>();

        /// <summary>
        /// Errors that occured during deserialization.
        /// </summary>
        /// <remarks>
        /// <see cref="ICustomResourceClient{TResource}.ListAsync"/> and <see cref="ICustomResourceClient{TResource}.Watch"/> will report deserialization errors here rather than throwing exceptions.
        /// This allows you handle individual defective resources without loosing access to all the rest.
        /// </remarks>
        [JsonIgnore]
        public IReadOnlyList<ErrorContext> SerializationErrors => _serializationErrors;

        [OnError]
        internal void OnError(StreamingContext context, ErrorContext errorContext)
        {
            // Invalid metadata may compromise the ability to determine a resource's identity
            if (errorContext.Path.StartsWith("metadata")) return;

            _serializationErrors.Add(errorContext);
            errorContext.Handled = true;
        }
    }

    /// <summary>
    /// Base class for DTOs for Kubernetes Custom Resources with a "spec" and a "status" field.
    /// </summary>
    public abstract class CustomResource<TSpec, TStatus> : CustomResource
    {
        [JsonProperty("spec"), YamlMember(Alias = "spec")]
        public TSpec Spec { get; set; }

        [JsonProperty("status"), YamlMember(Alias = "status")]
        public TStatus Status { get; set; }

        protected CustomResource(CustomResourceDefinition definition)
            : base(definition)
        {}

        protected CustomResource(CustomResourceDefinition definition, string @namespace, string name, TSpec spec)
            : base(definition, @namespace, name)
        {
            Spec = spec;
        }

        public override bool Equals(object obj)
            => obj is CustomResource<TSpec, TStatus> other
            && this.NameEquals(other)
            && SpecEquals(other.Spec);

        protected virtual bool SpecEquals(TSpec other)
            => Equals(Spec, other);

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
    public abstract class CustomResource<TSpec> : CustomResource<TSpec, StatusV1>
    {
        protected CustomResource(CustomResourceDefinition definition)
            : base(definition)
        {}

        protected CustomResource(CustomResourceDefinition definition, string @namespace, string name, TSpec spec)
            : base(definition, @namespace, name, spec)
        {}
    }
}
