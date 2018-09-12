using System.Threading.Tasks;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceStore<TResourceSpec> : CustomResourceReadonlyStore<TResourceSpec>, ICustomResourceStore<TResourceSpec>
    {
        public CustomResourceStore(ICustomResourceWatcher<TResourceSpec> watcher)
            : base(watcher)
        {}

        public Task<CustomResource<TResourceSpec>> AddAsync(CustomResource<TResourceSpec> resource)
            => Client.CreateAsync(resource);

        public Task<CustomResource<TResourceSpec>> UpdateAsync(CustomResource<TResourceSpec> resource)
            => Client.UpdateAsync(resource);

        public Task<CustomResource<TResourceSpec>> DeleteAsync(string resourceName, string @namespace = null)
            => Client.DeleteAsync(resourceName, @namespace);
    }
}
