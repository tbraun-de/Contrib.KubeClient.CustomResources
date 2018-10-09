using System;
using JetBrains.Annotations;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="IKubeApiClient"/>.
        /// </summary>
        public static IServiceCollection AddKubernetesClient(this IServiceCollection services)
        {
            services.TryAddSingleton<KubeApiClientFactory>();
            services.TryAddSingleton(provider => provider.GetRequiredService<KubeApiClientFactory>().Build());
            return services;
        }

        /// <summary>
        /// Registers an <see cref="ICustomResourceClient{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="definition">The CRD API Version and plural name.</param>
        public static IServiceCollection AddCustomResourceClient<TResource>(this IServiceCollection services, CustomResourceDefinition<TResource> definition)
            where TResource : CustomResource
            => services.AddKubernetesClient()
                       .AddSingleton<ICustomResourceClient<TResource>, CustomResourceClient<TResource>>()
                       .AddSingleton(definition);

        /// <summary>
        /// Registers an <see cref="ICustomResourceClient{TResource}"/> and an <see cref="ICustomResourceWatcher{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="definition">The CRD API Version and plural name.</param>
        /// <param name="namespace">The namespace to watch; leave unset for all.</param>
        public static IServiceCollection AddCustomResourceWatcher<TResource>(this IServiceCollection services, CustomResourceDefinition<TResource> definition, string @namespace = null)
            where TResource : CustomResource
            => services.AddCustomResourceClient(definition)
                       .AddSingleton(new CustomResourceNamespace<TResource>(@namespace))
                       .AddSingleton<ICustomResourceWatcher<TResource>, CustomResourceWatcher<TResource>>()
                       .AddSingleton<ICustomResourceWatcher>(provider => provider.GetRequiredService<ICustomResourceWatcher<TResource>>());

        /// <summary>
        /// Starts watching for changes in an <see cref="ICustomResourceWatcher"/> registered with <see cref="AddCustomResourceWatcher{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
        public static IServiceProvider UseCustomResourceWatcher<TResource>(this IServiceProvider provider)
            where TResource : CustomResource
        {
            provider.GetRequiredService<ICustomResourceWatcher<TResource>>().StartWatching();
            return provider;
        }

        /// <summary>
        /// Instructs all resource watchers to start.
        /// </summary>
        public static IServiceProvider UseCustomResourceWatchers(this IServiceProvider provider)
        {
            foreach (ICustomResourceWatcher watcher in provider.GetServices<ICustomResourceWatcher>())
                watcher.StartWatching();

            return provider;
        }
    }
}
