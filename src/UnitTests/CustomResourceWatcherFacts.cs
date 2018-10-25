using System;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
using System.Threading;
using FluentAssertions;
using HTTPlease;
using IdentityServer4.Models;
using KubeClient.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceWatcherFacts
    {
        private readonly TestResourceWatcher _watcher;
        private readonly Subject<IResourceEventV1<CustomResource<Client>>> _resourceSubject;
        private readonly Mock<ICustomResourceClient<CustomResource<Client>>> _resourceClientMock;

        public CustomResourceWatcherFacts()
        {
            _resourceSubject = new Subject<IResourceEventV1<CustomResource<Client>>>();
            _resourceClientMock = new Mock<ICustomResourceClient<CustomResource<Client>>>();
            _resourceClientMock.SetupSequence(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>()))
                               .Returns(_resourceSubject)
                               .Returns(new Subject<IResourceEventV1<CustomResource<Client>>>());
            _watcher = new TestResourceWatcher(_resourceClientMock.Object);
            _watcher.Start();
        }

        [Fact]
        public void AddedResourceGetsAddedToCache()
        {
            var expectedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "1");

            _resourceSubject.OnNext(expectedResource);

            _watcher.RawResources.Should().Contain(expectedResource.Resource);
        }

        [Fact]
        public void ModifiedResourceGetsUpdatedInCache()
        {
            var addedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "4711");
            var modifiedResource = CreateResourceEvent(ResourceEventType.Modified, "expectedClientId", resourceVersion: "4712");
            modifiedResource.Resource.Spec.ClientName = "clientName";
            _resourceSubject.OnNext(addedResource);

            _resourceSubject.OnNext(modifiedResource);

            _watcher.RawResources.Should().Contain(modifiedResource.Resource);
        }

        [Fact]
        public void DeletedResourceGetsRemovedFromCache()
        {
            var watcherResources = _watcher.RawResources;

            var addedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "1");
            var removedResource = CreateResourceEvent(ResourceEventType.Deleted, "expectedClientId", resourceVersion: "2");
            _resourceSubject.OnNext(addedResource);
            _resourceSubject.OnNext(removedResource);

            watcherResources.Should().BeEmpty();
        }

        [Fact]
        public void RaisesConnectedEvent()
        {
            _watcher.ConnectedTriggered.Should().BeTrue();
        }

        [Fact]
        public void RaisesConnectionErrorEvent()
        {
            _watcher.Start();

            _resourceSubject.OnError(new Exception());

            _watcher.ConnectionErrorTriggered.Should().BeTrue();
        }

        [Fact]
        public void RaisesDataChangedEvent()
        {
            _watcher.Start();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "1", resourceVersion: "1"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "1", resourceVersion: "2"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Deleted, uid: "1", resourceVersion: "3"));

            _watcher.DataChangedTriggeredCount.Should().Be(3);
        }

        [Fact]
        public void RaisesDataChangedEventOnCacheInvalidation()
        {
            _watcher.Start();

            _resourceSubject.OnError(new HttpRequestException<StatusV1>(HttpStatusCode.Gone, new StatusV1()));

            _watcher.DataChangedTriggered.Should().BeTrue();
        }

        [Fact]
        public void DoesNotRaiseDataChangedEventWhenNothingChanged()
        {
            _watcher.Start();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "1", resourceVersion: "1"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "1", resourceVersion: "2"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "1", resourceVersion: "2"));

            _watcher.DataChangedTriggeredCount.Should().Be(2);
        }

        [Fact]
        public void ResubscribesOnError()
        {
            _watcher.Start();

            _resourceSubject.OnError(new Exception());

            _resourceClientMock.Verify(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void ResubscribesOnCompletion()
        {
            _watcher.Start();

            _resourceSubject.OnCompleted();

            _resourceClientMock.Verify(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void DoesNotResubscribeAfterStop()
        {
            _watcher.Start();
            _watcher.Stop();
            _resourceSubject.OnCompleted();

            _resourceClientMock.Verify(mock => mock.Watch(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Fact]
        public void PassesLastResourceVersionOnReconnect()
        {
            _watcher.Start();
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "4711", resourceVersion: "35"));

            _resourceSubject.OnError(new Exception());

            _resourceClientMock.Verify(mock => mock.Watch(It.IsAny<string>(), "35"));
        }

        [Fact]
        public void DropsCacheWhenResourceIsGone()
        {
            _watcher.Start();
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "4711", resourceVersion: "35"));

            _resourceSubject.OnError(new HttpRequestException<StatusV1>(HttpStatusCode.Gone, new StatusV1()));

            _watcher.RawResources.Should().BeEmpty();
        }

        [Fact]
        public void DropsResourceEventIfOlderThanLastKnown()
        {
            _watcher.Start();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "resource", resourceVersion: "10"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "resource", resourceVersion: "12"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "resource", resourceVersion: "11"));

            _watcher.RawResources.First().Metadata.ResourceVersion.Should().Be("12");
        }

        [Fact]
        public void DoesNotDropResourceEventIfVersionIsSmallerButDifferentResource()
        {
            _watcher.Start();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, uid: "resource", resourceVersion: "10"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "resource", resourceVersion: "12"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, uid: "anotherResource", resourceVersion: "9"));

            _watcher.RawResources.Single(r => r.Metadata.Uid == "resource").Metadata.ResourceVersion.Should().Be("12");
            _watcher.RawResources.Single(r => r.Metadata.Uid == "anotherResource").Metadata.ResourceVersion.Should().Be("9");
        }

        [Fact]
        public void ReceivingErroneousResourceEventDoesNotThrow()
        {
            _watcher.Start();

            Action receivingErroneousResourceEvent = () => _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Error, uid: "resource", resourceVersion: "1"));

            receivingErroneousResourceEvent.Should().NotThrow();
        }

        [Fact]
        public void ReceivingUnknownResourceEventTypeThrowsArgumentOutOfRangeException()
        {
            _watcher.Start();

            Action receivingUnknownResourceEventType = () => _resourceSubject.OnNext(CreateResourceEvent((ResourceEventType)(-1), uid: "resource", resourceVersion: "1"));

            receivingUnknownResourceEventType.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static ResourceEventV1<CustomResource<Client>> CreateResourceEvent(ResourceEventType eventType, string uid, string resourceVersion)
            => new ResourceEventV1<CustomResource<Client>>
            {
                EventType = eventType,
                Resource = new CustomResource<Client>
                {
                    Metadata = new ObjectMetaV1
                    {
                        Namespace = "namespace",
                        Name = uid,
                        ResourceVersion = resourceVersion,
                        Uid = uid
                    },
                    Spec = new Client {ClientId = uid}
                }
            };

        private class TestResourceWatcher : CustomResourceWatcher<CustomResource<Client>>
        {
            public TestResourceWatcher(ICustomResourceClient<CustomResource<Client>> client)
                : base(new Logger<CustomResourceWatcher<CustomResource<Client>>>(new LoggerFactory()), client, new CustomResourceDefinition<CustomResource<Client>>(apiVersion: "stable.contrib.identityserver.io/v1", pluralName: "identityclients"), new CustomResourceNamespace<CustomResource<Client>>(""))
            {
                Connected += (sender, args) => ConnectedTriggered = true;
                ConnectionError += (sender, args) => ConnectionErrorTriggered = true;
                DataChanged += (sender, args) =>
                {
                    DataChangedTriggered = true;
                    ++DataChangedTriggeredCount;
                };
            }

            public bool ConnectedTriggered { get; private set; }
            public bool ConnectionErrorTriggered { get; private set; }
            public bool DataChangedTriggered { get; private set; }
            public int DataChangedTriggeredCount { get; private set; }
        }
    }
}
