using Microsoft.Extensions.DependencyInjection;

namespace Contrib.KubeClient.CustomResources
{
    public static class CustomResourcesServiceCollectionExtensions
    {
        public static IServiceCollection AddKubernetesClient(this IServiceCollection services)
            => services.AddSingleton<KubeApiClientFactory>()
                       .AddSingleton(provider => provider.GetRequiredService<KubeApiClientFactory>().Build())
                       .AddSingleton<ICustomResourceClient, CustomResourceClient>();

    }
}
