# Publishing ASP.NET Core apps as OCI images

This document demonstrates how to publish ASP.NET console apps as container images. These instructions are part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

## Hello ASP.NET Core

Create the app.

```bash
dotnet new web -o hello-aspnet
cd hello-aspnet/
```

Publish and run the app image.

```bash
$ dotnet publish -t:PublishContainer
$ docker run --rm -it -p 8000:8080 hello-aspnet
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:8080
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Production
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /app
```

In another terminal, call the endpoint with `curl`.

```bash
$ curl http://localhost:8000
Hello World!
```

Inspect the image.

```bash
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-aspnet   latest    90bc845f23c9   8 seconds ago   217MB
```

## Using smaller base images

Chiseled images can make an image much smaller without changing anything else.

```bash
$ dotnet publish -t:PublishContainer -p:ContainerFamily=jammy-chiseled-extra
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-aspnet   latest    b0500047363a   8 seconds ago   147MB
```

Smaller images without globalization support can be used if `InvariantGlobalization` is used.

```bash
$ dotnet publish -t:PublishContainer -p:ContainerFamily=jammy-chiseled -p:InvariantGlobalization=true
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-aspnet   latest    fe3294c3dd3f   25 seconds ago   109MB
```

Composite images are a little smaller, still.

```bash
$ dotnet publish -t:PublishContainer -p:ContainerFamily=jammy-chiseled-composite -p:InvariantGlobalization=true
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-aspnet   latest    37dbfa21c5a9   16 seconds ago   102MB
```

The size difference is more significant for Arm64 images.

## Trimming

Size can be reduced further by publishing as self-contained and using trimming. Trimming is covered in more detail in [.NET SDK Publish Options](./publish-options.md).

Project file trimming enabled:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>hello_aspnet</RootNamespace>
    <InvariantGlobalization>true</InvariantGlobalization>

    <!-- Publishing properties -->
    <PublishTrimmed>true</PublishTrimmed>
    <ContainerRepository>hello-aspnet-chiseled-trimmed</ContainerRepository>
  </PropertyGroup>

</Project>
```

```bash
dotnet publish -t:PublishContainer
docker run -it --rm -p 8000:8080 hello-aspnet-chiseled-trimmed
```

Inspect image:

```bash
$ docker images hello-aspnet-chiseled-trimmed
REPOSITORY                      TAG       IMAGE ID       CREATED         SIZE
hello-aspnet-chiseled-trimmed   latest    339aa42a4300   2 minutes ago   39.9MB
```

Base image used: `mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled`
