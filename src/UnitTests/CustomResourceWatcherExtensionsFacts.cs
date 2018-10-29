using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceWatcherExtensionsFacts
    {
        private readonly Mock<ICustomResourceWatcher<Mock1Resource>> _watcherMock;
        private readonly ICustomResourceWatcher<Mock1Resource> _watcher;

        public CustomResourceWatcherExtensionsFacts()
        {
            var customResourceClientMock = new Mock<ICustomResourceClient<Mock1Resource>>();
            customResourceClientMock.Setup(mock => mock.CreateAsync(It.IsAny<Mock1Resource>(), It.IsAny<CancellationToken>()))
                                    .Returns<Mock1Resource, CancellationToken>((resource, _) =>
                                     {
                                         resource.Metadata.Uid = Guid.NewGuid().ToString("N");
                                         return Task.FromResult(resource);
                                     });
            customResourceClientMock.Setup(mock => mock.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .Returns<string, string, CancellationToken>((name, @namespace, _) => Task.FromResult(new Mock1Resource(name: name, @namespace: @namespace)));

            _watcherMock = new Mock<ICustomResourceWatcher<Mock1Resource>>();
            _watcherMock.SetupGet(mock => mock.Client).Returns(customResourceClientMock.Object);
            _watcher = _watcherMock.Object;
        }

        [Fact]
        public async Task FindsOneByName()
        {
            IEnumerable<Mock1Resource> resources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "123"),
                new Mock1Resource(name: "234"),
                new Mock1Resource(name: "345"),
                new Mock1Resource(name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourceFound = await _watcher.FindByNameAsync("123");

            resourceFound.Should().Be(resources.Single(x => x.Metadata.Name == "123"));
        }

        [Fact]
        public async Task FindsByNamespace()
        {
            var expectedResources = new List<Mock1Resource>
            {
                new Mock1Resource(@namespace: "123"),
                new Mock1Resource(@namespace: "123")
            };
            var resources = new List<Mock1Resource>
            {
                new Mock1Resource(@namespace: "234"),
                new Mock1Resource(@namespace: "345"),
                new Mock1Resource(@namespace: "456")
            };
            resources.AddRange(expectedResources);
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindByNamespaceAsync("123");

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task FindsByQuery()
        {
            var expectedResources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "745", spec: "test12134"),
                new Mock1Resource(name: "234", spec: "test12134")
            };
            var resources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "123", spec: "test123"),
                new Mock1Resource(name: "345", spec: "test123"),
                new Mock1Resource(name: "456", spec: "test123")
            };
            resources.AddRange(expectedResources);
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindAsync(r => r.Spec == "test12134");

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task ReturnsEmptyEnumerableIfNothingFound()
        {
            IEnumerable<Mock1Resource> resources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "123"),
                new Mock1Resource(name: "234"),
                new Mock1Resource(name: "345"),
                new Mock1Resource(name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindAsync(r => r.Spec == "not_in_there");

            resourcesFound.Should().BeEmpty();
        }

        [Fact]
        public void ThrowsIfNameCouldNotBeFound()
        {
            IEnumerable<Mock1Resource> resources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "123"),
                new Mock1Resource(name: "234"),
                new Mock1Resource(name: "345"),
                new Mock1Resource(name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            Func<Task> find = async () => await _watcher.FindByNameAsync("not_in_there");
            find.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public async Task GettingAllResourcesReturnsAll()
        {
            var expectedResources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "745"),
                new Mock1Resource(name: "234")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(expectedResources);

            var resourcesFound = await _watcher.FindAllAsync();

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task ReturnsTheCorrectCountOfResources()
        {
            var resources = new List<Mock1Resource>
            {
                new Mock1Resource(name: "745"),
                new Mock1Resource(name: "234")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            long actualCount = await _watcher.CountAsync();

            actualCount.Should().Be(resources.Count);
        }
    }
}
