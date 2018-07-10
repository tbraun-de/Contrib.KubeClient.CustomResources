using KubeClient.Models;

namespace Contrib.KubeClient.CustomResources
{
    public static class CustomResourceFactory
    {
        public static CustomResource<T> Create<T>(T spec, string name = "test", string @namespace = "test-ns")
        {
            return new CustomResource<T>
            {
                Metadata = new ObjectMetaV1
                {
                    Name = name,
                    Namespace = @namespace
                },
                Spec = spec
            };
        }
    }
}
