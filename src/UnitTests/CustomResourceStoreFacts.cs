using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceStoreFacts
    {
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
            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

            var resourceFound = await store.FindByNameAsync("123");

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

            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

            var resourcesFound = await store.FindByNamespaceAsync("123");

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

            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

            var resourcesFound = await store.FindAsync(r => r.Spec == "test12134");

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
            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(resources);

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

            var resourcesFound = await store.FindAsync(r => r.Spec == "not_in_there");

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

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

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

            var watcherMock = new Mock<ICustomResourceWatcher<string>>();
            watcherMock.SetupGet(expression: mock => mock.RawResources).Returns(expectedResources);

            var store = new CustomResourceStore<string>(watcher: watcherMock.Object);

            var resourcesFound = await store.GetAllAsync();

            resourcesFound.Should().BeEquivalentTo(expectedResources);
        }
    }
}
