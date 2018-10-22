using System;

namespace Contrib.KubeClient.CustomResources
{
    public class KubernetesConfigurationStoreOptions
    {
        /// <summary>
        /// Configures the timeout for watching kubernetes event streams after the stream will be closed automatically.
        /// Default: 5 minutes
        /// </summary>
        public TimeSpan WatchTimeout { get; set; } = TimeSpan.FromMinutes(5);
    }
}
