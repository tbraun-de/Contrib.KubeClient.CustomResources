using System;
using System.Collections.Generic;

namespace Contrib.KubeClient.CustomResources
{
    public interface ICustomResourceWatcher<TSpec>
    {
        /// <summary>
        /// Gets all <typeparamref name="TSpec"/>s which are currently active.
        /// </summary>
        IEnumerable<TSpec> Resources { get; }
        /// <summary>
        /// Gets all CustomResources which are currently active.
        /// </summary>
        IEnumerable<CustomResource<TSpec>> RawResources { get; }

        /// <summary>
        /// Starts watching for CustomResources.
        /// </summary>
        void StartWatching();

        /// <summary>
        /// Triggered whenever the connection to the KubeApi is closed.
        /// </summary>
        event EventHandler<Exception> OnConnectionError;

        /// <summary>
        /// Triggered whenever the connection to the KubeApi is (re)established.
        /// </summary>
        event EventHandler OnConnected;
    }
}
