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
    public class CustomResourceStoreFacts
    {
        private readonly Mock<ICustomResourceWatcher<string>> _watcherMock;
        private readonly CustomResourceStore<string> _store;
        private readonly Mock<ICustomResourceClient<string>> _customResourceClientMock;

        public CustomResourceStoreFacts()
        {
            _customResourceClientMock = new Mock<ICustomResourceClient<string>>();
            _customResourceClientMock.Setup(mock => mock.CreateAsync(It.IsAny<CustomResource<string>>(), It.IsAny<CancellationToken>()))
                                    .Returns<CustomResource<string>, CancellationToken>((resource, _) =>
                                     {
                                         resource.Metadata.Uid = Guid.NewGuid().ToString("N");
                                         return Task.FromResult(resource);
                                     });
            _customResourceClientMock.Setup(mock => mock.UpdateAsync(It.IsAny<CustomResource<string>>(), It.IsAny<CancellationToken>()))
                                    .Returns<CustomResource<string>, CancellationToken>((resource, _) => Task.FromResult(resource));
            _customResourceClientMock.Setup(mock => mock.DeleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .Returns<string, string, CancellationToken>((name, @namespace, _) => Task.FromResult(CustomResourceFactory.Create("bla", name, @namespace)));

            _watcherMock = new Mock<ICustomResourceWatcher<string>>();
            _watcherMock.SetupGet(mock => mock.Client).Returns(_customResourceClientMock.Object);

            _store = new CustomResourceStore<string>(_watcherMock.Object);
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

            var resourceFound = await _store.FindByNameAsync("123");

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

            var resourcesFound = await _store.FindByNamespaceAsync("123");

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

            var resourcesFound = await _store.FindAsync(r => r.Spec == "test12134");

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

            var resourcesFound = await _store.FindAsync(r => r.Spec == "not_in_there");

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
            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var store = new CustomResourceReadonlyStore<string>(watcher: watcherMock.Object);

            Func<Task> find = async () => await store.FindByNameAsync("not_in_there");
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

            var resourcesFound = await _store.FindAllAsync();

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

            long actualCount = await _store.CountAsync();

            actualCount.Should().Be(resources.Count);
        }

        [Fact]
        public async Task AddsCustomResource()
        {
            var resource = CustomResourceFactory.Create(spec: "test123", @namespace: "123");

            var createdResource = await _store.AddAsync(resource);

            createdResource.Metadata.Uid.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task UpdatesCustomResource()
        {
            var resource = CustomResourceFactory.Create(spec: "test123", @namespace: "123");

            await _store.UpdateAsync(resource);

            _customResourceClientMock.Verify(mock => mock.UpdateAsync(resource, It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task DeletesCustomResource()
        {
            var resource = CustomResourceFactory.Create(spec: "test123", @namespace: "resourceNamespace", name: "resourceName");

            await _store.DeleteAsync(resource.Metadata.Name, resource.Metadata.Namespace);

            _customResourceClientMock.Verify(mock => mock.DeleteAsync(resource.Metadata.Name, resource.Metadata.Namespace, It.IsAny<CancellationToken>()));
        }
    }
}
