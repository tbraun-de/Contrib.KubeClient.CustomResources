using System.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using KubeClient.Models;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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
            serviceCollection.AddSingleton(CreateResourceClient<int>());
            serviceCollection.AddSingleton(CreateResourceClient<string>());
            serviceCollection.AddCustomResourceWatcher<CustomResource<string>, TestResourceWatcher<string>>(crdApiVersion: "foo/v1", crdPluralName: "strings");
            serviceCollection.AddCustomResourceWatcher<CustomResource<int>, TestResourceWatcher<int>>(crdApiVersion: "foo/v1", crdPluralName: "integers");
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
            _applicationBuilder.UseCustomResourceWatcher<TestResourceWatcher<string>>();

            _serviceProvider.GetServices<TestResourceWatcher<string>>()
                            .Should()
                            .ContainSingle(watcher => watcher.IsActive);
        }

        private static ICustomResourceClient<CustomResource<TResourceSpec>> CreateResourceClient<TResourceSpec>()
        {
            var clientMock = new Mock<ICustomResourceClient<CustomResource<TResourceSpec>>>();
            clientMock.Setup(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>())).Returns(new Subject<IResourceEventV1<CustomResource<TResourceSpec>>>());
            return clientMock.Object;
        }

        public class TestResourceWatcher<T> : CustomResourceWatcher<CustomResource<T>>
        {
            public TestResourceWatcher(ILogger<TestResourceWatcher<T>> logger, ICustomResourceClient<CustomResource<T>> client, CustomResourceDefinition<CustomResource<T>> crd)
                : base(logger, client, crd)
            {}
        }
    }
}
