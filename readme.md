# Contrib.KubeClient.CustomResources

[![NuGet package](https://img.shields.io/nuget/v/Contrib.KubeClient.CustomResources.svg)](https://www.nuget.org/packages/Contrib.KubeClient.CustomResources/)
[![Build status](https://img.shields.io/appveyor/ci/AXOOM/contrib-kubeclient-customresources.svg)](https://ci.appveyor.com/project/AXOOM/contrib-kubeclient-customresources)

KubeClient contribution to simplify work with CustomResources.

## Usage

CustomResourceWatchers can be injected via `Microsoft.Extensions.DependencyInjection`.

```csharp
services
    .AddOptions()
    .Configure<KubernetesConfigurationStoreOptions>(opt => opt.ConnectionString = "http://localhost:8001/")
    .AddCustomResourceWatcher<CustomerResource, CustomerWatcher>(crdApiVersion="stable.myorg.com", crdPluralName="customers");
```

Make sure to set `crdApiVersion` and `crdPluralName` according to the values specified in the CRD you are using.

We furthermore provide an implementation for a `CustomResourceWatcher<TResource>` providing the actual resource.
This is meant to be an InMemoryCollection of all sorts of CustomResources of a certain _Kind_ stored in the cluster.

```csharp
public class CustomerWatcher : CustomResourceWatcher<CustomerResource>
{
    public CustomerWatcher(ILogger<CustomerWatcher> logger, ICustomResourceClient client, CustomResourceDefinition<CustomerResource> crd)
      : base(logger, client, crd, @namespace: string.Empty)
    {}
}
```

Setting the `@namespace` parameter to `string.Empty` does not filter for namespaces whereas setting a specific namespace only watches this exact namespace.

We provide a basic set of predefined `CustomResource` implementation.
You can derive from `CustomResource<TSpec>` if you are only interested in the `spec` property of a custom resource.
If you also need the `state` property, feel free to derive from `CustomResource<TSpec, TStatus>`

```csharp
public class Customer
{
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public DateTime Created { get; set; }
}

public class CustomerResource: CustomResource<Customer> { }
```

### Running in or outside the K8s cluster

We support both, running inside a K8s cluster or accessing it remotely.
Using [KubeApiClientFactory](src/library/KubeApiClientFactory.cs) you can provide a connection string (by setting the `KubernetesConfigurationStoreOptions.ConnectionString`) to be used.
If this connection string is set to `null`, ` `, or `string.Empty`, we assume, the application is running inside the cluster.

## Development

Run `build.ps1` to compile the source code and create NuGet packages.
This script takes a version number as an input argument. The source code itself contains no version numbers. Instead version numbers should be determined at build time using [GitVersion](http://gitversion.readthedocs.io/).
