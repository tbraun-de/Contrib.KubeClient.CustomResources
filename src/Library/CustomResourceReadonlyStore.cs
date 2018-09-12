using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceReadonlyStore<TResourceSpec> : ICustomResourceReadonlyStore<TResourceSpec>
    {
        private ICustomResourceWatcher<TResourceSpec> _watcher;

        protected ICustomResourceClient<TResourceSpec> Client => _watcher.Client;

        public CustomResourceReadonlyStore(ICustomResourceWatcher<TResourceSpec> watcher)
        {
            _watcher = watcher;
            watcher.StartWatching();
        }

        public Task<IEnumerable<CustomResource<TResourceSpec>>> FindAllAsync()
            => Task.FromResult(_watcher.RawResources);

        public Task<IEnumerable<CustomResource<TResourceSpec>>> FindAsync(Func<CustomResource<TResourceSpec>, bool> query)
            => Task.FromResult(_watcher.RawResources.Where(query));

        public Task<IEnumerable<CustomResource<TResourceSpec>>> FindByNamespaceAsync(string @namespace)
            => FindAsync(res => res.Metadata.Namespace.Equals(@namespace, StringComparison.InvariantCultureIgnoreCase));

        public Task<CustomResource<TResourceSpec>> FindByNameAsync(string name)
        {
            var customResource = _watcher.RawResources.FirstOrDefault(res => res.Metadata.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (customResource == null)
                throw new KeyNotFoundException($"No such resource '{name}'");

            return Task.FromResult(customResource);
        }

        public Task<long> CountAsync() => Task.FromResult(_watcher.RawResources.LongCount());
    }
}
