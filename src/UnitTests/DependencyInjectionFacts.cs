using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Contrib.KubeClient.CustomResources
{
    public class DependencyInjectionFacts
    {
        private readonly ServiceProvider _provider;

        public DependencyInjectionFacts()
        {
            _provider = new ServiceCollection()
                       .AddLogging(builder => builder.AddConsole())
                       .AddOptions()
                       .AddKubeClient(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {["ApiEndPoint"] = "http://example.com/"}).Build())
                       .AddCustomResourceWatcher(new CustomResourceDefinition<CustomResource<string>>(apiVersion: "foo/v1", pluralName: "strings"))
                       .AddCustomResourceWatcher(new CustomResourceDefinition<CustomResource<int>>(apiVersion: "foo/v1", pluralName: "ints"))
                       .BuildServiceProvider();
        }

        [Fact]
        public void CanResolveClients()
        {
            _provider.GetRequiredService<ICustomResourceClient<CustomResource<string>>>();
            _provider.GetRequiredService<ICustomResourceClient<CustomResource<int>>>();
        }

        [Fact]
        public void CanResolveWatchers()
        {
            _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<string>>>();
            _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<int>>>();
        }

        [Fact]
        public void CanResolveListOfWatchers()
        {
            _provider.GetServices<ICustomResourceWatcher>().Should().BeEquivalentTo(
                _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<string>>>(),
                _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<int>>>());
        }

        [Fact]
        public void CanResolveListOfHostedServices()
        {
            _provider.GetServices<IHostedService>().Should().BeEquivalentTo(
                _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<string>>>(),
                _provider.GetRequiredService<ICustomResourceWatcher<CustomResource<int>>>());
        }
    }
}
