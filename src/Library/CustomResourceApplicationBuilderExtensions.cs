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
        /// Starts the <see cref="ICustomResourceWatcher"/> <typeparam name="TWatcher"/>.
        /// </summary>
        public static IApplicationBuilder UseCustomResourceWatcher<TWatcher>(this IApplicationBuilder appBuilder)
            where TWatcher : ICustomResourceWatcher
        {
            appBuilder.ApplicationServices.GetRequiredService<TWatcher>().StartWatching();
            return appBuilder;
        }
    }
}
