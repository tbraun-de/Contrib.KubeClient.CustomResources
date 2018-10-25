using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Watches Kubernetes Custom Resources for changes.
    /// </summary>
    public interface ICustomResourceWatcher : IHostedService
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
    }

    /// <summary>
    /// Watches Kubernetes Custom Resources of a specific type for changes and keeps an in-memory representation.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public interface ICustomResourceWatcher<TResource> : ICustomResourceWatcher
        where TResource : CustomResource
    {
        /// <summary>
        /// Gets all CustomResources which are currently active.
        /// </summary>
        IEnumerable<TResource> RawResources { get; }

        /// <summary>
        /// The used custom resource client.
        /// </summary>
        ICustomResourceClient<TResource> Client { get; }
    }
}
