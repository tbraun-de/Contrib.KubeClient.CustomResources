using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using KubeClient.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class DependencyInjectionExtensionsFacts
    {
        private readonly ServiceProvider _provider;

        public DependencyInjectionExtensionsFacts()
        {
            _provider = new ServiceCollection()
                       .AddLogging(builder => builder.AddConsole())
                       .AddOptions()
                       .AddKubeClient(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {["ApiEndPoint"] = "http://example.com/"}).Build())
                       .AddSingleton(CreateResourceClient<int>())
                       .AddSingleton(CreateResourceClient<string>())
                       .AddCustomResourceWatcher(new CustomResourceDefinition<CustomResource<string>>(apiVersion: "foo/v1", pluralName: "strings"))
                       .AddCustomResourceWatcher(new CustomResourceDefinition<CustomResource<int>>(apiVersion: "foo/v1", pluralName: "ints"))
                       .BuildServiceProvider();
        }

        [Fact]
        public void StartsAllWatchers()
        {
            _provider.UseCustomResourceWatchers();

            _provider.GetServices<ICustomResourceWatcher>()
                     .Select(watcher => watcher.IsActive)
                     .Should()
                     .AllBeEquivalentTo(true);
        }

        [Fact]
        public void StartsOnlyRequestedWatcher()
        {
            _provider.UseCustomResourceWatcher<CustomResource<string>>();

            _provider.GetServices<ICustomResourceWatcher>()
                     .Should()
                     .ContainSingle(watcher => watcher.IsActive);
        }

        private static ICustomResourceClient<CustomResource<TResourceSpec>> CreateResourceClient<TResourceSpec>()
        {
            var clientMock = new Mock<ICustomResourceClient<CustomResource<TResourceSpec>>>();
            clientMock.Setup(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>())).Returns(new Subject<IResourceEventV1<CustomResource<TResourceSpec>>>());
            return clientMock.Object;
        }
    }
}
