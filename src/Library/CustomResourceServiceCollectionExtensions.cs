using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using KubeClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Contrib.KubeClient.CustomResources
{
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public static class CustomResourceServiceCollectionExtensions
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
        /// Registers a <see cref="ICustomResourceWatcher{TResource}"/>, <see cref="ICustomResourceClient{TResource}"/> and a <see cref="CustomResourceDefinition{TResource}"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="crdApiVersion">The crd API Version (&lt;apiGroupName&gt;/&lt;version&gt;, e.g. 'your.company/v1').</param>
        /// <param name="crdPluralName">The plural name (see <code>spec.names.plural</code>) of the CRD.</param>
        public static IServiceCollection AddCustomResourceWatcher<TResource, TWatcher>(this IServiceCollection services, string crdApiVersion, string crdPluralName)
            where TWatcher : class, ICustomResourceWatcher<TResource>
            where TResource : CustomResource
        {
            services.TryAddSingleton<ICustomResourceClient<TResource>, CustomResourceClient<TResource>>();
            return services.AddKubernetesClient()
                           .AddSingleton(new CustomResourceDefinition<TResource>(crdApiVersion, crdPluralName))
                           .AddSingleton<TWatcher>()
                           .AddSingleton<ICustomResourceWatcher>(provider => provider.GetRequiredService<TWatcher>())
                           .AddSingleton<ICustomResourceWatcher<TResource>>(provider => provider.GetRequiredService<TWatcher>());
        }
    }
}
