namespace Contrib.KubeClient.CustomResources
{
    public class Mock2Resource : CustomResource
    {
        public new static CustomResourceDefinition Definition { get; } = new CustomResourceDefinition(apiVersion: "example.com/v1", pluralName: "mock2s", kind: "Mock");

        public Mock2Resource()
            : base(Definition)
        {}
    }
}
