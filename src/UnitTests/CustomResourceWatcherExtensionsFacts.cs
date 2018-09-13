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
        private readonly Mock<ICustomResourceWatcher<string>> _watcherMock;
        private readonly ICustomResourceWatcher<string> _watcher;

        public CustomResourceWatcherExtensionsFacts()
        {
            var customResourceClientMock = new Mock<ICustomResourceClient<string>>();
            customResourceClientMock.Setup(mock => mock.CreateAsync(It.IsAny<CustomResource<string>>(), It.IsAny<CancellationToken>()))
                                    .Returns<CustomResource<string>, CancellationToken>((resource, _) =>
                                     {
                                         resource.Metadata.Uid = Guid.NewGuid().ToString("N");
                                         return Task.FromResult(resource);
                                     });
            customResourceClientMock.Setup(mock => mock.UpdateAsync(It.IsAny<CustomResource<string>>(), It.IsAny<CancellationToken>()))
                                    .Returns<CustomResource<string>, CancellationToken>((resource, _) => Task.FromResult(resource));
            customResourceClientMock.Setup(mock => mock.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .Returns<string, string, CancellationToken>((name, @namespace, _) => Task.FromResult(CustomResourceFactory.Create("bla", name, @namespace)));

            _watcherMock = new Mock<ICustomResourceWatcher<string>>();
            _watcherMock.SetupGet(mock => mock.Client).Returns(customResourceClientMock.Object);
            _watcher = _watcherMock.Object;
        }

        [Fact]
        public async Task FindsOneByName()
        {
            IEnumerable<CustomResource<string>> resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", name: "123"),
                CustomResourceFactory.Create(spec: "test123", name: "234"),
                CustomResourceFactory.Create(spec: "test123", name: "345"),
                CustomResourceFactory.Create(spec: "test123", name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourceFound = await _watcher.FindByNameAsync("123");

            resourceFound.Should().Be(resources.Single(x => x.Metadata.Name == "123"));
        }

        [Fact]
        public async Task FindsByNamespace()
        {
            var expectedResources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", @namespace: "123"),
                CustomResourceFactory.Create(spec: "test12123", @namespace: "123")
            };
            List<CustomResource<string>> resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", @namespace: "234"),
                CustomResourceFactory.Create(spec: "test123", @namespace: "345"),
                CustomResourceFactory.Create(spec: "test123", @namespace: "456")
            };
            resources.AddRange(expectedResources);
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindByNamespaceAsync("123");

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task FindsByQuery()
        {
            var expectedResources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test12134", name: "745"),
                CustomResourceFactory.Create(spec: "test12134", name: "234")
            };
            List<CustomResource<string>> resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", name: "123"),
                CustomResourceFactory.Create(spec: "test123", name: "345"),
                CustomResourceFactory.Create(spec: "test123", name: "456")
            };
            resources.AddRange(expectedResources);
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindAsync(r => r.Spec == "test12134");

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task ReturnsEmptyEnumerableIfNothingFound()
        {
            IEnumerable<CustomResource<string>> resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", name: "123"),
                CustomResourceFactory.Create(spec: "test12134", name: "234"),
                CustomResourceFactory.Create(spec: "test123", name: "345"),
                CustomResourceFactory.Create(spec: "test123", name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var resourcesFound = await _watcher.FindAsync(r => r.Spec == "not_in_there");

            resourcesFound.Should().BeEmpty();
        }

        [Fact]
        public void ThrowsIfNameCouldNotBeFound()
        {
            IEnumerable<CustomResource<string>> resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test123", name: "123"),
                CustomResourceFactory.Create(spec: "test12134", name: "234"),
                CustomResourceFactory.Create(spec: "test123", name: "345"),
                CustomResourceFactory.Create(spec: "test123", name: "456")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            Func<Task> find = async () => await _watcher.FindByNameAsync("not_in_there");
            find.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public async Task GettingAllResourcesReturnsAll()
        {
            var expectedResources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test12134", name: "745"),
                CustomResourceFactory.Create(spec: "test12134", name: "234")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(expectedResources);

            var resourcesFound = await _watcher.FindAllAsync();

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }

        [Fact]
        public async Task ReturnsTheCorrectCountOfResources()
        {
            var resources = new List<CustomResource<string>>
            {
                CustomResourceFactory.Create(spec: "test12134", name: "745"),
                CustomResourceFactory.Create(spec: "test12134", name: "234")
            };
            _watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            long actualCount = await _watcher.CountAsync();

            actualCount.Should().Be(resources.Count);
        }
    }
}
