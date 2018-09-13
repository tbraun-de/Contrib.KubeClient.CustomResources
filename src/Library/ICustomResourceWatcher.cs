using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public interface ICustomResourceWatcher
    {
        /// <summary>
        /// Triggered whenever the connection to the KubeApi is closed.
        /// </summary>
        event EventHandler<Exception> ConnectionError;

        /// <summary>
        /// Triggered whenever the connection to the KubeApi is (re)established.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        /// Triggered whenever resources have changed.
        /// </summary>
        event EventHandler DataChanged;

        /// <summary>
        /// Indicates whether the resource watcher is watching or not.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Starts watching for CustomResources.
        /// </summary>
        void StartWatching();
    }

    [PublicAPI]
    public interface ICustomResourceWatcher<TResourceSpec> : ICustomResourceWatcher
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
    }
}
