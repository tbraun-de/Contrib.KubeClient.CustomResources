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
                       .AddCustomResourceWatcher<Mock1Resource>()
                       .AddCustomResourceWatcher<Mock2Resource>()
                       .BuildServiceProvider();
        }

        [Fact]
        public void CanResolveClients()
        {
            _provider.GetRequiredService<ICustomResourceClient<Mock1Resource>>();
            _provider.GetRequiredService<ICustomResourceClient<Mock2Resource>>();
        }

        [Fact]
        public void CanResolveWatchers()
        {
            _provider.GetRequiredService<ICustomResourceWatcher<Mock1Resource>>();
            _provider.GetRequiredService<ICustomResourceWatcher<Mock2Resource>>();
        }

        [Fact]
        public void CanResolveListOfWatchers()
        {
            _provider.GetServices<ICustomResourceWatcher>().Should().BeEquivalentTo(
                _provider.GetRequiredService<ICustomResourceWatcher<Mock1Resource>>(),
                _provider.GetRequiredService<ICustomResourceWatcher<Mock2Resource>>());
        }

        [Fact]
        public void CanResolveListOfHostedServices()
        {
            _provider.GetServices<IHostedService>().Should().BeEquivalentTo(
                _provider.GetRequiredService<ICustomResourceWatcher<Mock1Resource>>(),
                _provider.GetRequiredService<ICustomResourceWatcher<Mock2Resource>>());
        }
    }
}
