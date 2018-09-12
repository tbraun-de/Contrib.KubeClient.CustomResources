# Contrib.KubeClient.CustomResources

[![NuGet package](https://img.shields.io/nuget/v/Contrib.KubeClient.CustomResources.svg)](https://www.nuget.org/packages/Contrib.KubeClient.CustomResources/)
[![Build status](https://img.shields.io/appveyor/ci/AXOOM/contrib-kubeclient-customresources.svg)](https://ci.appveyor.com/project/AXOOM/contrib-kubeclient-customresources)

KubeClient contribution to simplify work with CustomResources.

## Usage

The Kubernetes client can be injected via `Microsoft.Extensions.DependencyInjection`.

```csharp
services
    .AddOptions()
    .Configure<KubernetesConfigurationStoreOptions>(opt => opt.ConnectionString = "http://localhost:8001/")
    .AddKubernetesClient()
```

We furthermore provide an implementation for an `ICustomResourceStore<TResourceSpec>`.
This is meant to be an InMemoryCollection of all sorts of CustomResources of a certain _Kind_ stored in the cluster.

```csharp
 public class CustomerStore : CustomResourceStore<Customer>, ICustomerStore
    {
        public CustomerStore(ICustomResourceWatcher<Customer> watcher)
            : base(watcher)
        {}
    }
```

You only have to model the `spec` part of the CustomResourceDefinition and pass this class as `TResourceSpec` type argument.

```csharp
public class Customer
{
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public DateTime Created { get; set; }
}
```

As you already might have recognized, the last missing part is a `CustomResourceWatcher<TResourceSpec>` providing the actual resource.

```csharp
public class CustomerWatcher : CustomResourceWatcher<Customer>
{
    public CustomerWatcher(ILogger<CustomerWatcher> logger, ICustomResourceClient client)
      : base(logger, client, new CustomResourceDefinition<Customer>(apiVersion: "stable.mycompany.com", pluralName: "customers"), @namespace: string.Empty)
    {}
}
```

Make sure to set `apiVersion` and `crdPluralName` according to the values specified in the CRD you are using.
A more interesting part is the `@namespace` parameter; setting it to `string.Empty` does not filter for namespaces whereas setting a namespace only watches this exact namespace.

### Running in or outside the K8s cluster

We support both, running inside a K8s cluster or accessing it remotely.
Using [KubeApiClientFactory](src/library/KubeApiClientFactory.cs) you can provide a connection string (by setting the `KubernetesConfigurationStoreOptions.ConnectionString`) to be used.
If this connection string is set to `null`, ` `, or `string.Empty`, we assume, the application is running inside the cluster.

## Development

Run `build.ps1` to compile the source code and create NuGet packages.
This script takes a version number as an input argument. The source code itself contains no version numbers. Instead version numbers should be determined at build time using [GitVersion](http://gitversion.readthedocs.io/).
