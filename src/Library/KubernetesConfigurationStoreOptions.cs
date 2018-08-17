using System;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public class KubernetesConfigurationStoreOptions
    {
        /// <summary>
        /// The connection string which points to the Kubernetes cluster.
        /// If not set, the service assumes that it runs inside the kubernetes cluster and autoconfigures itself.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Configures the timeout for watching kubernetes event streams after the stream will be closed automatically.
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan WatchTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
