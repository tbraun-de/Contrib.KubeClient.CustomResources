using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public interface ICustomResourceWatcher<TResourceSpec>
    {
        /// <summary>
        /// Gets all <typeparamref name="TResourceSpec"/>s which are currently active.
        /// </summary>
        IEnumerable<TResourceSpec> Resources { get; }

        /// <summary>
        /// Gets all CustomResources which are currently active.
        /// </summary>
        IEnumerable<CustomResource<TResourceSpec>> RawResources { get; }

        /// <summary>
        /// The used custom resource client.
        /// </summary>
        ICustomResourceClient<TResourceSpec> Client { get; }

        /// <summary>
        /// Starts watching for CustomResources.
        /// </summary>
        void StartWatching();

        /// <summary>
        /// Triggered whenever the connection to the KubeApi is closed.
        /// </summary>
        event EventHandler<Exception> ConnectionError;

        /// <summary>
        /// Triggered whenever the connection to the KubeApi is (re)established.
        /// </summary>
        event EventHandler Connected;
    }
}
