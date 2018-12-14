using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Contrib.KubeClient.CustomResources
{
    /// <summary>
    /// Provides an in-memory cache of a list of Kubernetes Custom Resources. Performs automatic thread-safe updating using the Kubernetes Watch API.
    /// </summary>
    public interface ICustomResourceWatcher : IHostedService, IDisposable
    {
        /// <summary>
        /// Triggered whenever resources have changed.
        /// </summary>
        event EventHandler DataChanged;
    }

    /// <summary>
    /// Provides an in-memory cache of a list of Kubernetes Custom Resources. Performs automatic thread-safe updating using the Kubernetes Watch API.
    /// </summary>
    /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
    public interface ICustomResourceWatcher<out TResource> : ICustomResourceWatcher, IEnumerable<TResource>
        where TResource : CustomResource
    {}
}
