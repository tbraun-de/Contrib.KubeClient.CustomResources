using System.Collections.Generic;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    public class CustomResourceList<TResource> : KubeResourceListV1<TResource>
        where TResource : KubeResourceV1
    {
        public override List<TResource> Items { get; } = new List<TResource>();
    }
}
