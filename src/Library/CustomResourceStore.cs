using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceStore<TResource> : ICustomResourceStore<TResource>
    {
        private readonly IEnumerable<CustomResource<TResource>> _resources;

        public CustomResourceStore(ICustomResourceWatcher<TResource> watcher)
        {
            _resources = watcher.RawResources;
            watcher.StartWatching();
        }

        public Task<IEnumerable<CustomResource<TResource>>> GetAllAsync()
            => Task.FromResult(_resources);

        public Task<IEnumerable<CustomResource<TResource>>> FindAsync(Func<CustomResource<TResource>, bool> query)
            => Task.FromResult(_resources.Where(query));

        public Task<IEnumerable<CustomResource<TResource>>> FindByNamespaceAsync(string @namespace)
            => FindAsync(res => res.Metadata.Namespace.Equals(@namespace, StringComparison.InvariantCultureIgnoreCase));

        public Task<CustomResource<TResource>> FindByNameAsync(string name)
        {
            var customResource = _resources.FirstOrDefault(res => res.Metadata.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (customResource == null)
                throw new KeyNotFoundException($"No such resource '{name}'");

            return Task.FromResult(customResource);
        }

        public Task<long> CountAsync() => Task.FromResult(_resources.LongCount());
    }
}
