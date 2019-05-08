using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Moq;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceClientRealizeStateFacts : IDisposable
    {
        private readonly Mock<ICustomResourceClient<Mock1Resource>> _clientMock = new Mock<ICustomResourceClient<Mock1Resource>>();
        private readonly ICustomResourceClient<Mock1Resource> _client;

        public CustomResourceClientRealizeStateFacts()
        {
            _client = _clientMock.Object;
        }

        [Fact]
        public async Task CreatesMissingItems()
        {
            const string labelSelector = "a=b";
            const string @namespace = "ns";

            var item1 = new Mock1Resource(@namespace, "item1", 1);
            var item2 = new Mock1Resource(@namespace, "item2", 2);

            _clientMock.Setup(x => x.ListAsync(labelSelector, @namespace, CancellationToken.None)).ReturnsAsync(new CustomResourceList<Mock1Resource>
            {
                Items = {item1}
            });

            _clientMock.Setup(x => x.CreateAsync(item2, CancellationToken.None)).ReturnsAsync(item1);

            await _client.RealizeStateAsync(new[] {item1, item2}, labelSelector, @namespace);
        }

        [Fact]
        public async Task UpdatesExistingItems()
        {
            const string labelSelector = "a=b";
            const string @namespace = "ns";

            var item1 = new Mock1Resource(@namespace, "item1", 1);
            var item2 = new Mock1Resource(@namespace, "item2", 2);
            var item2Mismatch = new Mock1Resource(@namespace, "item2", 3);

            _clientMock.Setup(x => x.ListAsync(labelSelector, @namespace, CancellationToken.None)).ReturnsAsync(new CustomResourceList<Mock1Resource>
            {
                Items = {item1, item2Mismatch}
            });

            _clientMock.Setup(x => x.UpdateAsync(item2.Metadata.Name, It.IsAny<Action<JsonPatchDocument<Mock1Resource>>>(), @namespace, CancellationToken.None)).ReturnsAsync(item2);

            await _client.RealizeStateAsync(new[] {item1, item2}, labelSelector, @namespace);
        }

        [Fact]
        public async Task DeletesSuperfluousItems()
        {
            const string labelSelector = "a=b";
            const string @namespace = "ns";

            var item1 = new Mock1Resource(@namespace, "item1", 1);
            var item2 = new Mock1Resource(@namespace, "item2", 2);

            _clientMock.Setup(x => x.ListAsync(labelSelector, @namespace, CancellationToken.None)).ReturnsAsync(new CustomResourceList<Mock1Resource>
            {
                Items = {item1, item2}
            });

            _clientMock.Setup(x => x.DeleteAsync(item2.Metadata.Name, item2.Metadata.Namespace, CancellationToken.None)).ReturnsAsync(item2);

            await _client.RealizeStateAsync(new[] {item1}, labelSelector, @namespace);
        }

        public void Dispose()
        {
            _clientMock.VerifyAll();
        }
    }
}
