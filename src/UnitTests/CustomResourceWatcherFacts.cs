using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceWatcherFacts : IDisposable
    {
        private const string TestNamespace = "test-namespace";
        private const string TestResourceVersion = "5";
        private readonly CustomResourceWatcher<Mock1Resource> _watcher;

        private readonly List<Mock1Resource> _items;
        private readonly Mock<ICustomResourceClient<Mock1Resource>> _clientMock = new Mock<ICustomResourceClient<Mock1Resource>>();
        private readonly Subject<IResourceEventV1<Mock1Resource>> _events = new Subject<IResourceEventV1<Mock1Resource>>();

        public CustomResourceWatcherFacts()
        {
            var list = new CustomResourceList<Mock1Resource>
            {
                Metadata = new ListMetaV1 {ResourceVersion = TestResourceVersion}
            };
            _items = list.Items;

            _clientMock.Setup(x => x.ListAsync(null, TestNamespace, CancellationToken.None))
                      .ReturnsAsync(list);
            _clientMock.SetupSequence(x => x.Watch(TestNamespace, TestResourceVersion))
                      .Returns(_events)
                      .Returns(new Subject<IResourceEventV1<Mock1Resource>>());

            _watcher = new CustomResourceWatcher<Mock1Resource>(
                new LoggerFactory().CreateLogger<CustomResourceWatcher<Mock1Resource>>(),
                _clientMock.Object,
                new CustomResourceNamespace<Mock1Resource>(TestNamespace));
        }

        public void Dispose() => _watcher.Dispose();

        [Fact]
        public async Task InitialListGetsAddedToCache()
        {
            var resource1 = new Mock1Resource(TestNamespace, "1");
            var resource2 = new Mock1Resource(TestNamespace, "2");

            _items.Add(resource1);
            _items.Add(resource2);
            await _watcher.StartAsync();

            _watcher.Should().BeEquivalentTo(resource1, resource2);
        }

        [Fact]
        public async Task AddedResourceGetsAddedToCache()
        {
            var resource1 = new Mock1Resource(TestNamespace, "1");
            var resource2 = new Mock1Resource(TestNamespace, "2");

            _items.Add(resource1);
            await _watcher.StartAsync();
            _events.OnNext(Added(resource2));

            _watcher.Should().BeEquivalentTo(resource1, resource2);
        }

        [Fact]
        public async Task ModifiedResourceGetsUpdatedInCache()
        {
            var resource1A = new Mock1Resource(TestNamespace, "1") {Spec = "a"};
            var resource1B = new Mock1Resource(TestNamespace, "1") {Spec = "b"};

            _items.Add(resource1A);
            await _watcher.StartAsync();
            _events.OnNext(Modified(resource1B));

            _watcher.Should().BeEquivalentTo(resource1B);
        }

        [Fact]
        public async Task DeletedResourceGetsRemovedFromCache()
        {
            var resource1 = new Mock1Resource(TestNamespace, "1");
            var resource2 = new Mock1Resource(TestNamespace, "2");

            _items.Add(resource1);
            _items.Add(resource2);
            await _watcher.StartAsync();
            _events.OnNext(Deleted(resource2));

            _watcher.Should().BeEquivalentTo(resource1);
        }

        [Fact]
        public async Task RaisesDataChangedEvent()
        {
            int triggerCounter = 0;
            _watcher.DataChanged += delegate { triggerCounter++; };

            _items.Add(new Mock1Resource(TestNamespace, "1"));
            _items.Add(new Mock1Resource(TestNamespace, "2"));
            await _watcher.StartAsync();
            triggerCounter.Should().Be(1);

            _events.OnNext(Modified(new Mock1Resource(TestNamespace, "1")));
            _events.OnNext(Modified(new Mock1Resource(TestNamespace, "2")));
            triggerCounter.Should().Be(3);
        }

        [Fact]
        public async Task ResubscribesOnCompletion()
        {
            await _watcher.StartAsync();
            _events.OnCompleted();

            _clientMock.Verify(x => x.ListAsync(null, TestNamespace, CancellationToken.None), Times.Exactly(2));
            _clientMock.Verify(x => x.Watch(TestNamespace, TestResourceVersion), Times.Exactly(2));
        }

        [Fact]
        public async Task ResubscribesOnError()
        {
            await _watcher.StartAsync();
            _events.OnError(new Exception());

            _clientMock.Verify(x => x.ListAsync(null, TestNamespace, CancellationToken.None), Times.Exactly(2));
            _clientMock.Verify(x => x.Watch(TestNamespace, TestResourceVersion), Times.Exactly(2));
        }

        [Fact]
        public async Task DoesNotResubscribeAfterStop()
        {
            await _watcher.StartAsync();
            await _watcher.StopAsync();
            _events.OnCompleted();

            _clientMock.Verify(x => x.ListAsync(null, TestNamespace, CancellationToken.None), Times.Once);
            _clientMock.Verify(x => x.Watch(TestNamespace, TestResourceVersion), Times.Once);
        }

        private static IResourceEventV1<Mock1Resource> Added(Mock1Resource resource)
            => new ResourceEventV1<Mock1Resource> {EventType = ResourceEventType.Added, Resource = resource};

        private static IResourceEventV1<Mock1Resource> Modified(Mock1Resource resource)
            => new ResourceEventV1<Mock1Resource> {EventType = ResourceEventType.Modified, Resource = resource};

        private static IResourceEventV1<Mock1Resource> Deleted(Mock1Resource resource)
            => new ResourceEventV1<Mock1Resource> {EventType = ResourceEventType.Deleted, Resource = resource};
    }
}
