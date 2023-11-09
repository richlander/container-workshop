# Publishing Console apps as OCI images

The easiest way to publish an app to a container image is with [.NET SDK OCI image publish](https://learn.microsoft.com/dotnet/core/docker/publish-as-container). This document demonstrates how to publish ASP.NET console apps as container images. More general instructions are provided in [OCI image publishing property reference](publish-oci-properties.md). These instructions are part of a [container workshop](README.md), which details fundamental workflows for using .NET in containers.

Related:

- [Publishing apps as OCI images](publish-oci.md)
- [Docker build publishing](dockerfile-samples.md)

## Hello ASP.NET Core

OCI publish can be enabled with the `PublishProfile` property. The `ContainerFamily` propery enables using smaller base images.

Create the app and publish to an image.

```bash
$ mkdir hello-aspnet
$ cd hello-aspnet/
$ dotnet new web
$ dotnet publish -p PublishProfile=DefaultContainer -p ContainerFamily=jammy-chiseled
```

Look at the image:

```bash
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED         SIZE
hello-aspnet   latest    fd744cd2f854   9 seconds ago   109MB
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

In another terminal:

```bash
$ curl http://localhost:8000
Hello World!
```

## Composite images (smaller)

Composite images are a smaller image variant.

```bash
$ dotnet publish -p:PublishProfile=DefaultContainer -p:ContainerFamily=jammy-chiseled-composite
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-aspnet   latest    37dbfa21c5a9   16 seconds ago   102MB
```

The size difference is more significant for Arm64 images.

## Trimming

Size can be reduced further by publishing as self-contained and using trimming.

```bash
$ dotnet publish -p:PublishProfile=DefaultContainer -p:ContainerFamily=jammy-chiseled -p:PublishTrimmed=true --sc
MSBuild version 17.8.0+6cdef4241 for .NET
  Determining projects to restore...
  Restored /home/rich/hello-aspnet/hello-aspnet.csproj (in 471 ms).
/home/rich/dotnet-rc2/sdk/8.0.100-rc.2.23502.2/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(311,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [/home/rich/hello-aspnet/hello-aspnet.csproj]
  hello-aspnet -> /home/rich/hello-aspnet/bin/Release/net8.0/linux-x64/hello-aspnet.dll
  Optimizing assemblies for size. This process might take a while.
  hello-aspnet -> /home/rich/hello-aspnet/bin/Release/net8.0/linux-x64/publish/
  Building image 'hello-aspnet' with tags 'latest' on top of base image 'mcr.microsoft.com/dotnet/runtime-deps:8.0.0-rc.2-jammy-chiseled'.
  Pushed image 'hello-aspnet:latest' to local registry via 'docker'.
$ docker images hello-aspnet
REPOSITORY     TAG       IMAGE ID       CREATED          SIZE
hello-aspnet   latest    6771192894dc   11 seconds ago   40MB
```
