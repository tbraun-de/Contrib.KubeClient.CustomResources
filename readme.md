# Contrib.KubeClient.CustomResources

[![NuGet package](https://img.shields.io/nuget/v/Contrib.KubeClient.CustomResources.svg)](https://www.nuget.org/packages/Contrib.KubeClient.CustomResources/)
[![Build status](https://img.shields.io/appveyor/ci/AXOOM/contrib-kubeclient-customresources.svg)](https://ci.appveyor.com/project/AXOOM/contrib-kubeclient-customresources)

Extension for [KubeClient](https://github.com/tintoy/dotnet-kube-client) to simplify working with [Kubernetes Custom Resources](https://kubernetes.io/docs/concepts/extend-kubernetes/api-extension/custom-resources/).

## Usage

You can create a class representing your Kubernetes Custom Resource by deriving from `CustomResource`, `CustomResource<TSpec>` or `CustomResource<TSpec, TStatus>`.

You will also need to provide an instance of `CustomResourceDefinition<TResource>` specifying your CRDs API version and Plural Name.

Example:
```csharp
public class PersonResource : CustomResource<PersonSpec>
{
    public static CustomResourceDefinition<PersonResource> Definition { get; } = new CustomResourceDefinition<PersonResource>(apiVersion: "stable.myorg.com", pluralName: "persons");
}

public class PersonSpec
{
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public DateTime Created { get; set; }
}
```

You can then configure an instance of `ICustomResourceClient<T>` for dependency injection like this:
```csharp
services.AddCustomResourceClient(PersonResource.Definition);
```

You can also set up a watcher that watches Custom Resources of a specific type for changes and keeps an in-memory representation:
```csharp
services.AddCustomResourceWatcher(PersonResource.Definition, @namespace: "my-kubernetes-namespace");
```

The watchers implement the `IHostedService` interface which causes [ASP.NET Core](https://docs.microsoft.com/de-de/aspnet/core/fundamentals/host/hosted-services) to automatically start and stop them with the hosting service.

## Development

Run `build.ps1` to compile the source code and create NuGet packages.
This script takes a version number as an input argument. The source code itself contains no version numbers. Instead version numbers should be determined at build time using [GitVersion](http://gitversion.readthedocs.io/).
