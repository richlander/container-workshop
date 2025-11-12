# Publish OCI image in SDK container

This document demonstrates how to publish .NET web apps as container images, relying on .NET SDK container images. It is part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

The following patterns rely on [OCI image publishing](https://learn.microsoft.com/dotnet/core/docker/publish-as-container). They run `dotnet publish` within an SDK container, avoiding the need to install .NET (and other dependencies) locally. This is particularly useful for native AOT, which is used in the examples. The overall pattern isn't specific to native AOT.

Native AOT SDK container images are used in the instructions: `mcr.microsoft.com/dotnet/sdk:10.0-aot`.

For non-native AOT use cases, the smaller SDK image can be used, such as `mcr.microsoft.com/dotnet/sdk:10.0`.

## Create app

Create or use your own app.

```bash
$ dotnet new webapiaot -n webapi
$ cd webapi
```

## Run app locally

The app can be run locally.

```bash
$ dotnet run
Using launch settings from /home/rich/git/container-workshop/webapi/Properties/launchSettings.json...
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5069
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/rich/git/container-workshop/webapi
```

The port on your machine will likely differ.

Call the service in another window:

```bash
$ curl http://localhost:5099/todos
[{"id":1,"title":"Walk the dog","dueBy":null,"isComplete":false},{"id":2,"title":"Do the dishes","dueBy":"2025-11-11","isComplete":false},{"id":3,"title":"Do the laundry","dueBy":"2025-11-12","isComplete":false},{"id":4,"title":"Clean the bathroom","dueBy":null,"isComplete":false},{"id":5,"title":"Clean the car","dueBy":"2025-11-13","isComplete":false}]
```

## Add new endpoint

An endpoint that returns `RuntimeInformation.OSDescription` would be nice. It can be added to `Program.cs` 

```csharp
app.MapGet("/os", () => $$"""{"os-description" : "{{System.Runtime.InteropServices.RuntimeInformation.OSDescription}}"}{{Environment.NewLine}}""");

app.Run();
```

That's not the most idiomatic C#. Returning an object and relying on automatic serialization would be more typical. However, that code is the best we can get in one line. It also demonstrates [interpolated raw string literals](https://learn.microsoft.com/dotnet/csharp/language-reference/tokens/interpolated#interpolated-raw-string-literals).

Once the app is re-run, the new end-point can be called and pretty-printed with `jq`.

```bash
$ curl -s http://localhost:5099/os | jq
{
  "os-description": "Ubuntu 24.04.3 LTS"
}
```

## Build bare binary locally

The app can be build locally with the .NET SDK and [Native AOT Prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/). A native AOT SDK container image can also be used, skipping the need to install other software.

Build the app, in the SDK container

```bash
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/sdk:10.0-aot dotnet publish -o app
$ ls -l app
total 35492
-rw-rw-r-- 1 root root      119 Nov 11 22:09 appsettings.Development.json
-rw-rw-r-- 1 root root      142 Nov 11 22:09 appsettings.json
-rwxr-xr-x 1 root root 11094384 Nov 11 22:17 webapi
-rwxr-xr-x 1 root root 25232400 Nov 11 22:17 webapi.dbg
-rw-r--r-- 1 root root       53 Nov 11 22:17 webapi.staticwebassets.endpoints.json
```

The app is now available locally. It's about 11MB.

It can be run in a similar way. It will run in a Linux environment, since the container images builds a Linux binary.

```bash
$ ./app/webapi
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /home/rich/git/container-workshop/webapi
```

This time, it is hosted on port `5000`.

```bash
$ curl -s http://localhost:5000/os | jq
{
  "os-description": "Ubuntu 24.04.3 LTS"
}
```

## Build a container image

This pattern can be taken one step further, to build a container image. We're going to use a similar volume mounting technique, but with a [tarball archive as the output](https://github.com/dotnet/core/issues/8440#issuecomment-1743593480) using the `ContainerArchiveOutputPath` property.

The project is configured to reduce app size:

```bash
$ cat webapi.csproj 
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>true</InvariantGlobalization>
    <PublishAot>true</PublishAot>
    <CopyOutputSymbolsToPublishDirectory>false</CopyOutputSymbolsToPublishDirectory>
    <OptimizationPreference>Size</OptimizationPreference>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
  </ItemGroup>
  
</Project>
```

The image can now be built.

```bash
$ docker run --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/sdk:10.0-aot dotnet publish -t:PublishContainer -p:ContainerArchiveOutputPath=/source/image/webapi.tar.gz
$ ls image
webapi.tar.gz
$ docker load --input image/webapi.tar.gz
4242a3dfb5a3: Loading layer  5.237MB/5.237MB
The image webapi:latest already exists, renaming the old one with ID sha256:8e53bf4aed18d85e7a2383ec249cb0ff4c93489ecdaad4d176f6cf9973c89853 to empty string
Loaded image: webapi:latest
$ docker images webapi
REPOSITORY   TAG       IMAGE ID       CREATED         SIZE
webapi       latest    765151dbe0af   4 minutes ago   25MB
```

That worked. The image was published inside a container image, written to the local machine via a volume mount, and then loaded into the local docker cache via `docker load`.

There are two sizes listed above. The tarball is compressed and the container image is not.

## Publish container image to a remote registry

The image can be published to a remote registry, using much the same pattern. There are some key differences.

- `ContainerRepository` must specify the image name plus any "org" information.
- `ContainerRegistry` must specify the registry name, like `docker.io` or `foo.azurecr.io`.
- [Credentials must be provided](https://github.com/dotnet/sdk-container-builds/blob/main/docs/RegistryAuthentication.md) to push to a registry.

Credentials can be provided in two ways.

- Pass credentials as [environment variables](https://github.com/dotnet/sdk-container-builds/issues/486).
- Volume mount `.docker/config.json`. This approach only works in environments where credentials are left unencrypted (primarily, Linux) or via [custom-generated json file](https://github.com/dotnet/sdk-container-builds/issues/484#issuecomment-1657048065) using the same format.

For this scenario, I'm going to volume mount `.docker/config.json` and login to https://hub.docker.com/.

```bash
$ docker login -u richlander
Password: 
WARNING! Your password will be stored unencrypted in /home/rich/.docker/config.json.
```

And then publish, volume mounting the app directory and docker credentials:

```bash
$ docker run --rm -it -v $(pwd):/source -w /source -v /home/rich/.docker:/root/.docker mcr.microsoft.com/dotnet/sdk:10.0-aot dotnet publish -t:PublishContainer -p:ContainerRepository=richlander/webapi -p:ContainerRegistry=docker.io
$ docker run --rm -d -p 8000:8080 richlander/webapi
$ curl -s http://localhost:8000/os | jq
{
  "os-description": "Ubuntu 24.04.3 LTS"
}
```

I can also access the endpoint from another machine on the same network.

```bash
$ curl http://merritt:8000/os | jq
{
  "os-description": "Ubuntu 24.04.3 LTS"
}
```

## Publish container image to a local registry

It is possible to host a local registry using the [`registry`](https://hub.docker.com/_/registry) image. Starting in .NET 10, the SDK can push to insecure registries so this all just works.

Launch a local registry instance

```bash
$ docker run -d -p 5000:5000 registry
```

Publish the image and push to the local registry.

```bash
$ docker run --network host --rm -it -v $(pwd):/source -w /source mcr.microsoft.com/dotnet/sdk:10.0-noble-aot dotnet publish /t PublishContainer -p ContainerRepository=hello-native-api -p ContainerRegistry=http://localhost:5000 -v d
Restore complete (3.6s)
    Determining projects to restore...
    Restored /source/webapi.csproj (in 3.2 sec).
  webapi net10.0 linux-arm64 succeeded (0.8s) → bin/Release/net10.0/linux-arm64/publish/
  webapi net10.0 linux-arm64 succeeded (0.7s)
    Building image 'hello-native-api' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:10.0-noble-chiseled'.
    [Microsoft.NET.Build.Containers.Registry] Uploading layer 'sha256:8fff930231e2b39857aa470066309894f9a8186b312da75abd92c5e5301b9cf2' to 'localhost:5000'.
    [Microsoft.NET.Build.Containers.Registry] Uploading layer 'sha256:59e1b2e43541dd0ff74ca531df1fe5087649450193965a2684f79b3141ae6d69' to 'localhost:5000'.
    [Microsoft.NET.Build.Containers.Registry] Uploading layer 'sha256:acb47e78e665f478f813899537c6d2f477f52bfacc85a10f80f2d0c05d2b6f6c' to 'localhost:5000'.
    [Microsoft.NET.Build.Containers.Registry] Layer 'sha256:8fff930231e2b39857aa470066309894f9a8186b312da75abd92c5e5301b9cf2' already exists.
    [Microsoft.NET.Build.Containers.Registry] Layer 'sha256:59e1b2e43541dd0ff74ca531df1fe5087649450193965a2684f79b3141ae6d69' already exists.
    [Microsoft.NET.Build.Containers.Registry] Finished uploading layer 'sha256:acb47e78e665f478f813899537c6d2f477f52bfacc85a10f80f2d0c05d2b6f6c' to 'localhost:5000'.
    [Microsoft.NET.Build.Containers.Registry] Uploading config to registry at blob 'sha256:158ad1b5881e07a92bf9f6c35e11da990fb07b6b11466d3305e7dc1eb5868d27',
    [Microsoft.NET.Build.Containers.Registry] Uploaded config to registry.
    [Microsoft.NET.Build.Containers.Registry] Uploading tag 'latest' to 'localhost:5000'.
    [Microsoft.NET.Build.Containers.Registry] Uploaded tag 'latest' to 'localhost:5000'.
    Pushed image 'hello-native-api:latest' to registry 'localhost:5000'
```