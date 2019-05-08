using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceFacts
    {
        [Fact]
        public void CanDeserialize()
        {
            JsonConvert.DeserializeObject<Mock1Resource>("{\"metadata\":{\"name\":\"myname\",\"namespace\":\"mynamespace\"},\"spec\":1}")
                       .Should().BeEquivalentTo(new Mock1Resource("mynamespace", "myname", 1));
        }

        [Fact]
        public void RecordsDeserializationErrors()
        {
            var resource = JsonConvert.DeserializeObject<Mock1Resource>("{\"metadata\":{\"name\":\"myname\",\"namespace\":\"mynamespace\"},\"spec\":\"wrong\"}");
            resource.SerializationErrors.Should().HaveCount(1);
        }

        [Fact]
        public void ThrowsOnMetadataDeserializationErrors()
        {
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Mock1Resource>("{\"metadata\":\"wrong\",\"spec\":\"myspec\"}"));
        }
    }
}
