using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceApplicationBuilderExtensionsFacts
    {
        private readonly ApplicationBuilder _applicationBuilder;
        private readonly ServiceProvider _serviceProvider;

        public CustomResourceApplicationBuilderExtensionsFacts()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConsole());
            serviceCollection.AddOptions();
            serviceCollection.Configure<KubernetesConfigurationStoreOptions>(opt => opt.ConnectionString = "https://nowhere");
            serviceCollection.AddCustomResourceWatcher<string, TestResourceWatcher<string>>(crdApiVersion: "foo/v1", crdPluralName: "strings");
            serviceCollection.AddCustomResourceWatcher<int, TestResourceWatcher<int>>(crdApiVersion: "foo/v1", crdPluralName: "integers");
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _applicationBuilder = new ApplicationBuilder(_serviceProvider);
        }

        [Fact]
        public void StartsAllWatchers()
        {
            _applicationBuilder.UseCustomResourceWatchers();

            _serviceProvider.GetServices<ICustomResourceWatcher>()
                            .Select(watcher => watcher.IsActive)
                            .Should()
                            .AllBeEquivalentTo(true);
        }

        [Fact]
        public void StartsOnlyRequestedWatcher()
        {
            _applicationBuilder.UseCustomResourceWatcher<int>();

            _serviceProvider.GetServices<ICustomResourceWatcher>()
                            .Should()
                            .ContainSingle(watcher => watcher.IsActive);
        }

        public class TestResourceWatcher<T> : CustomResourceWatcher<T>
        {
            public TestResourceWatcher(ILogger<TestResourceWatcher<T>> logger, ICustomResourceClient<T> client, CustomResourceDefinition<T> crd)
                : base(logger, client, crd, @namespace: "")
            {}
        }
    }
}
