using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class CustomResourcesServiceCollectionExtensions
    {
        public static IServiceCollection AddKubernetesClient(this IServiceCollection services)
            => services.AddSingleton<KubeApiClientFactory>()
                       .AddSingleton(provider => provider.GetRequiredService<KubeApiClientFactory>().Build())
                       .AddSingleton<ICustomResourceClient, CustomResourceClient>();

    }
}
