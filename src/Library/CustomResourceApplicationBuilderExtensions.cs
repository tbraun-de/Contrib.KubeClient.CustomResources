using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Contrib.KubeClient.CustomResources
{
    [PublicAPI]
    public static class CustomResourceApplicationBuilderExtensions
    {
        /// <summary>
        /// Instructs all resource watchers to start.
        /// </summary>
        public static IApplicationBuilder UseCustomResourceWatchers(this IApplicationBuilder appBuilder)
        {
            var watchers = appBuilder.ApplicationServices.GetServices<ICustomResourceWatcher>();
            foreach (ICustomResourceWatcher watcher in watchers)
            {
                watcher.StartWatching();
            }

            return appBuilder;
        }

        /// <summary>
        /// Starts <see cref="ICustomResourceWatcher{TResourceSpec}"/>.
        /// </summary>
        public static IApplicationBuilder UseCustomResourceWatcher<TResourceSpec>(this IApplicationBuilder appBuilder)
        {
            appBuilder.ApplicationServices.GetRequiredService<ICustomResourceWatcher<TResourceSpec>>().StartWatching();
            return appBuilder;
        }
    }
}
