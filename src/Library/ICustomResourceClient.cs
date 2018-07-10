using System;
using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    public interface ICustomResourceClient
    {
        IObservable<IResourceEventV1<CustomResource<TSpec>>> Watch<TSpec>(string apiGroup, string crdPluralName, string @namespace = "", string lastSeenResourceVersion = "0");
    }
}
