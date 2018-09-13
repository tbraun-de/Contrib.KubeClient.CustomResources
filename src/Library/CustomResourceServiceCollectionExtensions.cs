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
        /// Registers a <see cref="ICustomResourceWatcher{TResourceSpec}"/>, <see cref="ICustomResourceClient{TResourceSpec}"/> and a <see cref="CustomResourceDefinition{TResourceSpec}"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="crdApiVersion">The crd API Version (&lt;apiGroupName&gt;/&lt;version&gt;, e.g. 'your.company/v1').</param>
        /// <param name="crdPluralName">The plural name (see <code>spec.names.plural</code>) of the CRD.</param>
        public static IServiceCollection AddCustomResourceWatcher<TResourceSpec, TWatcher>(this IServiceCollection services, string crdApiVersion, string crdPluralName)
            where TWatcher : class, ICustomResourceWatcher<TResourceSpec>
        {
            services.TryAddSingleton<ICustomResourceClient<TResourceSpec>, CustomResourceClient<TResourceSpec>>();
            return services.AddKubernetesClient()
                           .AddSingleton(new CustomResourceDefinition<TResourceSpec>(crdApiVersion, crdPluralName))
                           .AddSingleton<ICustomResourceWatcher<TResourceSpec>, TWatcher>()
                           .AddSingleton<ICustomResourceWatcher, TWatcher>();
        }
    }
}
