using System;
using System.Linq;
using System.Net;
using System.Reactive.Subjects;
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
        private readonly Mock<ICustomResourceClient> _resourceClientMock;

        public CustomResourceWatcherFacts()
        {
            _resourceSubject = new Subject<IResourceEventV1<CustomResource<Client>>>();
            _resourceClientMock = new Mock<ICustomResourceClient>();
            _resourceClientMock.SetupSequence(mock => mock.Watch<Client>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                               .Returns(_resourceSubject)
                               .Returns(new Subject<IResourceEventV1<CustomResource<Client>>>());
            _watcher = new TestResourceWatcher(_resourceClientMock.Object);
            _watcher.StartWatching();
        }

        [Fact]
        public void AddedResourceGetsAddedToCache()
        {
            var expectedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "1");

            _resourceSubject.OnNext(expectedResource);

            _watcher.Resources.Should().Contain(expectedResource.Resource.Spec);
        }

        [Fact]
        public void ModifiedResourceGetsUpdatedInCache()
        {
            var addedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "4711");
            var modifiedResource = CreateResourceEvent(ResourceEventType.Modified, "expectedClientId", resourceVersion: "4712");
            modifiedResource.Resource.Spec.ClientName = "clientname";
            _resourceSubject.OnNext(addedResource);

            _resourceSubject.OnNext(modifiedResource);

            _watcher.Resources.Should().Contain(modifiedResource.Resource.Spec);
        }

        [Fact]
        public void DeletedResourceGetsRemovedFromCache()
        {
            var watcherResources = _watcher.Resources;

            var addedResource = CreateResourceEvent(ResourceEventType.Added, "expectedClientId", resourceVersion: "1");
            var removedResource = CreateResourceEvent(ResourceEventType.Deleted, "expectedClientId", resourceVersion: "2");
            _resourceSubject.OnNext(addedResource);
            _resourceSubject.OnNext(removedResource);

            watcherResources.Should().BeEmpty();
        }

        [Fact]
        public void RaisesOnConnected()
        {
            _watcher.OnConnectedTriggered.Should().BeTrue();
        }

        [Fact]
        public void RaisesOnConnectionError()
        {
            _watcher.StartWatching();

            _resourceSubject.OnError(new Exception());

            _watcher.OnConnectionErrorTriggered.Should().BeTrue();
        }

        [Fact]
        public void ResubscribesOnError()
        {
            _watcher.StartWatching();

            _resourceSubject.OnError(new Exception());

            _resourceClientMock.Verify(mock => mock.Watch<Client>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void ResubscribesOnCompletion()
        {
            _watcher.StartWatching();

            _resourceSubject.OnCompleted();

            _resourceClientMock.Verify(mock => mock.Watch<Client>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void DoesNotResubscribeAfterDisposal()
        {
            _watcher.StartWatching();

            _watcher.Dispose();
            _resourceSubject.OnCompleted();

            _resourceClientMock.Verify(mock => mock.Watch<Client>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(1));
        }

        [Fact]
        public void PassesLastResourceVersionOnReconnect()
        {
            _watcher.StartWatching();
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, name: "4711", resourceVersion: "35"));

            _resourceSubject.OnError(new Exception());

            _resourceClientMock.Verify(mock => mock.Watch<Client>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "35"));
        }

        [Fact]
        public void DropsCacheWhenResourceIsGone()
        {
            _watcher.StartWatching();
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, name: "4711", resourceVersion: "35"));

            _resourceSubject.OnError(new HttpRequestException<StatusV1>(HttpStatusCode.Gone, new StatusV1()));

            _watcher.Resources.Should().BeEmpty();
        }

        [Fact]
        public void DropsResourceEventIfOlderThanLastKnown()
        {
            _watcher.StartWatching();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, name: "resource", resourceVersion: "10"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, name: "resource", resourceVersion: "12"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, name: "resource", resourceVersion: "11"));

            _watcher.RawResources.First().Metadata.ResourceVersion.Should().Be("12");
        }

        [Fact]
        public void DoesNotDropResourceEventIfVersionIsSmallerButDifferentResource()
        {
            _watcher.StartWatching();

            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Added, name: "resource", resourceVersion: "10"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, name: "resource", resourceVersion: "12"));
            _resourceSubject.OnNext(CreateResourceEvent(ResourceEventType.Modified, name: "anotherResource", resourceVersion: "9"));

            _watcher.RawResources.Single(r => r.Metadata.Name == "resource").Metadata.ResourceVersion.Should().Be("12");
            _watcher.RawResources.Single(r => r.Metadata.Name == "anotherResource").Metadata.ResourceVersion.Should().Be("9");
        }

        private static ResourceEventV1<CustomResource<Client>> CreateResourceEvent(ResourceEventType eventType, string name, string resourceVersion)
            => new ResourceEventV1<CustomResource<Client>>
            {
                EventType = eventType,
                Resource = new CustomResource<Client>
                {
                    Metadata = new ObjectMetaV1
                    {
                        Namespace = "namespace",
                        Name = name,
                        ResourceVersion = resourceVersion,
                        Uid = name
                    },
                    Spec = new Client {ClientId = name}
                }
            };

        private class TestResourceWatcher : CustomResourceWatcher<Client>
        {
            public TestResourceWatcher(ICustomResourceClient client)
                : base(new Logger<CustomResourceWatcher<Client>>(new LoggerFactory()), client, apiGroup: "stable.contrib.identityserver.io", crdPluralName: "identityclients", @namespace: string.Empty)
            {
                OnConnected += (sender, args) => OnConnectedTriggered = true;
                OnConnectionError += (sender, args) => OnConnectionErrorTriggered = true;
            }

            public bool OnConnectedTriggered { get; private set; }
            public bool OnConnectionErrorTriggered { get; private set; }
        }
    }
}
