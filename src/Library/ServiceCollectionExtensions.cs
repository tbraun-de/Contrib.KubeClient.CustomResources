using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ICustomResourceClient{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
        /// <param name="services">The service collection.</param>
        public static IServiceCollection AddCustomResourceClient<TResource>(this IServiceCollection services)
            where TResource : CustomResource, new()
            => services.AddSingleton<ICustomResourceClient<TResource>, CustomResourceClient<TResource>>();

        /// <summary>
        /// Registers an <see cref="ICustomResourceClient{TResource}"/> and an <see cref="ICustomResourceWatcher{TResource}"/>.
        /// </summary>
        /// <typeparam name="TResource">The Kubernetes Custom Resource DTO type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="namespace">The namespace to watch; leave unset for all.</param>
        public static IServiceCollection AddCustomResourceWatcher<TResource>(this IServiceCollection services, string @namespace = null)
            where TResource : CustomResource, new()
            => services.AddCustomResourceClient<TResource>()
                       .AddSingleton(new CustomResourceNamespace<TResource>(@namespace))
                       .AddSingleton<ICustomResourceWatcher<TResource>, CustomResourceWatcher<TResource>>()
                       .AddSingleton<ICustomResourceWatcher>(provider => provider.GetRequiredService<ICustomResourceWatcher<TResource>>())
                       .AddSingleton<IHostedService>(provider => provider.GetRequiredService<ICustomResourceWatcher<TResource>>());
    }
}
